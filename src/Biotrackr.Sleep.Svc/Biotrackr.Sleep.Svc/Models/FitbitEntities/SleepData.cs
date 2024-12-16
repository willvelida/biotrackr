using System.Text.Json.Serialization;

namespace Biotrackr.Sleep.Svc.Models.FitbitEntities
{
    public class SleepData
    {
        [JsonPropertyName("dateTime")]
        public DateTime DateTime { get; set; }
        [JsonPropertyName("level")]
        public string Level { get; set; }
        [JsonPropertyName("seconds")]
        public int Seconds { get; set; }
    }
}
