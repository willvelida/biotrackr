using System.Buffers;
using System.Security.Cryptography;
using System.Text.Json;
using Anthropic;
using Anthropic.Models.Messages;
using Biotrackr.Chat.Api.Configuration;
using Biotrackr.Chat.Api.Telemetry;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;

namespace Biotrackr.Chat.Api.Tools
{
    /// <summary>
    /// Reviewer Agent that validates report output against source data at retrieval time.
    /// Uses Claude (Anthropic) with a validation-focused system prompt.
    /// Stateless — created per review, no tools needed.
    /// </summary>
    public sealed class ReportReviewerService : IReportReviewerService
    {
        private readonly Settings _settings;
        private readonly ILogger<ReportReviewerService> _logger;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IMemoryCache _cache;

        public ReportReviewerService(
            IOptions<Settings> settings,
            ILogger<ReportReviewerService> logger,
            IHttpClientFactory httpClientFactory,
            IMemoryCache cache)
        {
            ArgumentNullException.ThrowIfNull(settings);
            ArgumentNullException.ThrowIfNull(logger);
            ArgumentNullException.ThrowIfNull(httpClientFactory);
            ArgumentNullException.ThrowIfNull(cache);
            _settings = settings.Value;
            _logger = logger;
            _httpClientFactory = httpClientFactory;
            _cache = cache;
        }

        public async Task<ReviewResult> ReviewReportAsync(
            string reportSummary, object? sourceDataSnapshot, string reportType)
        {
            if (string.IsNullOrWhiteSpace(_settings.ReviewerSystemPrompt))
            {
                _logger.LogWarning("Reviewer system prompt not configured. Report will be presented with review-skip disclosure.");
                return new ReviewResult
                {
                    Approved = true,
                    ReviewCompleted = false,
                    ReviewSkipReason = "Reviewer system prompt not configured",
                    ValidatedSummary = reportSummary + "\n\n⚠️ Note: This report has not been independently reviewed because the review system is not configured. Please verify the data manually.",
                    Concerns = ["Review was skipped: reviewer system prompt is not configured. Report data has not been independently validated."]
                };
            }

            var sourceDataJson = sourceDataSnapshot is JsonElement je
                ? je.GetRawText()
                : JsonSerializer.Serialize(sourceDataSnapshot);

            var cacheKey = DeriveCacheKey(sourceDataJson, reportSummary, reportType);

            if (_cache.TryGetValue(cacheKey, out ReviewResult? cachedResult))
            {
                _logger.LogDebug("Review cache hit for {ReportType}", reportType);
                return cachedResult!;
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
                return ParseReviewResult(responseText, reportSummary, cacheKey);
            }
            catch (Exception ex)
            {
                AnthropicTelemetry.RecordError(activity, ex);
                _logger.LogError(ex, "Reviewer agent failed for {ReportType}. Report will be presented with review-failure disclosure.", reportType);
                return new ReviewResult
                {
                    Approved = true,
                    ReviewCompleted = false,
                    ReviewSkipReason = "Reviewer agent failed due to a service error",
                    ValidatedSummary = reportSummary + "\n\n⚠️ Note: This report could not be independently reviewed due to a service error. Please verify the data manually.",
                    Concerns = ["Review could not be completed: the reviewer service encountered an error. Report data has not been independently validated."]
                };
            }
            finally
            {
                activity?.Dispose();
            }
        }

        private ReviewResult ParseReviewResult(string responseText, string fallbackSummary, string cacheKey)
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
                    {
                        result.ReviewCompleted = true;
                        _cache.Set(cacheKey, result, TimeSpan.FromMinutes(30));
                        return result;
                    }
                }
            }
            catch
            {
                // Fall through to fail-closed default
            }

            return new ReviewResult
            {
                Approved = true,
                ReviewCompleted = false,
                ReviewSkipReason = "Failed to parse reviewer response",
                ValidatedSummary = fallbackSummary + "\n\n⚠️ Note: The independent review could not be completed because the reviewer's response was unreadable. Please verify the data manually.",
                Concerns = ["Review response was unreadable: the reviewer produced a response that could not be interpreted. Report data has not been independently validated."]
            };
        }

        private const int StackallocUtf8Threshold = 512;

        private static string DeriveCacheKey(
            string sourceDataJson, string reportSummary, string reportType)
        {
            Span<byte> hashBytes = stackalloc byte[SHA256.HashSizeInBytes];

            using (var hash = IncrementalHash.CreateHash(HashAlgorithmName.SHA256))
            {
                AppendLengthPrefixedUtf8(hash, reportType);
                AppendLengthPrefixedUtf8(hash, reportSummary);
                AppendLengthPrefixedUtf8(hash, sourceDataJson);

                if (!hash.TryGetHashAndReset(hashBytes, out _))
                {
                    throw new CryptographicException("Failed to compute SHA-256 hash for cache key.");
                }
            }

            return $"reviewer:{reportType}:{Convert.ToHexString(hashBytes)}";

            static void AppendLengthPrefixedUtf8(IncrementalHash hash, ReadOnlySpan<char> text)
            {
                int byteCount = System.Text.Encoding.UTF8.GetByteCount(text);

                // Write a 4-byte big-endian length prefix to make framing unambiguous
                Span<byte> lengthPrefix = stackalloc byte[4];
                System.Buffers.Binary.BinaryPrimitives.WriteInt32BigEndian(lengthPrefix, byteCount);
                hash.AppendData(lengthPrefix);

                if (byteCount <= StackallocUtf8Threshold)
                {
                    Span<byte> buffer = stackalloc byte[byteCount];
                    System.Text.Encoding.UTF8.GetBytes(text, buffer);
                    hash.AppendData(buffer);
                }
                else
                {
                    byte[] rented = ArrayPool<byte>.Shared.Rent(byteCount);
                    try
                    {
                        int written = System.Text.Encoding.UTF8.GetBytes(text, rented);
                        hash.AppendData(rented.AsSpan(0, written));
                    }
                    finally
                    {
                        ArrayPool<byte>.Shared.Return(rented);
                    }
                }
            }
        }
    }

    public sealed class ReviewResult
    {
        public bool Approved { get; set; } = true;
        public bool ReviewCompleted { get; set; }
        public string? ReviewSkipReason { get; set; }
        public List<string> Concerns { get; set; } = [];
        public string ValidatedSummary { get; set; } = string.Empty;
    }
}
