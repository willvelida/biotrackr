using System.Text.Json.Serialization;

namespace Biotrackr.Sleep.Svc.Models.FitbitEntities
{
    public class Summary
    {
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
