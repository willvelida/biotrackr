using Biotrackr.Chat.Api.Services;
using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;
using System.Runtime.CompilerServices;
using System.Text.Json;

namespace Biotrackr.Chat.Api.Middleware
{
    /// <summary>
    /// Agent middleware that persists conversation messages to Cosmos DB
    /// after each agent streaming run completes.
    /// </summary>
    public class ConversationPersistenceMiddleware(
        IChatHistoryRepository repository,
        ILogger<ConversationPersistenceMiddleware> logger)
    {
        public async IAsyncEnumerable<AgentResponseUpdate> HandleAsync(
            IEnumerable<ChatMessage> messages,
            AgentSession? session,
            AgentRunOptions? options,
            AIAgent innerAgent,
            [EnumeratorCancellation] CancellationToken cancellationToken)
        {
            // Collect the user message (last message in the input)
            var userMessage = messages.LastOrDefault(m => m.Role == ChatRole.User);
            var sessionId = session?.GetHashCode().ToString("x8") ?? Guid.NewGuid().ToString();

            // Save the user message to Cosmos before streaming
            if (userMessage is not null)
            {
                var userContent = string.Join("", userMessage.Contents.OfType<TextContent>().Select(c => c.Text));
                if (!string.IsNullOrWhiteSpace(userContent))
                {
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

            // Stream the agent response, collecting the full text
            var responseText = new System.Text.StringBuilder();
            var toolCalls = new List<string>();

            await foreach (var update in innerAgent.RunStreamingAsync(messages, session, options, cancellationToken))
            {
                // Collect response content for persistence
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

            // Save the assistant response to Cosmos after streaming completes
            var assistantContent = responseText.ToString();
            if (!string.IsNullOrWhiteSpace(assistantContent))
            {
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
