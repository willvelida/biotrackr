using System.Text.Json.Serialization;

namespace Biotrackr.Sleep.Api.Models.FitbitEntities
{
    public class Levels
    {
        [JsonPropertyName("data")]
        public List<SleepData> Data { get; set; }
        [JsonPropertyName("shortData")]
        public List<SleepData> ShortData { get; set; }
        [JsonPropertyName("summary")]
        public Summary Summary { get; set; }
    }
}
