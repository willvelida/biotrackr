using System.Text.Json.Serialization;

namespace Biotrackr.Chat.Api.Models.Activity;

public class ActivityItem
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    [JsonPropertyName("activity")]
    public ActivityData Activity { get; set; } = new();

    [JsonPropertyName("date")]
    public string Date { get; set; } = string.Empty;

    [JsonPropertyName("documentType")]
    public string DocumentType { get; set; } = string.Empty;
}
