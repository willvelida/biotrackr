using System.Text.Json.Serialization;

namespace Biotrackr.Activity.Api.Models;

/// <summary>
/// One exercise session from Google Health. Times are the user's local time as ISO-8601 without an offset.
/// </summary>
public class Workout
{
    [JsonPropertyName("name")]
    public string? Name { get; set; }

    [JsonPropertyName("exerciseType")]
    public string? ExerciseType { get; set; }

    [JsonPropertyName("startTime")]
    public string? StartTime { get; set; }

    [JsonPropertyName("endTime")]
    public string? EndTime { get; set; }

    [JsonPropertyName("durationMinutes")]
    public int DurationMinutes { get; set; }

    [JsonPropertyName("calories")]
    public int? Calories { get; set; }

    [JsonPropertyName("steps")]
    public int? Steps { get; set; }

    [JsonPropertyName("distanceKm")]
    public double? DistanceKm { get; set; }

    [JsonPropertyName("averageHeartRate")]
    public int? AverageHeartRate { get; set; }

    [JsonPropertyName("activeZoneMinutes")]
    public int? ActiveZoneMinutes { get; set; }
}
