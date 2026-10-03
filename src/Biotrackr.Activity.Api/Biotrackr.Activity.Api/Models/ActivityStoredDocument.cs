using Biotrackr.Activity.Api.Models.FitbitEntities;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Biotrackr.Activity.Api.Models;

/// <summary>
/// Activity document as stored in Cosmos DB. Version 1 documents (no <see cref="SchemaVersion"/>) hold
/// <see cref="Activity"/>; version 2 documents hold the raw Google Health API bodies in <see cref="Google"/>.
/// Never return this type from an HTTP handler: <see cref="Google"/> is a Newtonsoft <see cref="JObject"/>
/// and does not serialise through System.Text.Json. Translate it to <see cref="ActivityDocument"/> first.
/// </summary>
public class ActivityStoredDocument
{
    public string Id { get; set; } = string.Empty;

    public string Date { get; set; } = string.Empty;

    public string DocumentType { get; set; } = string.Empty;

    // Null is ignored on write so seeded version 1 documents match production ones, which lack these fields.
    [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
    public string? Provider { get; set; }

    [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
    public int? SchemaVersion { get; set; }

    [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
    public ActivityResponse? Activity { get; set; }

    [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
    public JObject? Google { get; set; }
}
