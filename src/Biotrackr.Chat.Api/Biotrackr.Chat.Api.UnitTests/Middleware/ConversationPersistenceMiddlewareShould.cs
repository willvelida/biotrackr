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
