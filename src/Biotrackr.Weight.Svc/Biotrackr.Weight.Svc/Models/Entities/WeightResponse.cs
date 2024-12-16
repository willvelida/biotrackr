using System.Text.Json.Serialization;

namespace Biotrackr.Weight.Svc.Models.Entities
{
    public class WeightResponse
    {
        [JsonPropertyName("weight")]
        public List<Weight> Weight { get; set; }
    }
}
