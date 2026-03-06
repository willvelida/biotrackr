using System.Text.Json.Serialization;

namespace Biotrackr.UI.Models.Food
{
    public class FoodUnit
    {
        [JsonPropertyName("id")]
        public int Id { get; set; }

        [JsonPropertyName("name")]
        public string Name { get; set; } = string.Empty;

        [JsonPropertyName("plural")]
        public string Plural { get; set; } = string.Empty;
    }
}
