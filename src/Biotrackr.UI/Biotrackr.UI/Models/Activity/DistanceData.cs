using System.Text.Json.Serialization;

namespace Biotrackr.UI.Models.Activity
{
    public class DistanceData
    {
        [JsonPropertyName("activity")]
        public string Activity { get; set; } = string.Empty;

        [JsonPropertyName("distance")]
        public double Distance { get; set; }
    }
}
