using Biotrackr.Chat.Api.Middleware;
using Biotrackr.Chat.Api.Services;
using FluentAssertions;
using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging;
using Moq;
using System.Runtime.CompilerServices;
using System.Text.Json;
using ChatMessage = Microsoft.Extensions.AI.ChatMessage;
using ChatConversationDocument = Biotrackr.Chat.Api.Models.ChatConversationDocument;

namespace Biotrackr.Chat.Api.UnitTests.Middleware
{
    public class ConversationPersistenceMiddlewareShould
    {
        private readonly Mock<IChatHistoryRepository> _repositoryMock;
        private readonly Mock<ILogger<ConversationPersistenceMiddleware>> _loggerMock;
        private readonly ConversationPersistenceMiddleware _sut;

        public ConversationPersistenceMiddlewareShould()
        {
            _repositoryMock = new Mock<IChatHistoryRepository>();
            _loggerMock = new Mock<ILogger<ConversationPersistenceMiddleware>>();

            _repositoryMock
                .Setup(r => r.SaveMessageAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<List<string>?>()))
                .ReturnsAsync(new ChatConversationDocument());

            _sut = new ConversationPersistenceMiddleware(_repositoryMock.Object, _loggerMock.Object);
        }

        private static IEnumerable<ChatMessage> CreateMessages(string userText)
        {
            return [new ChatMessage(ChatRole.User, userText)];
        }

        [Fact]
        public async Task LogToolCallWithArguments_WhenFunctionCallContentIsYielded()
        {
            // Arrange
            var arguments = new Dictionary<string, object?> { ["date"] = "2026-01-15", ["page"] = 1 };
            var functionCall = new FunctionCallContent("call-1", "GetActivityByDate", arguments);
            var agent = new FakeAgent(functionCall, new TextContent("Here is your data."));
            var messages = CreateMessages("Show me my activity");

            // Act
            await foreach (var _ in _sut.HandleAsync(messages, null, null, agent, CancellationToken.None))
            {
                // Drain the stream
            }

            // Assert
            _loggerMock.VerifyLog(l => l.LogInformation(
                It.Is<string>(s => s.Contains("GetActivityByDate") && s.Contains("2026-01-15"))),
                Times.Once);
        }

        [Fact]
        public async Task LogNullArguments_WhenFunctionCallHasNoArguments()
        {
            // Arrange
            var functionCall = new FunctionCallContent("call-1", "GetActivityByDate");
            var agent = new FakeAgent(functionCall, new TextContent("Here is your data."));
            var messages = CreateMessages("Show me my activity");

            // Act
            await foreach (var _ in _sut.HandleAsync(messages, null, null, agent, CancellationToken.None))
            {
                // Drain the stream
            }

            // Assert
            _loggerMock.VerifyLog(l => l.LogInformation(
                It.Is<string>(s => s.Contains("GetActivityByDate") && s.Contains("null"))),
                Times.Once);
        }

        [Fact]
        public async Task NotPersistArgumentsToCosmosDb_WhenToolCallIsLogged()
        {
            // Arrange
            var arguments = new Dictionary<string, object?> { ["date"] = "2026-01-15" };
            var functionCall = new FunctionCallContent("call-1", "GetActivityByDate", arguments);
            var agent = new FakeAgent(functionCall, new TextContent("Here is your data."));
            var messages = CreateMessages("Show me my activity");

            // Act
            await foreach (var _ in _sut.HandleAsync(messages, null, null, agent, CancellationToken.None))
            {
                // Drain the stream
            }

            // Assert — only tool names are persisted, not arguments
            _repositoryMock.Verify(r => r.SaveMessageAsync(
                It.IsAny<string>(),
                "assistant",
                It.IsAny<string>(),
                It.Is<List<string>?>(tc => tc != null && tc.Contains("GetActivityByDate") && tc.All(t => !t.Contains("2026-01-15")))),
                Times.Once);
        }

        [Fact]
        public void MaskSensitiveFields_ShouldReturnNull_WhenArgumentsAreNull()
        {
            var result = ConversationPersistenceMiddleware.MaskSensitiveFields(null);
            result.Should().Be("null");
        }

        [Fact]
        public void MaskSensitiveFields_ShouldSerializeArguments_WhenArgumentsAreProvided()
        {
            var arguments = new Dictionary<string, object?> { ["date"] = "2026-01-15", ["page"] = 1 };
            var result = ConversationPersistenceMiddleware.MaskSensitiveFields(arguments);
            result.Should().Contain("2026-01-15");
            result.Should().Contain("page");
        }

