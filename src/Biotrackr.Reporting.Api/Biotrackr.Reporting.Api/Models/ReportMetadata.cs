using System.Text.Json.Serialization;

namespace Biotrackr.Reporting.Api.Models
{
    public class ReportMetadata
    {
        [JsonPropertyName("jobId")]
        public string JobId { get; set; } = string.Empty;

        [JsonPropertyName("status")]
        public string Status { get; set; } = ReportStatus.Generating;

        [JsonPropertyName("reportType")]
        public string ReportType { get; set; } = string.Empty;

        [JsonPropertyName("dateRange")]
        public ReportDateRange DateRange { get; set; } = new();

        [JsonPropertyName("generatedAt")]
        public DateTimeOffset? GeneratedAt { get; set; }

        [JsonPropertyName("summary")]
        public string? Summary { get; set; }

        [JsonPropertyName("artifacts")]
        public List<string> Artifacts { get; set; } = [];

        [JsonPropertyName("sourceDataSnapshot")]
        public object? SourceDataSnapshot { get; set; }

        [JsonPropertyName("error")]
        public string? Error { get; set; }

        [JsonPropertyName("blobPath")]
        public string? BlobPath { get; set; }

        [JsonPropertyName("reviewedAt")]
        public DateTimeOffset? ReviewedAt { get; set; }

        [JsonPropertyName("reviewApproved")]
        public bool? ReviewApproved { get; set; }

        [JsonPropertyName("reviewConcerns")]
        public List<string>? ReviewConcerns { get; set; }

        [JsonPropertyName("reviewValidatedSummary")]
        public string? ReviewValidatedSummary { get; set; }
    }

    public class ReportDateRange
    {
        [JsonPropertyName("start")]
        public string Start { get; set; } = string.Empty;

        [JsonPropertyName("end")]
        public string End { get; set; } = string.Empty;
    }

    public static class ReportStatus
    {
        public const string Generating = "generating";
        public const string Generated = "generated";
        public const string Failed = "failed";
        public const string Reviewed = "reviewed";
    }

    public static class ReportType
    {
        public const string WeeklySummary = "weekly_summary";
        public const string MonthlySummary = "monthly_summary";
        public const string TrendAnalysis = "trend_analysis";
        public const string DietAnalysis = "diet_analysis";
        public const string CorrelationReport = "correlation_report";

        public static readonly string[] All =
        [
            WeeklySummary, MonthlySummary, TrendAnalysis, DietAnalysis, CorrelationReport
        ];

        public static bool IsValid(string reportType) => All.Contains(reportType);
    }
}
