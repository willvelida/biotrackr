using System.Text.Json.Serialization;

namespace Biotrackr.Chat.Api.Models.Activity;

public class ActivityData
{
    [JsonPropertyName("activities")]
    public List<ActivityLog> Activities { get; set; } = [];

    [JsonPropertyName("goals")]
    public ActivityGoals Goals { get; set; } = new();

    [JsonPropertyName("summary")]
    public ActivitySummary Summary { get; set; } = new();
}
