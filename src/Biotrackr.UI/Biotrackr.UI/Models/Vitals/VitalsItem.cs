using System.Text.Json.Serialization;

namespace Biotrackr.UI.Models.Vitals
{
    public class VitalsItem
    {
        [JsonPropertyName("id")]
        public string Id { get; set; } = string.Empty;

        [JsonPropertyName("weight")]
        public VitalsData? Weight { get; set; }

        [JsonPropertyName("bloodPressureReadings")]
        public List<BloodPressureReadingData>? BloodPressureReadings { get; set; }

        [JsonPropertyName("date")]
        public string Date { get; set; } = string.Empty;

        [JsonPropertyName("documentType")]
        public string DocumentType { get; set; } = string.Empty;

        [JsonPropertyName("provider")]
        public string? Provider { get; set; }
    }
}
