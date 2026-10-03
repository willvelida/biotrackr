using System.Text.Json.Serialization;

namespace Biotrackr.Food.Api.Models.FitbitEntities;

public class FoodResponse
{
    [JsonPropertyName("foods")]
    public List<Food> Foods { get; set; } = new();

    [JsonPropertyName("goals")]
    public Goals? Goals { get; set; } = new();

    [JsonPropertyName("summary")]
    public Summary Summary { get; set; } = new();
}
