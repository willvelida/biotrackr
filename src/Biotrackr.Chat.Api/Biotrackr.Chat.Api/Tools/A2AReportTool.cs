using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Net.Http.Json;
using System.Text.Json;
using Biotrackr.Chat.Api.Configuration;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Options;

namespace Biotrackr.Chat.Api.Tools
{
    /// <summary>
    /// Tool that submits report generation requests to Reporting.Api and checks their status.
    /// Uses fire-and-forget via the REST endpoint: submit returns a job ID immediately,
    /// and the user can check status later via <see cref="CheckReportStatus"/>.
    /// Uses a fire-and-forget pattern: submits the job via A2A and returns a job ID immediately.
    /// The user can then ask to check status via <see cref="CheckReportStatus"/>.
    /// </summary>
    [ExcludeFromCodeCoverage]
    public sealed class A2AReportTool
    {
        private readonly Settings _settings;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IReportReviewerService _reviewerService;
        private readonly ILogger<A2AReportTool> _logger;

        public A2AReportTool(
            IOptions<Settings> settings,
            IHttpClientFactory httpClientFactory,
            IReportReviewerService reviewerService,
            ILogger<A2AReportTool> logger)
        {
            _settings = settings.Value;
            _httpClientFactory = httpClientFactory;
            _reviewerService = reviewerService;
            _logger = logger;
        }

        [Description("Start generating a health report. Returns a job ID that can be used to check status later. " +
            "Use this when the user requests a report, PDF, chart, visualization, diet program, or multi-day analysis. " +
            "Available report types: weekly_summary, monthly_summary, trend_analysis, diet_analysis, correlation_report.")]
        public async Task<string> GenerateReport(
            [Description("The type of report to generate")] string reportType,
            [Description("Start date in yyyy-MM-dd format")] string startDate,
            [Description("End date in yyyy-MM-dd format")] string endDate,
            [Description("Natural language instruction describing what to include in the report")] string taskMessage,
            [Description("The raw JSON health data retrieved from MCP tools for the requested date range. Must include the actual data records used for the report.")] string sourceDataSnapshot)
        {
            _logger.LogInformation("GenerateReport called: {ReportType} from {StartDate} to {EndDate}", reportType, startDate, endDate);

            var snapshot = DeserializeSnapshot(sourceDataSnapshot);
            if (snapshot is null)
            {
                _logger.LogError("Failed to parse sourceDataSnapshot ({Length} chars)", sourceDataSnapshot?.Length ?? 0);
                return "Sorry, I couldn't process the health data for your report. Please try again.";
            }

            try
            {
                var httpClient = _httpClientFactory.CreateClient("ReportingApi");

                var request = new
                {
                    reportType,
                    startDate,
                    endDate,
                    taskMessage,
                    sourceDataSnapshot = snapshot.Value
                };

                var response = await httpClient.PostAsJsonAsync("/api/reports/generate", request);

                if (!response.IsSuccessStatusCode)
                {
                    var errorBody = await response.Content.ReadAsStringAsync();
                    _logger.LogError("Reporting.Api returned {StatusCode}: {Error}", response.StatusCode, errorBody);
                    return "Sorry, I wasn't able to start your report right now. Please try again in a few minutes.";
                }

                var result = await response.Content.ReadFromJsonAsync<GenerateResponse>();
                _logger.LogInformation("Report job {JobId} started for {ReportType}", result?.JobId, reportType);

                return $"Report generation started. Job ID: {result?.JobId}. You can ask me to check on the status of this report.";
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError(ex, "Connection failure to Reporting.Api");
                return "Sorry, I'm unable to reach the report generation service right now. Please try again in a few minutes.";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error during report submission");
                return "Sorry, an unexpected error occurred while submitting your report. Please try again.";
            }
        }

        [Description("Check the status of a report generation job. If the report is ready, it will be reviewed for accuracy before presenting download links.")]
        public async Task<string> CheckReportStatus(
            [Description("The job ID returned by GenerateReport")] string jobId)
        {
            _logger.LogInformation("CheckReportStatus called for job {JobId}", jobId);

            try
            {
                var httpClient = _httpClientFactory.CreateClient("ReportingApi");
                var response = await httpClient.GetAsync($"/api/reports/{jobId}");

                if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
                {
                    return "I couldn't find that report. It may have expired or the ID may be incorrect. Would you like to generate a new one?";
                }

                if (!response.IsSuccessStatusCode)
                {
                    var errorBody = await response.Content.ReadAsStringAsync();
                    _logger.LogError("Reporting.Api returned {StatusCode}: {Error}", response.StatusCode, errorBody);
                    return "Sorry, I'm unable to check your report status right now. Please try again in a few minutes.";
                }

                var result = await response.Content.ReadFromJsonAsync<ReportStatusResponse>();
                if (result?.Metadata is null)
                {
                    return "Sorry, I'm unable to read your report details right now. Please try again in a few minutes.";
                }

                return result.Metadata.Status switch
                {
                    "generating" => "Your report is still being generated. Please check back in a moment.",
                    "failed" => "Unfortunately, your report couldn't be completed. Would you like to try generating a new one?",
                    "generated" or "reviewed" => await ReviewAndPresentAsync(result),
                    _ => "Your report is in an unexpected state. Would you like to try generating a new one?"
                };
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError(ex, "Failed to check report status for job {JobId}", jobId);
                return "Sorry, I'm unable to reach the report service right now. Please try again in a few minutes.";
            }
        }

