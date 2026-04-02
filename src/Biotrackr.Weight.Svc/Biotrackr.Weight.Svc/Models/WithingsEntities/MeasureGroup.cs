using System.Text.Json.Serialization;

namespace Biotrackr.Weight.Svc.Models.WithingsEntities
{
    public class MeasureGroup
    {
        [JsonPropertyName("grpid")]
        public long GrpId { get; set; }

        [JsonPropertyName("attrib")]
        public int Attrib { get; set; }

        [JsonPropertyName("date")]
        public long Date { get; set; }

        [JsonPropertyName("created")]
        public long Created { get; set; }

        [JsonPropertyName("category")]
        public int Category { get; set; }

        [JsonPropertyName("deviceid")]
        public string DeviceId { get; set; } = string.Empty;

        [JsonPropertyName("measures")]
        public List<Measure> Measures { get; set; } = [];
    }
}
