using System.Text.Json.Serialization;

namespace Biotrackr.UI.Models.Sleep
{
    public class SleepData
    {
        [JsonPropertyName("sleep")]
        public List<SleepRecord> Sleep { get; set; } = [];

        [JsonPropertyName("summary")]
        public SleepSummary Summary { get; set; } = new();
    }
}
