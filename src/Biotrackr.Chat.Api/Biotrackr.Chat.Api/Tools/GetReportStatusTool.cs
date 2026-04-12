using System.ComponentModel;
using System.Text.Json;
using Biotrackr.Chat.Api.Configuration;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Options;

namespace Biotrackr.Chat.Api.Tools
{
    /// <summary>
    /// Native function tool that checks report generation status and invokes the Reviewer Agent.
    /// When a report is ready, the Reviewer validates it against the source data before presenting.
    /// </summary>
    [Obsolete("Replaced by A2AReportTool. Will be removed when A2A packages exit preview.")]
    public sealed class GetReportStatusTool
    {
        private readonly Settings _settings;
        private readonly HttpClient _httpClient;
        private readonly IReportReviewerService _reviewerService;
        private readonly ILogger<GetReportStatusTool> _logger;

        public GetReportStatusTool(
            IOptions<Settings> settings,
            IHttpClientFactory httpClientFactory,
            IReportReviewerService reviewerService,
            ILogger<GetReportStatusTool> logger)
        {
            _settings = settings.Value;
            _httpClient = httpClientFactory.CreateClient("ReportingApi");
            _reviewerService = reviewerService;
            _logger = logger;
        }

        [Description("Check the status of a report generation job. If the report is ready, it will be reviewed for accuracy before presenting download links.")]
        public async Task<string> GetReportStatus(
            [Description("The job ID returned by RequestReport")] string jobId)
        {
            _logger.LogInformation("GetReportStatus called for job {JobId}", jobId);

            var response = await _httpClient.GetAsync($"/api/reports/{jobId}");

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

        private async Task<string> ReviewAndPresentAsync(ReportStatusResponse result)
        {
            // Invoke the Reviewer Agent to validate the report
            var reviewResult = await _reviewerService.ReviewReportAsync(
                result.Metadata.Summary ?? string.Empty,
                result.Metadata.SourceDataSnapshot,
                result.Metadata.ReportType);

            // Build inline images for charts and download links for documents
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

        public AIFunction AsAIFunction()
        {
            return AIFunctionFactory.Create(
                (string jobId) => GetReportStatus(jobId),
                nameof(GetReportStatus),
                "Check the status of a report generation job. If the report is ready, it will be reviewed for accuracy before presenting download links.");
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
