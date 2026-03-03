using System.Text.Json.Serialization;

namespace Biotrackr.Mcp.Server.Models.Sleep
{
    public class SleepDetails
    {
        [JsonPropertyName("count")]
        public int Count { get; set; }

        [JsonPropertyName("minutes")]
        public int Minutes { get; set; }

        [JsonPropertyName("thirtyDayAvgMinutes")]
        public int ThirtyDayAvgMinutes { get; set; }
    }
}
