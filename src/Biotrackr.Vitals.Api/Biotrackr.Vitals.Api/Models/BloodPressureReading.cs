using System.Text.Json.Serialization;

namespace Biotrackr.Vitals.Api.Models
{
    public class BloodPressureReading
    {
        [JsonPropertyName("systolic")]
        public int Systolic { get; set; }

        [JsonPropertyName("diastolic")]
        public int Diastolic { get; set; }

        [JsonPropertyName("heartRate")]
        public int HeartRate { get; set; }

        [JsonPropertyName("timestamp")]
        public string Timestamp { get; set; } = string.Empty;

        [JsonPropertyName("time")]
        public string Time { get; set; } = string.Empty;

        [JsonPropertyName("source")]
        public string Source { get; set; } = "Withings";

        [JsonPropertyName("logId")]
        public long LogId { get; set; }

        [JsonPropertyName("deviceId")]
        public string? DeviceId { get; set; }
    }
}
