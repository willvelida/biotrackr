using System.Text.Json;
using Anthropic;
using Biotrackr.Chat.Api.Configuration;
using Microsoft.Agents.AI;
using Microsoft.Extensions.Options;

namespace Biotrackr.Chat.Api.Tools
{
    /// <summary>
    /// Reviewer Agent that validates report output against source data at retrieval time.
    /// Uses Claude (Anthropic) with a validation-focused system prompt.
    /// Stateless — created per review, no tools needed.
    /// </summary>
    public sealed class ReportReviewerService
    {
        private readonly Settings _settings;
        private readonly ILogger<ReportReviewerService> _logger;

        public ReportReviewerService(
            IOptions<Settings> settings,
            ILogger<ReportReviewerService> logger)
        {
            _settings = settings.Value;
            _logger = logger;
        }

        public async Task<ReviewResult> ReviewReportAsync(
            string reportSummary, object? sourceDataSnapshot, string reportType)
        {
            if (string.IsNullOrWhiteSpace(_settings.ReviewerSystemPrompt))
            {
                _logger.LogWarning("Reviewer system prompt not configured. Skipping review.");
                return new ReviewResult
                {
                    Approved = true,
                    ValidatedSummary = reportSummary,
                    Concerns = []
                };
            }

            try
            {
                AnthropicClient anthropicClient = new() { ApiKey = _settings.AnthropicApiKey };
                AIAgent reviewer = anthropicClient.AsAIAgent(
                    model: _settings.ChatAgentModel,
                    name: "BiotrackrReportReviewer",
                    instructions: _settings.ReviewerSystemPrompt);

                var sourceDataJson = JsonSerializer.Serialize(sourceDataSnapshot, new JsonSerializerOptions { WriteIndented = false });

                var reviewPrompt = $"Review the following report for accuracy and safety.\n\n" +
                    $"Report Type: {reportType}\n" +
                    $"Report Summary: {reportSummary}\n\n" +
                    $"Source Data Snapshot:\n```json\n{sourceDataJson}\n```\n\n" +
                    "Respond with a JSON object:\n" +
                    "{\n" +
                    "  \"approved\": true/false,\n" +
                    "  \"concerns\": [\"list of concerns if any\"],\n" +
                    "  \"validatedSummary\": \"the summary with corrections and disclaimers applied\"\n" +
                    "}";

                var response = await reviewer.RunAsync(reviewPrompt);
                var responseText = response?.ToString() ?? string.Empty;

                _logger.LogInformation("Reviewer response for {ReportType}: {Length} chars", reportType, responseText.Length);

                // Parse the reviewer's JSON response
                return ParseReviewResult(responseText, reportSummary);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Reviewer agent failed. Passing report through with warning.");
                return new ReviewResult
                {
                    Approved = true,
                    ValidatedSummary = reportSummary + "\n\n⚠️ Note: This report could not be independently reviewed. Please verify the data manually.",
                    Concerns = []
                };
            }
        }

        private static ReviewResult ParseReviewResult(string responseText, string fallbackSummary)
        {
            try
            {
                // Try to extract JSON from the response (may be wrapped in markdown code blocks)
                var jsonStart = responseText.IndexOf('{');
                var jsonEnd = responseText.LastIndexOf('}');

                if (jsonStart >= 0 && jsonEnd > jsonStart)
                {
                    var json = responseText[jsonStart..(jsonEnd + 1)];
                    var result = JsonSerializer.Deserialize<ReviewResult>(json, new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    });

                    if (result is not null)
                        return result;
                }
            }
            catch
            {
                // Fall through to default
            }

            return new ReviewResult
            {
                Approved = true,
                ValidatedSummary = fallbackSummary,
                Concerns = []
            };
        }
    }

    public sealed class ReviewResult
    {
        public bool Approved { get; set; } = true;
        public List<string> Concerns { get; set; } = [];
        public string ValidatedSummary { get; set; } = string.Empty;
    }
}