        [Fact]
        public async Task HandleAsync_ShouldContinueStreaming_WhenUserMessagePersistenceFails()
        {
            // Arrange
            _repositoryMock.Setup(r => r.SaveMessageAsync(
                It.IsAny<string>(), "user", It.IsAny<string>(), null))
                .ThrowsAsync(new InvalidOperationException("Cosmos DB unavailable"));

            var agent = new FakeAgent(new TextContent("Here is your data."));
            var messages = CreateMessages("Show me my activity");

            // Act — should not throw
            var updates = new List<AgentResponseUpdate>();
            await foreach (var update in _sut.HandleAsync(messages, null, null, agent, CancellationToken.None))
            {
                updates.Add(update);
            }

            // Assert — streaming continued despite persistence failure
            updates.Should().NotBeEmpty();
        }

        [Fact]
        public async Task HandleAsync_ShouldContinueStreaming_WhenAssistantPersistenceFails()
        {
            // Arrange — user persistence succeeds, assistant persistence fails
            _repositoryMock.Setup(r => r.SaveMessageAsync(
                It.IsAny<string>(), "assistant", It.IsAny<string>(), It.IsAny<List<string>?>()))
                .ThrowsAsync(new InvalidOperationException("Cosmos DB throttled"));

            var agent = new FakeAgent(new TextContent("Here is your data."));
            var messages = CreateMessages("Show me my activity");

            // Act — should not throw
            var updates = new List<AgentResponseUpdate>();
            await foreach (var update in _sut.HandleAsync(messages, null, null, agent, CancellationToken.None))
            {
                updates.Add(update);
            }

            // Assert — all agent updates were yielded before persistence was attempted
            updates.Should().NotBeEmpty();
        }

        [Fact]
        public async Task HandleAsync_ShouldLogError_WhenUserMessagePersistenceFails()
        {
            // Arrange
            _repositoryMock.Setup(r => r.SaveMessageAsync(
                It.IsAny<string>(), "user", It.IsAny<string>(), null))
                .ThrowsAsync(new InvalidOperationException("Cosmos DB unavailable"));

            var agent = new FakeAgent(new TextContent("Here is your data."));
            var messages = CreateMessages("Show me my activity");

            // Act
            await foreach (var _ in _sut.HandleAsync(messages, null, null, agent, CancellationToken.None)) { }

            // Assert
            _loggerMock.VerifyLog(l => l.LogError(
                It.IsAny<InvalidOperationException>(),
                It.Is<string>(s => s.Contains("Failed to persist user message"))),
                Times.Once);
        }

        [Fact]
        public async Task HandleAsync_ShouldLogError_WhenAssistantPersistenceFails()
        {
            // Arrange
            _repositoryMock.Setup(r => r.SaveMessageAsync(
                It.IsAny<string>(), "assistant", It.IsAny<string>(), It.IsAny<List<string>?>()))
                .ThrowsAsync(new InvalidOperationException("Cosmos DB throttled"));

            var agent = new FakeAgent(new TextContent("Here is your data."));
            var messages = CreateMessages("Show me my activity");

            // Act
            await foreach (var _ in _sut.HandleAsync(messages, null, null, agent, CancellationToken.None)) { }

            // Assert
            _loggerMock.VerifyLog(l => l.LogError(
                It.IsAny<InvalidOperationException>(),
                It.Is<string>(s => s.Contains("Failed to persist assistant response"))),
                Times.Once);
        }

        /// <summary>
        /// Concrete AIAgent subclass for testing that yields preconfigured content.
        /// </summary>
        private sealed class FakeAgent(params AIContent[] contents) : AIAgent
        {
            protected override Task<AgentResponse> RunCoreAsync(
                IEnumerable<ChatMessage> messages,
                AgentSession session,
                AgentRunOptions options,
                CancellationToken cancellationToken)
            {
                return Task.FromResult(new AgentResponse(new ChatMessage(ChatRole.Assistant, contents)));
            }

            protected override async IAsyncEnumerable<AgentResponseUpdate> RunCoreStreamingAsync(
                IEnumerable<ChatMessage> messages,
                AgentSession session,
                AgentRunOptions options,
                [EnumeratorCancellation] CancellationToken cancellationToken)
            {
                yield return new AgentResponseUpdate(ChatRole.Assistant, contents);
                await Task.CompletedTask;
            }

            protected override ValueTask<AgentSession> CreateSessionCoreAsync(CancellationToken cancellationToken)
            {
                return new ValueTask<AgentSession>(new FakeSession());
            }

            protected override ValueTask<AgentSession> DeserializeSessionCoreAsync(
                JsonElement sessionData,
                JsonSerializerOptions? options,
                CancellationToken cancellationToken)
            {
                return new ValueTask<AgentSession>(new FakeSession());
            }

            protected override ValueTask<JsonElement> SerializeSessionCoreAsync(
                AgentSession session,
                JsonSerializerOptions? options,
                CancellationToken cancellationToken)
            {
                return new ValueTask<JsonElement>(JsonDocument.Parse("{}").RootElement);
            }
        }

        private sealed class FakeSession : AgentSession;
    }
}
