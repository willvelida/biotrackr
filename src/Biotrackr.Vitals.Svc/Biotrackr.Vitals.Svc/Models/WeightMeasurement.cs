using System.Text.Json.Serialization;

namespace Biotrackr.Vitals.Svc.Models
{
    public class WeightMeasurement
    {
        [JsonPropertyName("bmi")]
        public double Bmi { get; set; }

        [JsonPropertyName("date")]
        public string Date { get; set; } = string.Empty;

        [JsonPropertyName("fat")]
        public double Fat { get; set; }

        [JsonPropertyName("logId")]
        public object? LogId { get; set; }

        [JsonPropertyName("source")]
        public string Source { get; set; } = string.Empty;

        [JsonPropertyName("time")]
        public string Time { get; set; } = string.Empty;

        [JsonPropertyName("weight")]
        public double WeightKg { get; set; }

        [JsonPropertyName("fatMassKg")]
        public double? FatMassKg { get; set; }

        [JsonPropertyName("fatFreeMassKg")]
        public double? FatFreeMassKg { get; set; }

        [JsonPropertyName("muscleMassKg")]
        public double? MuscleMassKg { get; set; }

        [JsonPropertyName("boneMassKg")]
        public double? BoneMassKg { get; set; }

        [JsonPropertyName("waterMassKg")]
        public double? WaterMassKg { get; set; }

        [JsonPropertyName("visceralFatIndex")]
        public int? VisceralFatIndex { get; set; }
    }
}
