using System.Text.Json.Serialization;

namespace Biotrackr.UI.Models.Chat
{
    public class ChatConversationSummary
    {
        [JsonPropertyName("sessionId")]
        public string SessionId { get; set; } = string.Empty;

        [JsonPropertyName("title")]
        public string Title { get; set; } = string.Empty;

        [JsonPropertyName("lastUpdated")]
        public DateTime LastUpdated { get; set; }
    }
}
