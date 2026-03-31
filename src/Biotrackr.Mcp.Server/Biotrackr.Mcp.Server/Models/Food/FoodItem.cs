using System.Text.Json.Serialization;

namespace Biotrackr.Mcp.Server.Models.Food
{
    public class FoodItem
    {
        [JsonIgnore]
        [JsonPropertyName("id")]
        public string Id { get; set; } = string.Empty;

        [JsonPropertyName("food")]
        public FoodData Food { get; set; } = new();

        [JsonPropertyName("date")]
        public string Date { get; set; } = string.Empty;

        [JsonIgnore]
        [JsonPropertyName("documentType")]
        public string DocumentType { get; set; } = string.Empty;
    }
}
