using System.Text.Json.Serialization;

namespace Biotrackr.Chat.Api.Models.Sleep;

public class SleepSummary
{
    [JsonPropertyName("stages")]
    public SleepStages Stages { get; set; } = new();

    [JsonPropertyName("totalMinutesAsleep")]
    public int TotalMinutesAsleep { get; set; }

    [JsonPropertyName("totalSleepRecords")]
    public int TotalSleepRecords { get; set; }

    [JsonPropertyName("totalTimeInBed")]
    public int TotalTimeInBed { get; set; }
}
