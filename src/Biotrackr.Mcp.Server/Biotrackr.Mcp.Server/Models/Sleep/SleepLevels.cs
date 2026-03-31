using System.Text.Json.Serialization;

namespace Biotrackr.Mcp.Server.Models.Sleep
{
    public class SleepLevels
    {
        [JsonIgnore]
        [JsonPropertyName("data")]
        public List<SleepLevelData> Data { get; set; } = [];

        [JsonIgnore]
        [JsonPropertyName("shortData")]
        public List<SleepLevelData> ShortData { get; set; } = [];

        [JsonPropertyName("summary")]
        public SleepSummary Summary { get; set; } = new();
    }
}
