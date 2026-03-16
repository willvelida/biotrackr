using Biotrackr.Chat.Api.Middleware;
using FluentAssertions;
using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging;
using Moq;
using System.Net;
using System.Runtime.CompilerServices;
using System.Text.Json;
using ChatMessage = Microsoft.Extensions.AI.ChatMessage;

namespace Biotrackr.Chat.Api.UnitTests.Middleware
{
    public class GracefulDegradationMiddlewareShould
    {
        private readonly Mock<ILogger<GracefulDegradationMiddleware>> _loggerMock;
        private readonly GracefulDegradationMiddleware _sut;

        public GracefulDegradationMiddlewareShould()
        {
            _loggerMock = new Mock<ILogger<GracefulDegradationMiddleware>>();
            _sut = new GracefulDegradationMiddleware(_loggerMock.Object);
        }

        private static IEnumerable<ChatMessage> CreateMessages(string userText)
        {
            return [new ChatMessage(ChatRole.User, userText)];
        }

        [Fact]
        public async Task HandleAsync_ShouldPassThroughNormally_WhenNoExceptionOccurs()
        {
            // Arrange
            var agent = new FakeAgent(new TextContent("Here is your data."));
            var messages = CreateMessages("Show me my activity");

            // Act
            var updates = new List<AgentResponseUpdate>();
            await foreach (var update in _sut.HandleAsync(messages, null, null, agent, CancellationToken.None))
            {
                updates.Add(update);
            }

            // Assert
            updates.Should().ContainSingle();
            updates[0].Contents.OfType<TextContent>().First().Text.Should().Be("Here is your data.");
        }

        [Theory]
        [InlineData(HttpStatusCode.ServiceUnavailable)]
        [InlineData(HttpStatusCode.TooManyRequests)]
        [InlineData(HttpStatusCode.BadGateway)]
        [InlineData(HttpStatusCode.GatewayTimeout)]
        public async Task HandleAsync_ShouldReturnFriendlyMessage_WhenClaudeApiReturnsErrorStatusCode(HttpStatusCode statusCode)
        {
            // Arrange
            var agent = new ThrowingAgent(new HttpRequestException("API error", null, statusCode));
            var messages = CreateMessages("Show me my activity");

            // Act
            var updates = new List<AgentResponseUpdate>();
            await foreach (var update in _sut.HandleAsync(messages, null, null, agent, CancellationToken.None))
            {
                updates.Add(update);
            }

            // Assert
            updates.Should().ContainSingle();
            updates[0].Contents.OfType<TextContent>().First().Text
                .Should().Be(GracefulDegradationMiddleware.ServiceUnavailableMessage);
        }

        [Fact]
        public async Task HandleAsync_ShouldReturnTimeoutMessage_WhenClaudeApiTimesOut()
        {
            // Arrange
            var agent = new ThrowingAgent(
                new TaskCanceledException("Timeout", new TimeoutException("The operation timed out")));
            var messages = CreateMessages("Show me my activity");

            // Act
            var updates = new List<AgentResponseUpdate>();
            await foreach (var update in _sut.HandleAsync(messages, null, null, agent, CancellationToken.None))
            {
                updates.Add(update);
            }

            // Assert
            updates.Should().ContainSingle();
            updates[0].Contents.OfType<TextContent>().First().Text
                .Should().Be(GracefulDegradationMiddleware.TimeoutMessage);
        }

        [Fact]
        public async Task HandleAsync_ShouldLogWarning_WhenClaudeApiIsUnavailable()
        {
            // Arrange
            var agent = new ThrowingAgent(
                new HttpRequestException("Service Unavailable", null, HttpStatusCode.ServiceUnavailable));
            var messages = CreateMessages("Show me my activity");

            // Act
            await foreach (var _ in _sut.HandleAsync(messages, null, null, agent, CancellationToken.None)) { }

            // Assert
            _loggerMock.VerifyLog(l => l.LogWarning(
                It.IsAny<HttpRequestException>(),
                It.Is<string>(s => s.Contains("Claude API unavailable"))),
                Times.Once);
        }

        [Fact]
        public async Task HandleAsync_ShouldLogWarning_WhenClaudeApiTimesOut()
        {
            // Arrange
            var agent = new ThrowingAgent(
                new TaskCanceledException("Timeout", new TimeoutException("The operation timed out")));
            var messages = CreateMessages("Show me my activity");

            // Act
            await foreach (var _ in _sut.HandleAsync(messages, null, null, agent, CancellationToken.None)) { }

            // Assert
            _loggerMock.VerifyLog(l => l.LogWarning(
                It.IsAny<TaskCanceledException>(),
                It.Is<string>(s => s.Contains("Claude API timed out"))),
                Times.Once);
        }

        [Fact]
        public async Task HandleAsync_ShouldNotCatch_WhenHttpExceptionHasNonRetryableStatusCode()
        {
            // Arrange — 400 Bad Request should not be caught
            var agent = new ThrowingAgent(
                new HttpRequestException("Bad Request", null, HttpStatusCode.BadRequest));
            var messages = CreateMessages("Show me my activity");

            // Act & Assert
            var act = async () =>
            {
                await foreach (var _ in _sut.HandleAsync(messages, null, null, agent, CancellationToken.None)) { }
            };

            await act.Should().ThrowAsync<HttpRequestException>();
        }

        [Fact]
        public async Task HandleAsync_ShouldNotCatch_WhenTaskCancelledWithoutTimeoutInner()
        {
            // Arrange — plain cancellation (not timeout) should propagate
            var agent = new ThrowingAgent(new TaskCanceledException("Cancelled"));
            var messages = CreateMessages("Show me my activity");

            // Act & Assert
            var act = async () =>
            {
                await foreach (var _ in _sut.HandleAsync(messages, null, null, agent, CancellationToken.None)) { }
            };

            await act.Should().ThrowAsync<TaskCanceledException>();
        }

        /// <summary>
        /// Fake agent that yields content normally (no exceptions).
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

        /// <summary>
        /// Fake agent that throws an exception during streaming.
        /// </summary>
        private sealed class ThrowingAgent(Exception exception) : AIAgent
        {
            protected override Task<AgentResponse> RunCoreAsync(
                IEnumerable<ChatMessage> messages,
                AgentSession session,
                AgentRunOptions options,
                CancellationToken cancellationToken)
            {
                throw exception;
            }

            protected override async IAsyncEnumerable<AgentResponseUpdate> RunCoreStreamingAsync(
                IEnumerable<ChatMessage> messages,
                AgentSession session,
                AgentRunOptions options,
                [EnumeratorCancellation] CancellationToken cancellationToken)
            {
                await Task.CompletedTask;
                throw exception;
#pragma warning disable CS0162 // Unreachable code detected
                yield break;
#pragma warning restore CS0162
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
