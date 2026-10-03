using Biotrackr.Sleep.Api.Models.FitbitEntities;
using Newtonsoft.Json.Linq;

namespace Biotrackr.Sleep.Api.Models
{
    /// <summary>
    /// Sleep document as stored in Cosmos DB. Holds either a version 1 (Fitbit) document,
    /// which has no <see cref="SchemaVersion"/> and carries <see cref="Sleep"/>, or a
    /// version 2 (Google) document, which carries the raw Google payloads in <see cref="Google"/>.
    /// Read through the Newtonsoft-based Cosmos serializer only; never return it over HTTP.
    /// </summary>
    public class SleepStoredDocument
    {
        public string Id { get; set; } = string.Empty;
        public SleepResponse? Sleep { get; set; }
        public string Date { get; set; } = string.Empty;
        public string DocumentType { get; set; } = string.Empty;
        public string? Provider { get; set; }
        public int? SchemaVersion { get; set; }
        public JObject? Google { get; set; }
    }
}
