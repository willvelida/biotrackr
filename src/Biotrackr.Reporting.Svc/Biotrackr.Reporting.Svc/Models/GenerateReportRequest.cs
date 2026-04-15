using System.Text.Json.Serialization;

namespace Biotrackr.Reporting.Svc.Models;

public class GenerateReportRequest
{
    [JsonPropertyName("reportType")]
    public string ReportType { get; set; } = string.Empty;

    [JsonPropertyName("startDate")]
    public string StartDate { get; set; } = string.Empty;

    [JsonPropertyName("endDate")]
    public string EndDate { get; set; } = string.Empty;

    [JsonPropertyName("taskMessage")]
    public string TaskMessage { get; set; } = string.Empty;

    [JsonPropertyName("sourceDataSnapshot")]
    public object? SourceDataSnapshot { get; set; }
}
