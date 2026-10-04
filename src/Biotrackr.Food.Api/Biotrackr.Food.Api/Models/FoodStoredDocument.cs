using Biotrackr.Food.Api.Models.FitbitEntities;
using Newtonsoft.Json.Linq;

namespace Biotrackr.Food.Api.Models;

/// <summary>
/// Food document as stored in Cosmos DB. Version 1 (Fitbit) documents carry <see cref="Food"/> and no
/// <see cref="SchemaVersion"/>; version 2 (Google) documents carry the raw Google payloads in <see cref="Google"/>.
/// Read through the Newtonsoft-based Cosmos serializer only; never return this type from an HTTP handler.
/// </summary>
public class FoodStoredDocument
{
    public string Id { get; set; } = string.Empty;
    public string Date { get; set; } = string.Empty;
    public string DocumentType { get; set; } = "Food";
    public string? Provider { get; set; }
    public int? SchemaVersion { get; set; }
    public FoodResponse? Food { get; set; }
    public JObject? Google { get; set; }
}
