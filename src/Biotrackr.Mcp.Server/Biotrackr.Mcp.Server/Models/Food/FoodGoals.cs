using System.Text.Json.Serialization;

namespace Biotrackr.Mcp.Server.Models.Food
{
    public class FoodGoals
    {
        [JsonPropertyName("calories")]
        public int Calories { get; set; }
    }
}
