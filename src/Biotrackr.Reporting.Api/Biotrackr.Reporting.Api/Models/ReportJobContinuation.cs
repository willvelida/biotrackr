using System.Text.Json.Serialization;

namespace Biotrackr.Reporting.Api.Models
{
    public record ReportJobContinuation
    {
        [JsonPropertyName("jobId")]
        public string JobId { get; init; } = string.Empty;
    }
}
