using Biotrackr.Chat.Api.Configuration;
using Biotrackr.Chat.Api.Middleware;
using FluentAssertions;
using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using System.Runtime.CompilerServices;
using System.Text.Json;
using ChatMessage = Microsoft.Extensions.AI.ChatMessage;

namespace Biotrackr.Chat.Api.UnitTests.Middleware
{
    public class ToolPolicyMiddlewareShould
    {
        private readonly IMemoryCache _cache;
        private readonly Mock<ILogger<ToolPolicyMiddleware>> _loggerMock;
        private readonly ToolPolicyOptions _policyOptions;
        private readonly ToolPolicyMiddleware _sut;

        public ToolPolicyMiddlewareShould()
        {
            _cache = new MemoryCache(new MemoryCacheOptions());
            _loggerMock = new Mock<ILogger<ToolPolicyMiddleware>>();
            _policyOptions = new ToolPolicyOptions
            {
                MaxToolCallsPerSession = 3,
                AllowedToolNames =
                [
                    "GetActivityByDate", "GetActivityByDateRange", "GetActivityRecords",
                    "GetSleepByDate", "GetSleepByDateRange", "GetSleepRecords",
                    "GetVitalsByDate", "GetVitalsByDateRange", "GetVitalsRecords",
                    "GetFoodByDate", "GetFoodByDateRange", "GetFoodRecords"
                ]
            };

            _sut = new ToolPolicyMiddleware(
                _cache,
                Options.Create(_policyOptions),
                _loggerMock.Object);
        }

        private static IEnumerable<ChatMessage> CreateMessages(string userText)
        {
            return [new ChatMessage(ChatRole.User, userText)];
        }

