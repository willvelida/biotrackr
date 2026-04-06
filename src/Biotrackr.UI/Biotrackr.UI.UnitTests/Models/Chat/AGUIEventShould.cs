using System.Text.Json;
using Biotrackr.UI.Models.Chat;
using FluentAssertions;

namespace Biotrackr.UI.UnitTests.Models.Chat
{
    public class AGUIEventShould
    {
        private static readonly JsonSerializerOptions JsonOptions = new()
        {
            PropertyNameCaseInsensitive = true
        };

        [Fact]
        public void DeserializeToolCallResultEvent()
        {
            var json = """
                {
                    "type": "TOOL_CALL_RESULT",
                    "toolCallId": "call_abc123",
                    "content": "Report generation started. Job ID: 7f3e2a1b-4c5d-6e7f-8a9b-0c1d2e3f4a5b. You can ask me to check on the status of this report.",
                    "role": "tool"
                }
                """;

            var evt = JsonSerializer.Deserialize<AGUIEvent>(json, JsonOptions);

            evt.Should().NotBeNull();
            evt!.Type.Should().Be("TOOL_CALL_RESULT");
            evt.ToolCallId.Should().Be("call_abc123");
            evt.Content.Should().Contain("Job ID: 7f3e2a1b-4c5d-6e7f-8a9b-0c1d2e3f4a5b");
            evt.Role.Should().Be("tool");
        }

        [Fact]
        public void DeserializeToolCallStartEvent()
        {
            var json = """
                {
                    "type": "TOOL_CALL_START",
                    "toolCallId": "call_xyz789",
                    "toolCallName": "RequestReport",
                    "delta": "RequestReport"
                }
                """;

            var evt = JsonSerializer.Deserialize<AGUIEvent>(json, JsonOptions);

            evt.Should().NotBeNull();
            evt!.Type.Should().Be("TOOL_CALL_START");
            evt.ToolCallId.Should().Be("call_xyz789");
            evt.ToolCallName.Should().Be("RequestReport");
            evt.Delta.Should().Be("RequestReport");
        }

        [Fact]
        public void DeserializeTextMessageContentEvent_WithoutNewFields()
        {
            var json = """
                {
                    "type": "TEXT_MESSAGE_CONTENT",
                    "delta": "Hello, here is your report."
                }
                """;

            var evt = JsonSerializer.Deserialize<AGUIEvent>(json, JsonOptions);

            evt.Should().NotBeNull();
            evt!.Type.Should().Be("TEXT_MESSAGE_CONTENT");
            evt.Delta.Should().Be("Hello, here is your report.");
            evt.ToolCallId.Should().BeNull();
            evt.ToolCallName.Should().BeNull();
            evt.Content.Should().BeNull();
        }

        [Fact]
        public void DeserializeRunStartedEvent()
        {
            var json = """
                {
                    "type": "RUN_STARTED",
                    "threadId": "thread-123",
                    "runId": "run-456"
                }
                """;

            var evt = JsonSerializer.Deserialize<AGUIEvent>(json, JsonOptions);

            evt.Should().NotBeNull();
            evt!.Type.Should().Be("RUN_STARTED");
            evt.ThreadId.Should().Be("thread-123");
            evt.RunId.Should().Be("run-456");
        }
    }
}
