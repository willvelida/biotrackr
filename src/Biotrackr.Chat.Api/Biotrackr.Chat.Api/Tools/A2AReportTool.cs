using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using A2A;
using Biotrackr.Chat.Api.Configuration;
using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Options;

#pragma warning disable MEAI001 // ContinuationToken is experimental in preview MAF packages

namespace Biotrackr.Chat.Api.Tools
{
    /// <summary>
    /// A2A-based tool that sends report generation requests to Reporting.Api via the A2A protocol.
    /// Replaces the separate RequestReportTool + GetReportStatusTool with a single tool that
    /// handles the full lifecycle: submit → poll with exponential backoff → review → return.
    /// </summary>
    [ExcludeFromCodeCoverage]
    public sealed class A2AReportTool
    {
        private readonly Settings _settings;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly ReportReviewerService _reviewerService;
        private readonly ILogger<A2AReportTool> _logger;

        /// <summary>Maximum total wait time before timeout (15 minutes).</summary>
        private static readonly TimeSpan MaxTotalWait = TimeSpan.FromMinutes(15);

        /// <summary>Exponential backoff delays: 5s → 10s → 20s → 40s → 60s cap.</summary>
        private static readonly TimeSpan[] BackoffDelays =
        [
            TimeSpan.FromSeconds(5),
            TimeSpan.FromSeconds(10),
            TimeSpan.FromSeconds(20),
            TimeSpan.FromSeconds(40),
            TimeSpan.FromSeconds(60)
        ];

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

        [Description("Generate a health report via A2A protocol. Submits the request, waits for completion with status polling, " +
            "and returns the reviewed report. Use this when the user requests a report, PDF, chart, visualization, diet program, or multi-day analysis. " +
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
                var a2aClient = new A2AClient(new Uri(_settings.ReportingApiUrl), httpClient);
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

                // Send initial message
                var response = await agent.RunAsync(requestPayload, session);
                _logger.LogInformation("A2A initial response received. ContinuationToken present: {HasToken}",
                    response.ContinuationToken is not null);

                // Poll with exponential backoff while the task is still working
                var totalWaited = TimeSpan.Zero;
                var backoffIndex = 0;

                while (response.ContinuationToken is { } token)
                {
                    var delay = BackoffDelays[Math.Min(backoffIndex, BackoffDelays.Length - 1)];

                    if (totalWaited + delay > MaxTotalWait)
                    {
                        _logger.LogWarning("A2A report generation timed out after {Elapsed}", totalWaited);
                        return "Your report is taking longer than expected. It's still being generated in the background. " +
                               "Please ask me to check on it again in a few minutes.";
                    }

                    _logger.LogDebug("A2A polling: waiting {Delay}s (total waited: {TotalWaited}s, backoff index: {Index})",
                        delay.TotalSeconds, totalWaited.TotalSeconds, backoffIndex);

                    await Task.Delay(delay);
                    totalWaited += delay;
                    backoffIndex++;

                    response = await agent.RunAsync(
                        session,
                        options: new AgentRunOptions { ContinuationToken = token });
                }

                var responseText = response.Text;
                _logger.LogInformation("A2A report completed. Response length: {Length} chars", responseText?.Length ?? 0);

                if (string.IsNullOrWhiteSpace(responseText))
                {
                    return "The report was generated but no content was returned. Please try again.";
                }

                // Invoke the Reviewer Agent to validate the report before presenting
                var reviewResult = await _reviewerService.ReviewReportAsync(
                    responseText, snapshot.Value, reportType);

                if (!reviewResult.Approved)
                {
                    _logger.LogWarning("Reviewer flagged concerns for A2A report: {Concerns}",
                        string.Join("; ", reviewResult.Concerns));

                    var concerns = string.Join("\n- ", reviewResult.Concerns);
                    return $"Your report is ready but the reviewer flagged some concerns:\n- {concerns}\n\n" +
                           $"{reviewResult.ValidatedSummary}\n\n" +
                           "Would you like me to regenerate the report?";
                }

                return reviewResult.ValidatedSummary;
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
                _logger.LogError(ex, "Unexpected error during A2A report generation");
                return "Sorry, an unexpected error occurred while generating your report. Please try again.";
            }
        }

        public AIFunction AsAIFunction()
        {
            return AIFunctionFactory.Create(
                (string reportType, string startDate, string endDate, string taskMessage, string sourceDataSnapshot) =>
                    GenerateReport(reportType, startDate, endDate, taskMessage, sourceDataSnapshot),
                nameof(GenerateReport),
                "Generate a health report via A2A protocol. Submits the request, waits for completion with status polling, " +
                "and returns the reviewed report. Use this when the user requests a report, PDF, chart, visualization, diet program, or multi-day analysis. " +
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
    }
}
