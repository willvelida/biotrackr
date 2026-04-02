using System.Text.Json.Serialization;

namespace Biotrackr.Weight.Svc.Models.WithingsEntities
{
    public class WithingsMeasureResponse
    {
        [JsonPropertyName("status")]
        public int Status { get; set; }

        [JsonPropertyName("body")]
        public WithingsMeasureBody? Body { get; set; }
    }

    public class WithingsMeasureBody
    {
        [JsonPropertyName("updatetime")]
        public string UpdateTime { get; set; } = string.Empty;

        [JsonPropertyName("timezone")]
        public string Timezone { get; set; } = string.Empty;

        [JsonPropertyName("measuregrps")]
        public List<MeasureGroup> MeasureGroups { get; set; } = [];

        [JsonPropertyName("more")]
        public int More { get; set; }

        [JsonPropertyName("offset")]
        public int Offset { get; set; }
    }
}
