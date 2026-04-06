using System.Text.Json.Serialization;

namespace Biotrackr.UI.Models.Chat
{
    /// <summary>
    /// Represents a single AG-UI protocol SSE event from the Chat API.
    /// The MapAGUI endpoint streams events with typed "type" fields.
    /// </summary>
    public class AGUIEvent
    {
        [JsonPropertyName("type")]
        public string Type { get; set; } = string.Empty;

        [JsonPropertyName("messageId")]
        public string? MessageId { get; set; }

        [JsonPropertyName("delta")]
        public string? Delta { get; set; }

        [JsonPropertyName("role")]
        public string? Role { get; set; }

        [JsonPropertyName("threadId")]
        public string? ThreadId { get; set; }

        [JsonPropertyName("runId")]
        public string? RunId { get; set; }

        [JsonPropertyName("toolCallId")]
        public string? ToolCallId { get; set; }

        [JsonPropertyName("toolCallName")]
        public string? ToolCallName { get; set; }

        [JsonPropertyName("content")]
        public string? Content { get; set; }
    }
}
