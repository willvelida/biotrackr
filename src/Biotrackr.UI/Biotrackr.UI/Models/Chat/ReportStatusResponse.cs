using System.Text.Json.Serialization;

namespace Biotrackr.UI.Models.Chat;

public class ReportStatusResponse
{
    [JsonPropertyName("jobId")]
    public string JobId { get; set; } = string.Empty;

    [JsonPropertyName("status")]
    public string Status { get; set; } = string.Empty;
}
