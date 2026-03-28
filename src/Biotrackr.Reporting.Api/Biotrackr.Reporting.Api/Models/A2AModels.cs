using System.Text.Json.Serialization;

namespace Biotrackr.Reporting.Api.Models
{
    /// <summary>
    /// Extended response returned from the A2A task, containing the job ID for async polling.
    /// This is embedded in the A2A message response text alongside the standard A2A protocol flow.
    /// </summary>
    public class ReportJobResult
    {
        [JsonPropertyName("jobId")]
        public string JobId { get; set; } = string.Empty;

        [JsonPropertyName("status")]
        public string Status { get; set; } = string.Empty;

        [JsonPropertyName("message")]
        public string Message { get; set; } = string.Empty;
    }
}
