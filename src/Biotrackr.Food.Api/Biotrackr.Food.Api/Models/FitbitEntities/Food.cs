using System.Text.Json.Serialization;

namespace Biotrackr.Food.Api.Models.FitbitEntities;

public class Food
{
    [JsonPropertyName("isFavorite")]
    public bool? IsFavorite { get; set; } = false;

    [JsonPropertyName("logDate")]
    public string LogDate { get; set; } = string.Empty;

    [JsonPropertyName("logId")]
    public long? LogId { get; set; } = 0;

    [JsonPropertyName("loggedFood")]
    public LoggedFood LoggedFood { get; set; } = new();

    [JsonPropertyName("nutritionalValues")]
    public NutritionalValues NutritionalValues { get; set; } = new();
}
