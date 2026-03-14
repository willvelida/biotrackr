using System.Text.Json.Serialization;

namespace Biotrackr.Chat.Api.Models.Food;

public class FoodItem
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    [JsonPropertyName("food")]
    public FoodData Food { get; set; } = new();

    [JsonPropertyName("date")]
    public string Date { get; set; } = string.Empty;

    [JsonPropertyName("documentType")]
    public string DocumentType { get; set; } = string.Empty;
}
