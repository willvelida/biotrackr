using Biotrackr.Chat.Api.Configuration;
using Biotrackr.Chat.Api.Services;
using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Options;
using System.Runtime.CompilerServices;
using System.Text.Json;

namespace Biotrackr.Chat.Api.Middleware
{
    /// <summary>
    /// Agent middleware that persists conversation messages to Cosmos DB
    /// after each agent streaming run completes. Enforces ASI06 conversation
    /// safety limits: context window cap, message length limit, and conversation cap.
    /// </summary>
    public class ConversationPersistenceMiddleware(
        IChatHistoryRepository repository,
        IOptions<ConversationPolicyOptions> policyOptions,
        ILogger<ConversationPersistenceMiddleware> logger)
    {
        internal const string ConversationCapReachedMessage =
            "This conversation has reached its maximum length. Please start a new conversation to continue.";

        public async IAsyncEnumerable<AgentResponseUpdate> HandleAsync(
            IEnumerable<ChatMessage> messages,
            AgentSession? session,
            AgentRunOptions? options,
            AIAgent innerAgent,
            [EnumeratorCancellation] CancellationToken cancellationToken)
        {
            var policy = policyOptions.Value;
            var userMessage = messages.LastOrDefault(m => m.Role == ChatRole.User);
            var sessionId = options.GetConversationId(Guid.NewGuid().ToString());

            var conversationCapReached = await IsConversationCapReachedAsync(sessionId, policy);

            if (userMessage is not null && !conversationCapReached)
            {
                var userContent = string.Join("", userMessage.Contents.OfType<TextContent>().Select(c => c.Text));
                if (!string.IsNullOrWhiteSpace(userContent))
                {
                    userContent = TruncateContent(userContent, policy.MaxMessageContentLength, sessionId);

                    try
                    {
                        await repository.SaveMessageAsync(sessionId, "user", userContent);
                        logger.LogInformation("Persisted user message for session {SessionId}", sessionId);
                    }
                    catch (Exception ex)
                    {
                        logger.LogError(ex,
                            "Failed to persist user message for session {SessionId}. Continuing without persistence.",
                            sessionId);
                    }
                }
            }

            if (conversationCapReached)
            {
                logger.LogWarning(
                    "Conversation cap ({MaxMessages}) reached for session {SessionId}. Message not persisted.",
                    policy.MaxMessagesPerConversation, sessionId);
            }

            var messagesWithHistory = await HydrateConversationHistoryAsync(sessionId, messages, policy);

            var responseText = new System.Text.StringBuilder();
            var toolCalls = new List<string>();

            await foreach (var update in innerAgent.RunStreamingAsync(messagesWithHistory, session, options, cancellationToken))
            {
                foreach (var content in update.Contents)
                {
                    if (content is TextContent textContent)
                    {
                        responseText.Append(textContent.Text);
                    }
                    else if (content is FunctionCallContent functionCall)
                    {
                        toolCalls.Add(functionCall.Name);
                        logger.LogInformation(
                            "Tool call: {ToolName} with arguments: {Arguments} in session {SessionId}",
                            functionCall.Name,
                            MaskSensitiveFields(functionCall.Arguments),
                            sessionId);
                    }
                }

                yield return update;
            }

            var assistantContent = responseText.ToString();
            if (!string.IsNullOrWhiteSpace(assistantContent) && !conversationCapReached)
            {
                assistantContent = TruncateContent(assistantContent, policy.MaxMessageContentLength, sessionId);

                try
                {
                    await repository.SaveMessageAsync(
                        sessionId,
                        "assistant",
                        assistantContent,
                        toolCalls.Count > 0 ? toolCalls : null);
                    logger.LogInformation("Persisted assistant response for session {SessionId} ({ToolCount} tool calls)",
                        sessionId, toolCalls.Count);
                }
                catch (Exception ex)
                {
                    logger.LogError(ex,
                        "Failed to persist assistant response for session {SessionId}. " +
                        "The response was delivered to the user but will not appear in conversation history.",
                        sessionId);
                }
            }

            // Notify user when conversation cap is reached
            if (conversationCapReached)
            {
                yield return new AgentResponseUpdate(
                    ChatRole.Assistant,
                    [new TextContent($"\n\n---\n{ConversationCapReachedMessage}")]);
            }
        }

        /// <summary>
        /// Loads previous conversation messages from Cosmos DB and prepends them
        /// to the current messages so the agent has full conversation context.
        /// Caps hydrated messages to <see cref="ConversationPolicyOptions.MaxHydrationMessageCount"/>.
        /// </summary>
        internal async Task<IEnumerable<ChatMessage>> HydrateConversationHistoryAsync(
            string sessionId, IEnumerable<ChatMessage> currentMessages, ConversationPolicyOptions policy)
        {
            try
            {
                var conversation = await repository.GetConversationAsync(sessionId);
                if (conversation is null || conversation.Messages.Count <= 1)
                {
                    return currentMessages;
                }

                // Exclude the last message (just persisted above) and cap to max hydration count
                var allHistory = conversation.Messages.SkipLast(1);
                var cappedHistory = allHistory.TakeLast(policy.MaxHydrationMessageCount);

                var historicalMessages = cappedHistory
                    .Select(m => new ChatMessage(
                        m.Role == "assistant" ? ChatRole.Assistant : ChatRole.User,
                        m.Content))
                    .ToList();

                var totalHistory = conversation.Messages.Count - 1;
                if (totalHistory > policy.MaxHydrationMessageCount)
                {
                    logger.LogInformation(
                        "Hydration capped: {Hydrated} of {Total} historical messages for session {SessionId} (max {Max})",
                        historicalMessages.Count, totalHistory, sessionId, policy.MaxHydrationMessageCount);
                }
                else
                {
                    logger.LogInformation(
                        "Hydrated {Count} historical messages for session {SessionId}",
                        historicalMessages.Count, sessionId);
                }

                // Prepend history before current messages
                historicalMessages.AddRange(currentMessages);
                return historicalMessages;
            }
            catch (Exception ex)
            {
                logger.LogError(ex,
                    "Failed to hydrate conversation history for session {SessionId}. Continuing without history.",
                    sessionId);
                return currentMessages;
            }
        }

        /// <summary>
        /// Truncates content exceeding <paramref name="maxLength"/> characters.
        /// </summary>
        internal string TruncateContent(string content, int maxLength, string sessionId)
        {
            if (content.Length <= maxLength)
            {
                return content;
            }

            logger.LogWarning(
                "Message content truncated from {OriginalLength} to {MaxLength} characters for session {SessionId}",
                content.Length, maxLength, sessionId);

            return content[..maxLength];
        }

        /// <summary>
        /// Checks whether the conversation has reached the maximum message count.
        /// </summary>
        internal async Task<bool> IsConversationCapReachedAsync(string sessionId, ConversationPolicyOptions policy)
        {
            try
            {
                var conversation = await repository.GetConversationAsync(sessionId);
                return conversation is not null
                    && conversation.Messages.Count >= policy.MaxMessagesPerConversation;
            }
            catch (Exception ex)
            {
                logger.LogError(ex,
                    "Failed to check conversation cap for session {SessionId}. Assuming not reached.",
                    sessionId);
                return false;
            }
        }

        /// <summary>
        /// Serializes tool arguments with defensive PII masking.
        /// </summary>
        public static string MaskSensitiveFields(object? arguments)
        {
            if (arguments is null)
            {
                return "null";
            }

            return JsonSerializer.Serialize(arguments);
        }
    }
}
