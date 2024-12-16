using System.Text.Json.Serialization;

namespace Biotrackr.Sleep.Svc.Models.FitbitEntities
{
    public class SleepDetails
    {
        [JsonPropertyName("count")]
        public int Count { get; set; }
        [JsonPropertyName("minutes")]
        public int Minutes { get; set; }
        [JsonPropertyName("thirtyDayAvgMinutes")]
        public int ThirtyDayAvgMinutes { get; set; }
    }
}
