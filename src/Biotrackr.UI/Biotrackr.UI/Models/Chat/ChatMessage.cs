using System.Text.Json.Serialization;

namespace Biotrackr.UI.Models.Chat
{
    public class ChatMessage
    {
        [JsonPropertyName("role")]
        public string Role { get; set; } = string.Empty;

        [JsonPropertyName("content")]
        public string Content { get; set; } = string.Empty;

        [JsonPropertyName("timestamp")]
        public DateTime Timestamp { get; set; }

        [JsonPropertyName("toolCalls")]
        public List<string>? ToolCalls { get; set; }
    }
}
