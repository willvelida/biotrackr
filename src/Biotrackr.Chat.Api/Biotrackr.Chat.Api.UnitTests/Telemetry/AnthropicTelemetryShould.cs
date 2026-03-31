using System.Diagnostics;
using Biotrackr.Chat.Api.Telemetry;
using FluentAssertions;

namespace Biotrackr.Chat.Api.UnitTests.Telemetry
{
    public class AnthropicTelemetryShould : IDisposable
    {
        private readonly ActivityListener _listener;

        public AnthropicTelemetryShould()
        {
            _listener = new ActivityListener
            {
                ShouldListenTo = source => source.Name == "gen_ai.anthropic",
                Sample = (ref ActivityCreationOptions<ActivityContext> _) => ActivitySamplingResult.AllDataAndRecorded
            };
            ActivitySource.AddActivityListener(_listener);
        }

        public void Dispose()
        {
            _listener.Dispose();
        }

        [Fact]
        public void StartChatActivity_ShouldCreateActivityWithGenAiAttributes()
        {
            // Arrange
            var model = "claude-sonnet-4-20250514";

            // Act
            using var activity = AnthropicTelemetry.StartChatActivity(model);

            // Assert
            activity.Should().NotBeNull();
            activity!.DisplayName.Should().Be($"chat {model}");
            activity.Kind.Should().Be(ActivityKind.Client);
            activity.GetTagItem("gen_ai.operation.name").Should().Be("chat");
            activity.GetTagItem("gen_ai.provider.name").Should().Be("anthropic");
            activity.GetTagItem("gen_ai.request.model").Should().Be(model);
            activity.GetTagItem("server.address").Should().Be("api.anthropic.com");
            activity.GetTagItem("server.port").Should().Be(443);
        }

        [Fact]
        public void StartChatActivity_ShouldIncludeAgentId_WhenProvided()
        {
            // Arrange
            var model = "claude-sonnet-4-20250514";
            var agentId = "BiotrackrChatAgent";

            // Act
            using var activity = AnthropicTelemetry.StartChatActivity(model, agentId);

            // Assert
            activity.Should().NotBeNull();
            activity!.GetTagItem("gen_ai.agents.id").Should().Be(agentId);
        }

        [Fact]
        public void StartChatActivity_ShouldNotIncludeAgentId_WhenNull()
        {
            // Arrange
            var model = "claude-sonnet-4-20250514";

            // Act
            using var activity = AnthropicTelemetry.StartChatActivity(model, agentId: null);

            // Assert
            activity.Should().NotBeNull();
            activity!.GetTagItem("gen_ai.agents.id").Should().BeNull();
        }

        [Fact]
        public void RecordResponse_ShouldSetResponseAttributes()
        {
            // Arrange
            var model = "claude-sonnet-4-20250514";
            using var activity = AnthropicTelemetry.StartChatActivity(model);

            // Act
            AnthropicTelemetry.RecordResponse(
                activity,
                responseModel: "claude-sonnet-4-20250514",
                responseId: "msg_01XFDUDYJgAACzvnptvVoYEL",
                finishReason: "end_turn",
                inputTokens: 100,
                outputTokens: 50);

            // Assert
            activity.Should().NotBeNull();
            activity!.GetTagItem("gen_ai.response.model").Should().Be("claude-sonnet-4-20250514");
            activity.GetTagItem("gen_ai.response.id").Should().Be("msg_01XFDUDYJgAACzvnptvVoYEL");
            activity.GetTagItem("gen_ai.usage.input_tokens").Should().Be(100);
            activity.GetTagItem("gen_ai.usage.output_tokens").Should().Be(50);
            activity.GetTagItem("gen_ai.response.finish_reasons").Should().BeEquivalentTo(new[] { "end_turn" });
        }

        [Fact]
        public void RecordResponse_ShouldAggregateInputTokens_WithCachedTokens()
        {
            // Arrange
            var model = "claude-sonnet-4-20250514";
            using var activity = AnthropicTelemetry.StartChatActivity(model);

            // Act
            AnthropicTelemetry.RecordResponse(
                activity,
                responseModel: model,
                responseId: "msg_123",
                finishReason: "end_turn",
                inputTokens: 100,
                outputTokens: 50,
                cacheReadInputTokens: 200,
                cacheCreationInputTokens: 300);

            // Assert — total input = 100 + 200 + 300 = 600
            activity.Should().NotBeNull();
            activity!.GetTagItem("gen_ai.usage.input_tokens").Should().Be(600);
        }

        [Fact]
        public void RecordResponse_ShouldSetCacheTokenAttributes_WhenNonZero()
        {
            // Arrange
            var model = "claude-sonnet-4-20250514";
            using var activity = AnthropicTelemetry.StartChatActivity(model);

            // Act
            AnthropicTelemetry.RecordResponse(
                activity,
                responseModel: model,
                responseId: "msg_123",
                finishReason: "end_turn",
                inputTokens: 100,
                outputTokens: 50,
                cacheReadInputTokens: 200,
                cacheCreationInputTokens: 300);

            // Assert
            activity.Should().NotBeNull();
            activity!.GetTagItem("gen_ai.usage.cache_read.input_tokens").Should().Be(200);
            activity.GetTagItem("gen_ai.usage.cache_creation.input_tokens").Should().Be(300);
        }

        [Fact]
        public void RecordResponse_ShouldNotSetCacheAttributes_WhenZero()
        {
            // Arrange
            var model = "claude-sonnet-4-20250514";
            using var activity = AnthropicTelemetry.StartChatActivity(model);

            // Act
            AnthropicTelemetry.RecordResponse(
                activity,
                responseModel: model,
                responseId: "msg_123",
                finishReason: "end_turn",
                inputTokens: 100,
                outputTokens: 50,
                cacheReadInputTokens: 0,
                cacheCreationInputTokens: 0);

            // Assert
            activity.Should().NotBeNull();
            activity!.GetTagItem("gen_ai.usage.cache_read.input_tokens").Should().BeNull();
            activity.GetTagItem("gen_ai.usage.cache_creation.input_tokens").Should().BeNull();
        }

        [Fact]
        public void RecordError_ShouldSetErrorStatus()
        {
            // Arrange
            var model = "claude-sonnet-4-20250514";
            using var activity = AnthropicTelemetry.StartChatActivity(model);
            var exception = new HttpRequestException("API unavailable");

            // Act
            AnthropicTelemetry.RecordError(activity, exception);

            // Assert
            activity.Should().NotBeNull();
            activity!.Status.Should().Be(ActivityStatusCode.Error);
            activity.StatusDescription.Should().Be("API unavailable");
            activity.GetTagItem("error.type").Should().Be("HttpRequestException");
        }

        [Fact]
        public void RecordResponse_ShouldHandleNullActivity()
        {
            // Act — should not throw
            var act = () => AnthropicTelemetry.RecordResponse(
                null,
                responseModel: "claude-sonnet-4-20250514",
                responseId: "msg_123",
                finishReason: "end_turn",
                inputTokens: 100,
                outputTokens: 50);

            // Assert
            act.Should().NotThrow();
        }

        [Fact]
        public void RecordError_ShouldHandleNullActivity()
        {
            // Act — should not throw
            var act = () => AnthropicTelemetry.RecordError(null, new Exception("test"));

            // Assert
            act.Should().NotThrow();
        }
    }
}
