using System.Text.Json.Serialization;

namespace Biotrackr.Activity.Api.Models;

public class ActiveZoneMinutes
{
    [JsonPropertyName("fatBurn")]
    public int FatBurn { get; set; }

    [JsonPropertyName("cardio")]
    public int Cardio { get; set; }

    [JsonPropertyName("peak")]
    public int Peak { get; set; }

    [JsonPropertyName("total")]
    public int Total { get; set; }
}
