using System.Text.Json.Serialization;

namespace Biotrackr.Mcp.Server.Models.Food
{
    public class LoggedFood
    {
        [JsonPropertyName("accessLevel")]
        public string AccessLevel { get; set; } = string.Empty;

        [JsonPropertyName("amount")]
        public int Amount { get; set; }

        [JsonPropertyName("brand")]
        public string Brand { get; set; } = string.Empty;

        [JsonPropertyName("calories")]
        public int Calories { get; set; }

        [JsonPropertyName("foodId")]
        public int FoodId { get; set; }

        [JsonPropertyName("locale")]
        public string Locale { get; set; } = string.Empty;

        [JsonPropertyName("mealTypeId")]
        public int MealTypeId { get; set; }

        [JsonPropertyName("name")]
        public string Name { get; set; } = string.Empty;

        [JsonPropertyName("unit")]
        public FoodUnit Unit { get; set; } = new();

        [JsonPropertyName("units")]
        public List<int> Units { get; set; } = [];
    }
}
