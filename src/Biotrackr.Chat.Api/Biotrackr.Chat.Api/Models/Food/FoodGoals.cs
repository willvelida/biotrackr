using System.Text.Json.Serialization;

namespace Biotrackr.Chat.Api.Models.Food;

public class FoodGoals
{
    [JsonPropertyName("calories")]
    public int Calories { get; set; }
}
