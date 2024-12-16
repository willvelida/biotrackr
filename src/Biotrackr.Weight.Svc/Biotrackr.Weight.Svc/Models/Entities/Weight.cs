using System.Text.Json.Serialization;

namespace Biotrackr.Weight.Svc.Models.Entities
{
    public class Weight
    {
        [JsonPropertyName("bmi")]
        public double Bmi { get; set; }
        [JsonPropertyName("date")]
        public string Date { get; set; }
        [JsonPropertyName("fat")]
        public double Fat { get; set; }
        [JsonPropertyName("logId")]
        public object LogId { get; set; }
        [JsonPropertyName("source")]
        public string Source { get; set; }
        [JsonPropertyName("time")]
        public string Time { get; set; }
        [JsonPropertyName("weight")]
        public int weight { get; set; }
    }
}
