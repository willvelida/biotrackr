using System.Text.Json.Serialization;

namespace Biotrackr.Mcp.Server.Models.Food
{
    public class LoggedFood
    {
        [JsonIgnore]
        [JsonPropertyName("accessLevel")]
        public string AccessLevel { get; set; } = string.Empty;

        [JsonPropertyName("amount")]
        public int Amount { get; set; }

        [JsonPropertyName("brand")]
        public string Brand { get; set; } = string.Empty;

        [JsonPropertyName("calories")]
        public int Calories { get; set; }

        [JsonIgnore]
        [JsonPropertyName("foodId")]
        public int FoodId { get; set; }

        [JsonIgnore]
        [JsonPropertyName("locale")]
        public string Locale { get; set; } = string.Empty;

        [JsonIgnore]
        [JsonPropertyName("mealTypeId")]
        public int MealTypeId { get; set; }

        [JsonPropertyName("name")]
        public string Name { get; set; } = string.Empty;

        [JsonPropertyName("unit")]
        public FoodUnit Unit { get; set; } = new();

        [JsonIgnore]
        [JsonPropertyName("units")]
        public List<int> Units { get; set; } = [];
    }
}
