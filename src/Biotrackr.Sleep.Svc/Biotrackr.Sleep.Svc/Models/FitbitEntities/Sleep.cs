using System.Text.Json.Serialization;

namespace Biotrackr.Sleep.Svc.Models.FitbitEntities
{
    public class Sleep
    {
        [JsonPropertyName("dateOfSleep")]
        public string DateOfSleep { get; set; }
        [JsonPropertyName("duration")]
        public int Duration { get; set; }
        [JsonPropertyName("efficiency")]
        public int Efficiency { get; set; }
        [JsonPropertyName("endTime")]
        public DateTime EndTime { get; set; }
        [JsonPropertyName("infoCode")]
        public int InfoCode { get; set; }
        [JsonPropertyName("isMainSleep")]
        public bool IsMainSleep { get; set; }
        [JsonPropertyName("levels")]
        public Levels Levels { get; set; }
        [JsonPropertyName("logId")]
        public long LogId { get; set; }
        [JsonPropertyName("minutesAfterWakeup")]
        public int MinutesAfterWakeup { get; set; }
        [JsonPropertyName("minutesAsleep")]
        public int MinutesAsleep { get; set; }
        [JsonPropertyName("minutesAwake")]
        public int MinutesAwake { get; set; }
        [JsonPropertyName("minutesToFallAsleep")]
        public int MinutesToFallAsleep { get; set; }
        [JsonPropertyName("logType")]
        public string LogType { get; set; }
        [JsonPropertyName("startTime")]
        public DateTime StartTime { get; set; }
        [JsonPropertyName("timeInBed")]
        public int TimeInBed { get; set; }
        [JsonPropertyName("type")]
        public string Type { get; set; }
    }
}
