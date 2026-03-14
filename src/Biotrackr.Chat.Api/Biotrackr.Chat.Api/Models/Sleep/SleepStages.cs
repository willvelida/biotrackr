using System.Text.Json.Serialization;

namespace Biotrackr.Chat.Api.Models.Sleep;

public class SleepStages
{
    [JsonPropertyName("deep")]
    public int Deep { get; set; }

    [JsonPropertyName("light")]
    public int Light { get; set; }

    [JsonPropertyName("rem")]
    public int Rem { get; set; }

    [JsonPropertyName("wake")]
    public int Wake { get; set; }
}
