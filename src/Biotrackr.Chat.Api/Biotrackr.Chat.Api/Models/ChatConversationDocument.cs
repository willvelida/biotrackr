using System.Text.Json.Serialization;

namespace Biotrackr.Chat.Api.Models
{
    public class ChatConversationDocument
    {
        [JsonPropertyName("id")]
        public string Id { get; set; } = Guid.NewGuid().ToString();

        [JsonPropertyName("sessionId")]
        public string SessionId { get; set; } = string.Empty;

        [JsonPropertyName("title")]
        public string Title { get; set; } = string.Empty;

        [JsonPropertyName("lastUpdated")]
        public DateTime LastUpdated { get; set; } = DateTime.UtcNow;

        [JsonPropertyName("messages")]
        public List<ChatMessage> Messages { get; set; } = [];

        /// <summary>
        /// Cosmos DB per-document TTL in seconds. Refreshed on every upsert.
        /// </summary>
        [JsonPropertyName("ttl")]
        public int Ttl { get; set; } = 7776000;
    }
}
