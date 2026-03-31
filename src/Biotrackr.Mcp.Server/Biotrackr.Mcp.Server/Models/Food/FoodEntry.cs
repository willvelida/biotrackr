using System.Text.Json.Serialization;

namespace Biotrackr.Mcp.Server.Models.Food
{
    public class FoodEntry
    {
        [JsonIgnore]
        [JsonPropertyName("isFavorite")]
        public bool IsFavorite { get; set; }

        [JsonPropertyName("logDate")]
        public string LogDate { get; set; } = string.Empty;

        [JsonIgnore]
        [JsonPropertyName("logId")]
        public long LogId { get; set; }

        [JsonPropertyName("loggedFood")]
        public LoggedFood LoggedFood { get; set; } = new();

        [JsonPropertyName("nutritionalValues")]
        public NutritionalValues NutritionalValues { get; set; } = new();
    }
}
