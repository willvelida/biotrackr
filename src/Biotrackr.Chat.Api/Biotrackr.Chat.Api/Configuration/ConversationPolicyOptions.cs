namespace Biotrackr.Chat.Api.Configuration
{
    /// <summary>
    /// Conversation persistence safety limits (ASI06).
    /// </summary>
    public class ConversationPolicyOptions
    {
        /// <summary>
        /// Maximum number of historical messages to hydrate into the agent's context window.
        /// </summary>
        public int MaxHydrationMessageCount { get; set; } = 50;

        /// <summary>
        /// Maximum character length for a single persisted message. Oversized messages are truncated.
        /// </summary>
        public int MaxMessageContentLength { get; set; } = 10_000;

        /// <summary>
        /// Maximum total messages per conversation. Once reached, new messages are not persisted.
        /// </summary>
        public int MaxMessagesPerConversation { get; set; } = 100;
    }
}
