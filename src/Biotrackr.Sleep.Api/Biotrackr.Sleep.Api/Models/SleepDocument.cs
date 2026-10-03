using Biotrackr.Sleep.Api.Models.FitbitEntities;

namespace Biotrackr.Sleep.Api.Models
{
    public class SleepDocument
    {
        public string Id { get; set; }
        public SleepResponse Sleep { get; set; }
        public string Date { get; set; }
        public string DocumentType { get; set; }

        /// <summary><c>Fitbit</c> for version 1 documents, <c>Google</c> for version 2.</summary>
        public string Provider { get; set; }

        /// <summary>1 for documents stored without a schema version, otherwise the stored version.</summary>
        public int SchemaVersion { get; set; }

        /// <summary>Null for version 1 documents.</summary>
        public SleepEnrichment? SleepEnrichment { get; set; }
    }
}
