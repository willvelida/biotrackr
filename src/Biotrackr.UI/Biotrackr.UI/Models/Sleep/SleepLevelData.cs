using System.Text.Json.Serialization;

namespace Biotrackr.UI.Models.Sleep
{
    public class SleepLevelData
    {
        [JsonPropertyName("dateTime")]
        public DateTime DateTime { get; set; }

        [JsonPropertyName("level")]
        public string Level { get; set; } = string.Empty;

        [JsonPropertyName("seconds")]
        public int Seconds { get; set; }
    }
}
