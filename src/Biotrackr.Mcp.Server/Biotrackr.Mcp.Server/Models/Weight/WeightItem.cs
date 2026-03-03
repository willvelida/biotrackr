using System.Text.Json.Serialization;

namespace Biotrackr.Mcp.Server.Models.Weight
{
    public class WeightItem
    {
        [JsonPropertyName("id")]
        public string Id { get; set; } = string.Empty;

        [JsonPropertyName("weight")]
        public WeightData Weight { get; set; } = new();

        [JsonPropertyName("date")]
        public string Date { get; set; } = string.Empty;

        [JsonPropertyName("documentType")]
        public string DocumentType { get; set; } = string.Empty;
    }
}
