using System.Text.Json.Serialization;

namespace Biotrackr.Chat.Api.Models
{
    public class ChatMessage
    {
        [JsonPropertyName("role")]
        public string Role { get; set; } = string.Empty;

        [JsonPropertyName("content")]
        public string Content { get; set; } = string.Empty;

        [JsonPropertyName("timestamp")]
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;

        [JsonPropertyName("toolCalls")]
        public List<string>? ToolCalls { get; set; }
    }
}
