using System.Text.Json.Serialization;

namespace Biotrackr.Chat.Api.Models.Activity;

public class ActivityGoals
{
    [JsonPropertyName("activeMinutes")]
    public int ActiveMinutes { get; set; }

    [JsonPropertyName("caloriesOut")]
    public int CaloriesOut { get; set; }

    [JsonPropertyName("distance")]
    public double Distance { get; set; }

    [JsonPropertyName("floors")]
    public int Floors { get; set; }

    [JsonPropertyName("steps")]
    public int Steps { get; set; }
}
