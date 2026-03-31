using System.Text.Json.Serialization;

namespace Biotrackr.Mcp.Server.Models.Sleep
{
    public class SleepItem
    {
        [JsonIgnore]
        [JsonPropertyName("id")]
        public string Id { get; set; } = string.Empty;

        [JsonPropertyName("sleep")]
        public SleepData Sleep { get; set; } = new();

        [JsonPropertyName("date")]
        public string Date { get; set; } = string.Empty;

        [JsonIgnore]
        [JsonPropertyName("documentType")]
        public string DocumentType { get; set; } = string.Empty;
    }
}
