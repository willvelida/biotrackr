using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using A2A;
using Biotrackr.Chat.Api.Configuration;
using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Options;

#pragma warning disable MEAI001 // ResponseContinuationToken is in preview

namespace Biotrackr.Chat.Api.Tools
{
    /// <summary>
    /// A2A-based tool that sends report generation requests to Reporting.Api via the A2A protocol.
    /// Uses a fire-and-forget pattern: submits the job via A2A and returns a job ID immediately.
    /// The user can then ask to check status via <see cref="CheckReportStatus"/>.
    /// </summary>
    [ExcludeFromCodeCoverage]
    public sealed class A2AReportTool
    {
        private readonly Settings _settings;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly ReportReviewerService _reviewerService;
        private readonly ILogger<A2AReportTool> _logger;

        public A2AReportTool(
            IOptions<Settings> settings,
            IHttpClientFactory httpClientFactory,
            ReportReviewerService reviewerService,
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
            _logger.LogInformation("A2A GenerateReport called: {ReportType} from {StartDate} to {EndDate}", reportType, startDate, endDate);

            var snapshot = DeserializeSnapshot(sourceDataSnapshot);
            if (snapshot is null)
            {
                _logger.LogError("Failed to parse sourceDataSnapshot ({Length} chars)", sourceDataSnapshot?.Length ?? 0);
                return "Sorry, I couldn't process the health data for your report. Please try again.";
            }

            try
            {
                var httpClient = _httpClientFactory.CreateClient("A2AReportingClient");
                var a2aBaseUrl = _settings.ReportingApiUrl.TrimEnd('/') + "/a2a/report";
                var a2aClient = new A2AClient(new Uri(a2aBaseUrl), httpClient);
                var agent = a2aClient.AsAIAgent(
                    name: "ReportingAgent",
                    description: "Generates health reports via A2A protocol");

                var session = await agent.CreateSessionAsync();

                var requestPayload = JsonSerializer.Serialize(new
                {
                    reportType,
                    startDate,
                    endDate,
                    taskMessage,
                    sourceDataSnapshot = snapshot.Value
                });

                // Submit job via A2A — return immediately without polling
                var response = await agent.RunAsync(requestPayload, session);
                _logger.LogInformation("A2A report submitted. Text: {Text}, HasContinuationToken: {HasToken}",
                    response.Text, response.ContinuationToken is not null);

                // Extract job ID — try response text first, fall back to ContinuationToken
                var jobId = ExtractJobId(response.Text ?? string.Empty)
                    ?? ExtractJobIdFromContinuationToken(response.ContinuationToken);

                if (string.IsNullOrEmpty(jobId))
                {
                    _logger.LogWarning("Could not extract job ID from A2A response. Text: {Text}", response.Text);
                    return "Report generation started but I couldn't retrieve the job ID. Please try again.";
                }

                return $"Report generation started. Job ID: {jobId}. You can ask me to check on the status of this report.";
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError(ex, "A2A connection failure to Reporting.Api");
                return "Sorry, I'm unable to reach the report generation service right now. Please try again in a few minutes.";
            }
            catch (TaskCanceledException ex) when (ex.InnerException is TimeoutException)
            {
                _logger.LogError(ex, "A2A request timed out");
                return "The report generation request timed out. Please try again in a few minutes.";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error during A2A report submission");
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

        private static string? ExtractJobId(string responseText)
        {
            // Response format: "Report generation started. Job ID: {jobId}"
            const string marker = "Job ID: ";
            var idx = responseText.IndexOf(marker, StringComparison.OrdinalIgnoreCase);
            if (idx < 0) return null;

            var start = idx + marker.Length;
            var end = responseText.IndexOfAny(['.', ',', ' ', '\n'], start);
            return end < 0 ? responseText[start..].Trim() : responseText[start..end].Trim();
        }

        private static string? ExtractJobIdFromContinuationToken(ResponseContinuationToken? token)
        {
            if (token is null) return null;

            try
            {
                var bytes = token.ToBytes();
                using var doc = JsonDocument.Parse(bytes);
                if (doc.RootElement.TryGetProperty("JobId", out var jobIdElement))
                {
                    return jobIdElement.GetString();
                }
                // Try camelCase variant
                if (doc.RootElement.TryGetProperty("jobId", out var jobIdCamel))
                {
                    return jobIdCamel.GetString();
                }
            }
            catch (JsonException)
            {
                // Token isn't valid JSON — can't extract
            }

            return null;
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
