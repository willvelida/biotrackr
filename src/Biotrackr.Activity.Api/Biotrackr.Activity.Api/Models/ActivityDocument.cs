using System.Text.Json.Serialization;
using Biotrackr.Activity.Api.Models.FitbitEntities;

namespace Biotrackr.Activity.Api.Models
{
    // Activity Document (HTTP response model; Cosmos documents are read as ActivityStoredDocument)
    public class ActivityDocument
    {
        public string Id { get; set; }
        public ActivityResponse Activity { get; set; }
        public string Date { get; set; }
        public string DocumentType { get; set; }

        [JsonPropertyName("provider")]
        public string Provider { get; set; }

        [JsonPropertyName("schemaVersion")]
        public int SchemaVersion { get; set; }

        [JsonPropertyName("activityEnrichment")]
        public ActivityEnrichment? ActivityEnrichment { get; set; }
    }
}
