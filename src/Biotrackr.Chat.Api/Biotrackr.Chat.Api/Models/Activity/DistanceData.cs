using System.Text.Json.Serialization;

namespace Biotrackr.Chat.Api.Models.Activity;

public class DistanceData
{
    [JsonPropertyName("activity")]
    public string Activity { get; set; } = string.Empty;

    [JsonPropertyName("distance")]
    public double Distance { get; set; }
}
