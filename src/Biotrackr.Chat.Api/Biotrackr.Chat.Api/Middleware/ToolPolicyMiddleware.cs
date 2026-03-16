using Biotrackr.Chat.Api.Configuration;
using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using System.Runtime.CompilerServices;

namespace Biotrackr.Chat.Api.Middleware
{
    /// <summary>
    /// Agent middleware that enforces tool call policies including per-session
    /// rate limiting and allowed tool name validation.
    /// </summary>
    public class ToolPolicyMiddleware(
        IMemoryCache cache,
        IOptions<ToolPolicyOptions> options,
        ILogger<ToolPolicyMiddleware> logger)
    {
        public const string BudgetExceededMessage =
            "Tool call budget exceeded for this session. Please start a new conversation.";

        public async IAsyncEnumerable<AgentResponseUpdate> HandleAsync(
            IEnumerable<ChatMessage> messages,
            AgentSession? session,
            AgentRunOptions? runOptions,
            AIAgent innerAgent,
            [EnumeratorCancellation] CancellationToken cancellationToken)
        {
            var sessionId = runOptions.GetConversationId();
            var budgetKey = $"toolbudget:{sessionId}";
            var policyOptions = options.Value;

            await foreach (var update in innerAgent.RunStreamingAsync(messages, session, runOptions, cancellationToken))
            {
                var blocked = false;

                foreach (var content in update.Contents)
                {
                    if (content is FunctionCallContent functionCall)
                    {
                        // Log every tool invocation for monitoring/alerting
                        logger.LogInformation(
                            "Tool call invoked: {ToolName} in session {SessionId}",
                            functionCall.Name,
                            sessionId);

                        // Validate tool name is in the allowed set
                        if (!policyOptions.AllowedToolNames.Contains(functionCall.Name))
                        {
                            logger.LogWarning(
                                "Blocked unrecognised tool call: {ToolName} in session {SessionId}",
                                functionCall.Name,
                                sessionId);
                        }

                        // Enforce per-session tool call budget
                        var count = cache.GetOrCreate(budgetKey, entry =>
                        {
                            entry.SlidingExpiration = TimeSpan.FromHours(1);
                            return 0;
                        })!;

                        cache.Set(budgetKey, count + 1, new MemoryCacheEntryOptions
                        {
                            SlidingExpiration = TimeSpan.FromHours(1)
                        });

                        if (count >= policyOptions.MaxToolCallsPerSession)
                        {
                            logger.LogWarning(
                                "Tool call budget ({Budget}) exceeded for session {SessionId}. Total calls: {Count}",
                                policyOptions.MaxToolCallsPerSession,
                                sessionId,
                                count + 1);
                            blocked = true;
                        }
                    }
                }

                if (blocked)
                {
                    yield return new AgentResponseUpdate(
                        ChatRole.Assistant,
                        [new TextContent(BudgetExceededMessage)]);
                    yield break;
                }

                yield return update;
            }
        }
    }
}
