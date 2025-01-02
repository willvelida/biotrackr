using System.Text.Json.Serialization;

namespace Biotrackr.Weight.Api.Models.FitbitEntities
{
    public class WeightResponse
    {
        [JsonPropertyName("weight")]
        public List<Weight> Weight { get; set; }
    }
}
