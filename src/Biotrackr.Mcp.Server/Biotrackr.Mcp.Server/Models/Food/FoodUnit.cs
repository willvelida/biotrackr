using System.Text.Json.Serialization;

namespace Biotrackr.Mcp.Server.Models.Food
{
    public class FoodUnit
    {
        [JsonIgnore]
        [JsonPropertyName("id")]
        public int Id { get; set; }

        [JsonPropertyName("name")]
        public string Name { get; set; } = string.Empty;

        [JsonIgnore]
        [JsonPropertyName("plural")]
        public string Plural { get; set; } = string.Empty;
    }
}
