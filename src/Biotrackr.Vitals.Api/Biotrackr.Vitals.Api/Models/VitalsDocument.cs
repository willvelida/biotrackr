using System.Text.Json.Serialization;

namespace Biotrackr.Vitals.Api.Models
{
    public class VitalsDocument
    {
        [JsonPropertyName("id")]
        public string Id { get; set; }

        [JsonPropertyName("weight")]
        public WeightMeasurement? Weight { get; set; }

        [JsonPropertyName("bloodPressureReadings")]
        public List<BloodPressureReading>? BloodPressureReadings { get; set; }

        [JsonPropertyName("date")]
        public string Date { get; set; }

        [JsonPropertyName("documentType")]
        public string DocumentType { get; set; }

        [JsonPropertyName("provider")]
        public string? Provider { get; set; }
    }
}
