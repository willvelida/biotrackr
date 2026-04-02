using System.Text.Json.Serialization;

namespace Biotrackr.Weight.Svc.Models.WithingsEntities
{
    public class Measure
    {
        [JsonPropertyName("value")]
        public long Value { get; set; }

        [JsonPropertyName("type")]
        public int Type { get; set; }

        [JsonPropertyName("unit")]
        public int Unit { get; set; }
    }
}
