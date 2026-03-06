using System.Text.Json.Serialization;

namespace Biotrackr.UI.Models.Food
{
    public class FoodGoals
    {
        [JsonPropertyName("calories")]
        public int Calories { get; set; }
    }
}
