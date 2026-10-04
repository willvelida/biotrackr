using System.Text.Json.Serialization;

namespace Biotrackr.Food.Api.Models.FitbitEntities;

public class Unit
{
    [JsonPropertyName("id")]
    public int? Id { get; set; } = 0;

    [JsonPropertyName("name")]
    public string? Name { get; set; } = string.Empty;

    [JsonPropertyName("plural")]
    public string? Plural { get; set; } = string.Empty;
}
