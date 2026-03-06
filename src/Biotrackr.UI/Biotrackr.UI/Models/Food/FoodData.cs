using System.Text.Json.Serialization;

namespace Biotrackr.UI.Models.Food
{
    public class FoodData
    {
        [JsonPropertyName("foods")]
        public List<FoodEntry> Foods { get; set; } = [];

        [JsonPropertyName("goals")]
        public FoodGoals Goals { get; set; } = new();

        [JsonPropertyName("summary")]
        public FoodSummary Summary { get; set; } = new();
    }
}
