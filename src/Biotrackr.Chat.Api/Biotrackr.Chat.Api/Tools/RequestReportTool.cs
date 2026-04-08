using System.ComponentModel;
using System.Text.Json;
using Biotrackr.Chat.Api.Configuration;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Options;

namespace Biotrackr.Chat.Api.Tools
{
    /// <summary>
    /// Native function tool that triggers async report generation.
    /// Claude calls this when the user requests a report, chart, or analysis.
    /// The tool gathers health data context, submits a job to Reporting.Api, and returns a job ID.
    /// </summary>
    [Obsolete("Replaced by A2AReportTool. Will be removed when A2A packages exit preview.")]
    public sealed class RequestReportTool
    {
        private readonly Settings _settings;
        private readonly HttpClient _httpClient;
        private readonly ILogger<RequestReportTool> _logger;

        public RequestReportTool(
            IOptions<Settings> settings,
            IHttpClientFactory httpClientFactory,
            ILogger<RequestReportTool> logger)
        {
            _settings = settings.Value;
            _httpClient = httpClientFactory.CreateClient("ReportingApi");
            _logger = logger;
        }

        [Description("Start generating a health report. Returns a job ID that can be used to check status later. " +
            "Use this when the user requests a report, PDF, chart, visualization, diet program, or multi-day analysis. " +
            "Available report types: weekly_summary, monthly_summary, trend_analysis, diet_analysis, correlation_report.")]
        public async Task<string> RequestReport(
            [Description("The type of report to generate")] string reportType,
            [Description("Start date in yyyy-MM-dd format")] string startDate,
            [Description("End date in yyyy-MM-dd format")] string endDate,
            [Description("Natural language instruction describing what to include in the report")] string taskMessage,
            [Description("The raw JSON health data retrieved from MCP tools for the requested date range. Must include the actual data records used for the report.")] string sourceDataSnapshot)
        {
            _logger.LogInformation("RequestReport called: {ReportType} from {StartDate} to {EndDate}", reportType, startDate, endDate);

            var snapshot = DeserializeSnapshot(sourceDataSnapshot);
            if (snapshot is null)
            {
                _logger.LogError("Failed to parse sourceDataSnapshot ({Length} chars)", sourceDataSnapshot?.Length ?? 0);
                return "Sorry, I couldn't process the health data for your report. Please try again.";
            }

            var request = new
            {
                reportType,
                startDate,
                endDate,
                taskMessage,
                sourceDataSnapshot = snapshot.Value
            };

            var response = await _httpClient.PostAsJsonAsync("/api/reports/generate", request);

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

        public AIFunction AsAIFunction()
        {
            return AIFunctionFactory.Create(
                (string reportType, string startDate, string endDate, string taskMessage, string sourceDataSnapshot) =>
                    RequestReport(reportType, startDate, endDate, taskMessage, sourceDataSnapshot),
                nameof(RequestReport),
                "Start generating a health report. Returns a job ID that can be used to check status later. " +
                "Use this when the user requests a report, PDF, chart, visualization, diet program, or multi-day analysis. " +
                "Available report types: weekly_summary, monthly_summary, trend_analysis, diet_analysis, correlation_report.");
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

        private sealed class GenerateResponse
        {
            public string JobId { get; set; } = string.Empty;
            public string Status { get; set; } = string.Empty;
            public string Message { get; set; } = string.Empty;
        }
    }
}
