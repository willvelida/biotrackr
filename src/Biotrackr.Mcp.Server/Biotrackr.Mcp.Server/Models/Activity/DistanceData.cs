using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json.Serialization;

namespace Biotrackr.Mcp.Server.Models.Activity
{
    public class DistanceData
    {
        [JsonPropertyName("activity")]
        public string Activity { get; set; } = string.Empty;

        [JsonPropertyName("distance")]
        public double Distance { get; set; }
    }
}
