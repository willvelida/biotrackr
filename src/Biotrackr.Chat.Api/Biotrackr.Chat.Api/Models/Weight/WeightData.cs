using System.Text.Json.Serialization;

namespace Biotrackr.Chat.Api.Models.Weight;

public class WeightData
{
    [JsonPropertyName("bmi")]
    public double Bmi { get; set; }

    [JsonPropertyName("date")]
    public string Date { get; set; } = string.Empty;

    [JsonPropertyName("fat")]
    public double Fat { get; set; }

    [JsonPropertyName("logId")]
    public object? LogId { get; set; }

    [JsonPropertyName("source")]
    public string Source { get; set; } = string.Empty;

    [JsonPropertyName("time")]
    public string Time { get; set; } = string.Empty;

    [JsonPropertyName("weight")]
    public double Weight { get; set; }
}