        private async Task<string> ReviewAndPresentAsync(ReportStatusResponse result)
        {
            var reviewResult = await _reviewerService.ReviewReportAsync(
                result.Metadata.Summary ?? string.Empty,
                result.Metadata.SourceDataSnapshot,
                result.Metadata.ReportType);

            var images = new List<string>();
            var downloads = new List<string>();
            foreach (var (artifact, url) in result.ArtifactUrls)
            {
                if (IsImageArtifact(artifact))
                {
                    images.Add($"![{Path.GetFileNameWithoutExtension(artifact)}]({url})");
                }
                else
                {
                    downloads.Add($"- [📥 Download {artifact}]({url})");
                }
            }

            var imageSection = images.Count > 0
                ? $"\n\n{string.Join("\n\n", images)}"
                : "";

            var downloadSection = downloads.Count > 0
                ? $"\n\n{string.Join("\n", downloads)}"
                : "";

            if (!reviewResult.ReviewCompleted)
            {
                _logger.LogWarning("Review not completed for job {JobId}: {Reason}",
                    result.Metadata.JobId, reviewResult.ReviewSkipReason);

                var reviewStatus = reviewResult.Concerns.Count > 0
                    ? $"\n\n**Review Status:** The independent review did not complete.\n- {string.Join("\n- ", reviewResult.Concerns)}"
                    : "\n\n**Review Status:** The independent review did not complete.";

                return $"{reviewResult.ValidatedSummary}{reviewStatus}{imageSection}{downloadSection}";
            }

            if (!reviewResult.Approved)
            {
                _logger.LogWarning("Reviewer flagged concerns for job {JobId}: {Concerns}",
                    result.Metadata.JobId, string.Join("; ", reviewResult.Concerns));

                var concerns = string.Join("\n- ", reviewResult.Concerns);
                return $"Your report is ready but the reviewer flagged some concerns:\n- {concerns}\n\n" +
                       $"Summary: {reviewResult.ValidatedSummary}" +
                       $"{imageSection}{downloadSection}\n\n" +
                       "Would you like me to regenerate the report?";
            }

            return $"{reviewResult.ValidatedSummary}{imageSection}{downloadSection}";
        }

        public AIFunction AsGenerateReportFunction()
        {
            return AIFunctionFactory.Create(
                (string reportType, string startDate, string endDate, string taskMessage, string sourceDataSnapshot) =>
                    GenerateReport(reportType, startDate, endDate, taskMessage, sourceDataSnapshot),
                nameof(GenerateReport),
                "Start generating a health report. Returns a job ID that can be used to check status later. " +
                "Use this when the user requests a report, PDF, chart, visualization, diet program, or multi-day analysis. " +
                "Available report types: weekly_summary, monthly_summary, trend_analysis, diet_analysis, correlation_report.");
        }

        public AIFunction AsCheckReportStatusFunction()
        {
            return AIFunctionFactory.Create(
                (string jobId) => CheckReportStatus(jobId),
                nameof(CheckReportStatus),
                "Check the status of a report generation job. If the report is ready, it will be reviewed for accuracy before presenting download links.");
        }

        private static JsonElement? DeserializeSnapshot(string sourceDataSnapshot)
        {
            if (string.IsNullOrWhiteSpace(sourceDataSnapshot))
                return null;

            try
            {
                using var doc = JsonDocument.Parse(sourceDataSnapshot);
                return doc.RootElement.Clone();
            }
            catch (JsonException)
            {
                return null;
            }
        }

        private static readonly string[] ImageExtensions = [".png", ".jpg", ".jpeg", ".svg", ".gif", ".webp"];

        private static bool IsImageArtifact(string filename)
        {
            var ext = Path.GetExtension(filename);
            return ImageExtensions.Any(e => e.Equals(ext, StringComparison.OrdinalIgnoreCase));
        }

        private sealed class GenerateResponse
        {
            public string JobId { get; set; } = string.Empty;
            public string Status { get; set; } = string.Empty;
            public string Message { get; set; } = string.Empty;
        }

        private sealed class ReportStatusResponse
        {
            public ReportMetadataDto Metadata { get; set; } = new();
            public Dictionary<string, string> ArtifactUrls { get; set; } = [];
        }

        private sealed class ReportMetadataDto
        {
            public string JobId { get; set; } = string.Empty;
            public string Status { get; set; } = string.Empty;
            public string ReportType { get; set; } = string.Empty;
            public string? Summary { get; set; }
            public string? Error { get; set; }
            public object? SourceDataSnapshot { get; set; }
            public List<string> Artifacts { get; set; } = [];
        }
    }
}