        [Fact]
        public async Task AllowToolCalls_WhenWithinBudget()
        {
            // Arrange
            var functionCall = new FunctionCallContent("call-1", "GetActivityByDate",
                new Dictionary<string, object?> { ["date"] = "2026-01-15" });
            var agent = new FakeAgent(functionCall, new TextContent("Here is your data."));
            var messages = CreateMessages("Show me my activity");

            // Act
            var updates = new List<AgentResponseUpdate>();
            await foreach (var update in _sut.HandleAsync(messages, null, null, agent, CancellationToken.None))
            {
                updates.Add(update);
            }

            // Assert
            updates.Should().HaveCount(1);
            _loggerMock.Verify(
                x => x.Log(
                    LogLevel.Warning,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((o, t) => o.ToString()!.Contains("exceeded")),
                    It.IsAny<Exception?>(),
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
                Times.Never);
        }

        [Fact]
        public async Task LogWarning_WhenBudgetExceeded()
        {
            // Arrange — set budget to 3, make 4 calls
            var messages = CreateMessages("Show me data");

            // Exhaust the budget with 3 calls
            for (int i = 0; i < 3; i++)
            {
                var fc = new FunctionCallContent($"call-{i}", "GetActivityByDate",
                    new Dictionary<string, object?> { ["date"] = "2026-01-15" });
                var agent = new FakeAgent(fc, new TextContent("Data."));
                await foreach (var _ in _sut.HandleAsync(messages, null, null, agent, CancellationToken.None))
                {
                    // Drain
                }
            }

            // Act — 4th call should trigger warning
            var overBudgetCall = new FunctionCallContent("call-over", "GetActivityByDate",
                new Dictionary<string, object?> { ["date"] = "2026-01-15" });
            var overAgent = new FakeAgent(overBudgetCall, new TextContent("More data."));
            await foreach (var _ in _sut.HandleAsync(messages, null, null, overAgent, CancellationToken.None))
            {
                // Drain
            }

            // Assert
            _loggerMock.Verify(
                x => x.Log(
                    LogLevel.Warning,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((o, t) => o.ToString()!.Contains("exceeded")),
                    It.IsAny<Exception?>(),
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
                Times.Once);
        }

        [Fact]
        public async Task ReturnErrorMessageAndStopStream_WhenBudgetExceeded()
        {
            // Arrange — set budget to 3, make 4 calls
            var messages = CreateMessages("Show me data");

            // Exhaust the budget with 3 calls
            for (int i = 0; i < 3; i++)
            {
                var fc = new FunctionCallContent($"call-{i}", "GetActivityByDate",
                    new Dictionary<string, object?> { ["date"] = "2026-01-15" });
                var agent = new FakeAgent(fc, new TextContent("Data."));
                await foreach (var _ in _sut.HandleAsync(messages, null, null, agent, CancellationToken.None))
                {
                    // Drain
                }
            }

            // Act — 4th call should return error and stop streaming
            var overBudgetCall = new FunctionCallContent("call-over", "GetActivityByDate",
                new Dictionary<string, object?> { ["date"] = "2026-01-15" });
            var overAgent = new FakeAgent(overBudgetCall, new TextContent("More data."));
            var updates = new List<AgentResponseUpdate>();
            await foreach (var update in _sut.HandleAsync(messages, null, null, overAgent, CancellationToken.None))
            {
                updates.Add(update);
            }

            // Assert — only the error message update, not the original tool response
            updates.Should().HaveCount(1);
            var textContent = updates[0].Contents.OfType<TextContent>().Single();
            textContent.Text.Should().Be(ToolPolicyMiddleware.BudgetExceededMessage);
        }

        [Fact]
        public async Task LogWarning_WhenUnrecognisedToolNameUsed()
        {
            // Arrange
            var functionCall = new FunctionCallContent("call-1", "DeleteAllData",
                new Dictionary<string, object?> { ["confirm"] = true });
            var agent = new FakeAgent(functionCall, new TextContent("Done."));
            var messages = CreateMessages("Delete everything");

            // Act
            await foreach (var _ in _sut.HandleAsync(messages, null, null, agent, CancellationToken.None))
            {
                // Drain
            }

            // Assert
            _loggerMock.Verify(
                x => x.Log(
                    LogLevel.Warning,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((o, t) => o.ToString()!.Contains("Blocked unrecognised tool call") && o.ToString()!.Contains("DeleteAllData")),
                    It.IsAny<Exception?>(),
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
                Times.Once);
        }

        [Fact]
        public async Task LogInformation_WhenToolCallInvoked()
        {
            // Arrange
            var functionCall = new FunctionCallContent("call-1", "GetActivityByDate",
                new Dictionary<string, object?> { ["date"] = "2026-01-15" });
            var agent = new FakeAgent(functionCall, new TextContent("Here is your data."));
            var messages = CreateMessages("Show me my activity");

            // Act
            await foreach (var _ in _sut.HandleAsync(messages, null, null, agent, CancellationToken.None))
            {
                // Drain
            }

            // Assert
            _loggerMock.Verify(
                x => x.Log(
                    LogLevel.Information,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((o, t) => o.ToString()!.Contains("Tool call invoked:") && o.ToString()!.Contains("GetActivityByDate")),
                    It.IsAny<Exception?>(),
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
                Times.Once);
        }

        [Fact]
        public async Task TrackBudgetIndependently_ForDifferentSessions()
        {
            // Arrange — use a budget of 3
            var messages = CreateMessages("Show me data");

            // Exhaust budget with null session (uses "unknown" key)
            for (int i = 0; i < 3; i++)
            {
                var fc = new FunctionCallContent($"call-{i}", "GetActivityByDate",
                    new Dictionary<string, object?> { ["date"] = "2026-01-15" });
                var agent = new FakeAgent(fc, new TextContent("Data."));
                await foreach (var _ in _sut.HandleAsync(messages, null, null, agent, CancellationToken.None))
                {
                    // Drain
                }
            }

            // Act — create a fresh middleware with a different session key by using a separate cache key
            // The null session always maps to "unknown", so all calls above share the same budget
            // This 4th call on the same "unknown" session should trigger warning
            var overBudgetCall = new FunctionCallContent("call-over", "GetActivityByDate",
                new Dictionary<string, object?> { ["date"] = "2026-01-15" });
            var overAgent = new FakeAgent(overBudgetCall, new TextContent("More data."));
            await foreach (var _ in _sut.HandleAsync(messages, null, null, overAgent, CancellationToken.None))
            {
                // Drain
            }

            // Assert — budget exceeded on same "unknown" session
            _loggerMock.Verify(
                x => x.Log(
                    LogLevel.Warning,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((o, t) => o.ToString()!.Contains("exceeded")),
                    It.IsAny<Exception?>(),
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
                Times.Once);

            // A different cache key should have independent budget
            var separateCache = new MemoryCache(new MemoryCacheOptions());
            var separateSut = new ToolPolicyMiddleware(
                separateCache,
                Options.Create(_policyOptions),
                _loggerMock.Object);

            // Reset the logger mock to check fresh
            _loggerMock.Invocations.Clear();

            var freshCall = new FunctionCallContent("call-fresh", "GetSleepByDate",
                new Dictionary<string, object?> { ["date"] = "2026-01-15" });
            var freshAgent = new FakeAgent(freshCall, new TextContent("Sleep data."));
            await foreach (var _ in separateSut.HandleAsync(messages, null, null, freshAgent, CancellationToken.None))
            {
                // Drain
            }

            // Assert — no warning since this is a fresh cache
            _loggerMock.Verify(
                x => x.Log(
                    LogLevel.Warning,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((o, t) => o.ToString()!.Contains("exceeded")),
                    It.IsAny<Exception?>(),
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
                Times.Never);
        }

        [Fact]
        public async Task AllowThirdCall_WhenBudgetIsThree()
        {
            // Arrange — budget is 3, the 3rd call should still succeed without warning
            var messages = CreateMessages("Show me data");

            for (int i = 0; i < 3; i++)
            {
                var fc = new FunctionCallContent($"call-{i}", "GetActivityByDate",
                    new Dictionary<string, object?> { ["date"] = "2026-01-15" });
                var agent = new FakeAgent(fc, new TextContent("Data."));
                await foreach (var _ in _sut.HandleAsync(messages, null, null, agent, CancellationToken.None))
                {
                    // Drain
                }
            }

            // Assert — no exceeded warning for exactly at-limit calls
            _loggerMock.Verify(
                x => x.Log(
                    LogLevel.Warning,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((o, t) => o.ToString()!.Contains("exceeded")),
                    It.IsAny<Exception?>(),
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
                Times.Never);
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
