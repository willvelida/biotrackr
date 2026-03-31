using System.Diagnostics;

namespace Biotrackr.Chat.Api.Telemetry;

/// <summary>
/// Static instrumentation class for Anthropic API calls following OpenTelemetry gen_ai semantic conventions.
/// Creates <see cref="Activity"/> spans with standardised gen_ai.* attributes so traces are
/// correlated across Azure Monitor / Application Insights and AI Foundry.
/// </summary>
public static class AnthropicTelemetry
{
    /// <summary>
    /// ActivitySource registered with the OpenTelemetry pipeline in Program.cs.
    /// Name follows the gen_ai provider convention: gen_ai.{provider}.
    /// </summary>
    public static readonly ActivitySource Source = new("gen_ai.anthropic");

    /// <summary>
    /// Starts an Activity for a Claude chat operation following OpenTelemetry GenAI semantic conventions.
    /// Caller disposes the returned Activity after the API call completes and sets response attributes.
    /// </summary>
    public static Activity? StartChatActivity(string model, string? agentId = null)
    {
        var activity = Source.StartActivity($"chat {model}", ActivityKind.Client);
        if (activity is null) return null;

        activity.SetTag("gen_ai.operation.name", "chat");
        activity.SetTag("gen_ai.provider.name", "anthropic");
        activity.SetTag("gen_ai.request.model", model);
        activity.SetTag("server.address", "api.anthropic.com");
        activity.SetTag("server.port", 443);

        if (agentId is not null)
        {
            activity.SetTag("gen_ai.agents.id", agentId);
        }

        return activity;
    }

    /// <summary>
    /// Records response attributes on the Activity after a Claude API call completes.
    /// Token computation follows Anthropic convention: input_tokens excludes cached tokens.
    /// Total input = input_tokens + cache_read_input_tokens + cache_creation_input_tokens.
    /// </summary>
    public static void RecordResponse(
        Activity? activity,
        string? responseModel,
        string? responseId,
        string? finishReason,
        long inputTokens,
        long outputTokens,
        long cacheReadInputTokens = 0,
        long cacheCreationInputTokens = 0)
    {
        if (activity is null) return;

        activity.SetTag("gen_ai.response.model", responseModel);
        activity.SetTag("gen_ai.response.id", responseId);
        activity.SetTag("gen_ai.usage.input_tokens", inputTokens + cacheReadInputTokens + cacheCreationInputTokens);
        activity.SetTag("gen_ai.usage.output_tokens", outputTokens);

        if (cacheReadInputTokens > 0)
            activity.SetTag("gen_ai.usage.cache_read.input_tokens", cacheReadInputTokens);
        if (cacheCreationInputTokens > 0)
            activity.SetTag("gen_ai.usage.cache_creation.input_tokens", cacheCreationInputTokens);

        if (finishReason is not null)
            activity.SetTag("gen_ai.response.finish_reasons", new[] { finishReason });
    }

    /// <summary>
    /// Records an error on the Activity, setting the status to Error and the error.type tag.
    /// </summary>
    public static void RecordError(Activity? activity, Exception ex)
    {
        if (activity is null) return;
        activity.SetStatus(ActivityStatusCode.Error, ex.Message);
        activity.SetTag("error.type", ex.GetType().Name);
    }
}
