using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;

namespace Biotrackr.Chat.Api.Middleware
{
    /// <summary>
    /// Extension methods for extracting the AG-UI thread ID from agent run context.
    /// </summary>
    public static class AgentSessionExtensions
    {
        /// <summary>
        /// Extracts the AG-UI thread ID from the run options (set by MapAGUI),
        /// falling back to <paramref name="fallback"/> if unavailable.
        /// </summary>
        public static string GetConversationId(
            this AgentRunOptions? options,
            string fallback = "unknown")
        {
            if (options is ChatClientAgentRunOptions chatRunOptions
                && chatRunOptions.ChatOptions?.AdditionalProperties is { } props
                && props.TryGetValue("ag_ui_thread_id", out var threadIdObj)
                && threadIdObj is string threadId
                && !string.IsNullOrEmpty(threadId))
            {
                return threadId;
            }

            return fallback;
        }
    }
}
