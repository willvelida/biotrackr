using System.Text.Json.Serialization;

namespace Biotrackr.Reporting.Svc.Models;

public class ReportStatusResponse
{
    [JsonPropertyName("metadata")]
    public ReportMetadata Metadata { get; set; } = new();

    [JsonPropertyName("artifactUrls")]
    public Dictionary<string, string> ArtifactUrls { get; set; } = [];
}

public class ReportMetadata
{
    [JsonPropertyName("jobId")]
    public string JobId { get; set; } = string.Empty;

    [JsonPropertyName("status")]
    public string Status { get; set; } = string.Empty;

    [JsonPropertyName("reportType")]
    public string ReportType { get; set; } = string.Empty;

    [JsonPropertyName("summary")]
    public string? Summary { get; set; }

    [JsonPropertyName("error")]
    public string? Error { get; set; }
}
