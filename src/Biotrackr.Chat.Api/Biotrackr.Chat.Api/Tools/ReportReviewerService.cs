using System.Text.Json;
using Anthropic;
using Anthropic.Models.Messages;
using Biotrackr.Chat.Api.Configuration;
using Biotrackr.Chat.Api.Telemetry;
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
        private readonly IHttpClientFactory _httpClientFactory;

        public ReportReviewerService(
            IOptions<Settings> settings,
            ILogger<ReportReviewerService> logger,
            IHttpClientFactory httpClientFactory)
        {
            _settings = settings.Value;
            _logger = logger;
            _httpClientFactory = httpClientFactory;
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

            System.Diagnostics.Activity? activity = null;
            try
            {
                var httpClient = _httpClientFactory.CreateClient("Anthropic");
                AnthropicClient anthropicClient = new()
                {
                    ApiKey = _settings.AnthropicApiKey,
                    HttpClient = httpClient
                };

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

                activity = AnthropicTelemetry.StartChatActivity(
                    model: _settings.ChatAgentModel,
                    agentId: "BiotrackrReportReviewer");

                var result = await anthropicClient.Messages.Create(new MessageCreateParams
                {
                    MaxTokens = 4096,
                    Model = _settings.ChatAgentModel,
                    System = new List<TextBlockParam>
                    {
                        new TextBlockParam
                        {
                            Text = _settings.ReviewerSystemPrompt,
                            CacheControl = new CacheControlEphemeral()
                        }
                    },
                    Messages = [new MessageParam { Role = "user", Content = reviewPrompt }]
                });

                AnthropicTelemetry.RecordResponse(
                    activity,
                    responseModel: result.Model,
                    responseId: result.ID,
                    finishReason: result.StopReason,
                    inputTokens: result.Usage?.InputTokens ?? 0,
                    outputTokens: result.Usage?.OutputTokens ?? 0,
                    cacheReadInputTokens: result.Usage?.CacheReadInputTokens ?? 0,
                    cacheCreationInputTokens: result.Usage?.CacheCreationInputTokens ?? 0);

                var responseText = string.Join("", result.Content
                    .OfType<TextBlock>()
                    .Select(b => b.Text));

                _logger.LogInformation("Reviewer response for {ReportType}: {Length} chars, cache_read_input_tokens: {CacheRead}",
                    reportType, responseText.Length, result.Usage?.CacheReadInputTokens ?? 0);

                // Parse the reviewer's JSON response
                return ParseReviewResult(responseText, reportSummary);
            }
            catch (Exception ex)
            {
                AnthropicTelemetry.RecordError(activity, ex);
                _logger.LogError(ex, "Reviewer agent failed. Passing report through with warning.");
                return new ReviewResult
                {
                    Approved = true,
                    ValidatedSummary = reportSummary + "\n\n⚠️ Note: This report could not be independently reviewed. Please verify the data manually.",
                    Concerns = []
                };
            }
            finally
            {
                activity?.Dispose();
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
