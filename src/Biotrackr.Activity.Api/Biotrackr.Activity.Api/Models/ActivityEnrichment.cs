using System.Text.Json.Serialization;

namespace Biotrackr.Activity.Api.Models;

/// <summary>
/// Google-era additions to an activity day (plan contract C2). Null for version 1 documents.
/// </summary>
public class ActivityEnrichment
{
    /// <summary>Active zone minutes for the day; null when Google returned no rollup for the date.</summary>
    [JsonPropertyName("activeZoneMinutes")]
    public ActiveZoneMinutes? ActiveZoneMinutes { get; set; }

    [JsonPropertyName("workouts")]
    public List<Workout> Workouts { get; set; } = [];
}
