using System.Text.Json.Serialization;

namespace Biotrackr.Sleep.Svc.Models.FitbitEntities
{
    public class Summary
    {
        [JsonPropertyName("deep")]
        public SleepDetails Deep { get; set; }
        [JsonPropertyName("light")]
        public SleepDetails Light { get; set; }
        [JsonPropertyName("rem")]
        public SleepDetails Rem { get; set; }
        [JsonPropertyName("wake")]
        public SleepDetails Wake { get; set; }
        [JsonPropertyName("stages")]
        public Stages Stages { get; set; }
        [JsonPropertyName("totalMinutesAsleep")]
        public int TotalMinutesAsleep { get; set; }
        [JsonPropertyName("totalSleepRecords")]
        public int TotalSleepRecords { get; set; }
        [JsonPropertyName("totalTimeInBed")]
        public int TotalTimeInBed { get; set; }
    }
}
