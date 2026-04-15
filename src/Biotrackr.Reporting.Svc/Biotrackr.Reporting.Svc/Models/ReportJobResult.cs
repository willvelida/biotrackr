using System.Text.Json.Serialization;

namespace Biotrackr.Reporting.Svc.Models;

public class ReportJobResult
{
    [JsonPropertyName("jobId")]
    public string JobId { get; set; } = string.Empty;

    [JsonPropertyName("status")]
    public string Status { get; set; } = string.Empty;

    [JsonPropertyName("message")]
    public string Message { get; set; } = string.Empty;
}
