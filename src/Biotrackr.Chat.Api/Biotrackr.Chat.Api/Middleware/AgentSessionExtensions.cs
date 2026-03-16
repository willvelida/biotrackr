using Microsoft.Agents.AI;

namespace Biotrackr.Chat.Api.Middleware
{
    /// <summary>
    /// Extension methods for extracting conversation identity from <see cref="AgentSession"/>.
    /// </summary>
    public static class AgentSessionExtensions
    {
        /// <summary>
        /// Extracts the conversation/thread ID from the AG-UI session,
        /// falling back to <paramref name="fallback"/> if unavailable.
        /// </summary>
        public static string GetConversationId(this AgentSession? session, string fallback = "unknown")
        {
            if (session is ChatClientAgentSession chatSession
                && !string.IsNullOrEmpty(chatSession.ConversationId))
            {
                return chatSession.ConversationId;
            }

            return fallback;
        }
    }
}
