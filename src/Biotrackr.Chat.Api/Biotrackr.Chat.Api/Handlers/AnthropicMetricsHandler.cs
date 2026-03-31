using System.Diagnostics;
using System.Diagnostics.Metrics;

namespace Biotrackr.Chat.Api.Handlers;

public sealed class AnthropicMetricsHandler : DelegatingHandler
{
    private static readonly Meter s_meter = new("Biotrackr.Chat.Anthropic");
    private static readonly Counter<long> s_requestCount = s_meter.CreateCounter<long>(
        "anthropic.requests.total", description: "Total Anthropic API requests");
    private static readonly Counter<long> s_rateLimitHits = s_meter.CreateCounter<long>(
        "anthropic.ratelimit.hits", description: "Rate limit 429 responses");
    private static readonly Histogram<double> s_requestDuration = s_meter.CreateHistogram<double>(
        "anthropic.request.duration", "ms", "Anthropic API request duration");

    private readonly ILogger<AnthropicMetricsHandler> _logger;

    public AnthropicMetricsHandler(ILogger<AnthropicMetricsHandler> logger)
    {
        _logger = logger;
    }

    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var sw = Stopwatch.StartNew();
        var response = await base.SendAsync(request, cancellationToken);
        sw.Stop();

        s_requestCount.Add(1);
        s_requestDuration.Record(sw.Elapsed.TotalMilliseconds);

        if ((int)response.StatusCode == 429)
        {
            s_rateLimitHits.Add(1);
            _logger.LogWarning("Anthropic rate limit hit (429)");
        }

        CaptureRateLimitHeaders(response);
        return response;
    }

    private void CaptureRateLimitHeaders(HttpResponseMessage response)
    {
        TryLogHeader(response, "anthropic-ratelimit-input-tokens-limit", "InputTokensLimit");
        TryLogHeader(response, "anthropic-ratelimit-input-tokens-remaining", "InputTokensRemaining");
        TryLogHeader(response, "anthropic-ratelimit-output-tokens-remaining", "OutputTokensRemaining");
        TryLogHeader(response, "anthropic-ratelimit-requests-remaining", "RequestsRemaining");
    }

    private void TryLogHeader(HttpResponseMessage response, string header, string metricName)
    {
        if (response.Headers.TryGetValues(header, out var values))
        {
            var value = values.FirstOrDefault();
            if (long.TryParse(value, out var parsed))
            {
                _logger.LogDebug("Anthropic {MetricName}: {Value}", metricName, parsed);
            }
        }
    }
}
