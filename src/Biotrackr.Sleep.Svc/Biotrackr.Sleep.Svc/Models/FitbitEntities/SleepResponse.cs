using System.Text.Json.Serialization;

namespace Biotrackr.Sleep.Svc.Models.FitbitEntities
{
    public class SleepResponse
    {
        [JsonPropertyName("sleep")]
        public List<Sleep> Sleep { get; set; }
        [JsonPropertyName("summary")]
        public Summary Summary { get; set; }
    }
}
