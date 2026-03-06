using System.Text.Json.Serialization;

namespace Biotrackr.UI.Models.Sleep
{
    public class SleepLevels
    {
        [JsonPropertyName("data")]
        public List<SleepLevelData> Data { get; set; } = [];

        [JsonPropertyName("shortData")]
        public List<SleepLevelData> ShortData { get; set; } = [];

        [JsonPropertyName("summary")]
        public SleepSummary Summary { get; set; } = new();
    }
}
