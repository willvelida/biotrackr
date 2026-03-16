using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;
using System.Net;
using System.Runtime.CompilerServices;

namespace Biotrackr.Chat.Api.Middleware
{
    /// <summary>
    /// Agent middleware that catches Claude API failures during streaming
    /// and returns a user-friendly error message instead of crashing the SSE connection.
    /// </summary>
    public class GracefulDegradationMiddleware(
        ILogger<GracefulDegradationMiddleware> logger)
    {
        public const string ServiceUnavailableMessage =
            "I'm sorry, I'm temporarily unable to process your request. The AI service is currently unavailable. Please try again in a few moments.";

        public const string TimeoutMessage =
            "I'm sorry, the request timed out. Please try again.";

        public async IAsyncEnumerable<AgentResponseUpdate> HandleAsync(
            IEnumerable<ChatMessage> messages,
            AgentSession? session,
            AgentRunOptions? options,
            AIAgent innerAgent,
            [EnumeratorCancellation] CancellationToken cancellationToken)
        {
            var sessionId = options.GetConversationId();

            IAsyncEnumerator<AgentResponseUpdate>? enumerator = null;
            try
            {
                enumerator = innerAgent.RunStreamingAsync(messages, session, options, cancellationToken)
                    .GetAsyncEnumerator(cancellationToken);

                while (true)
                {
                    AgentResponseUpdate current;
                    string? errorMessage = null;
                    try
                    {
                        if (!await enumerator.MoveNextAsync())
                            break;
                        current = enumerator.Current;
                    }
                    catch (HttpRequestException ex) when (
                        ex.StatusCode is HttpStatusCode.ServiceUnavailable
                            or HttpStatusCode.TooManyRequests
                            or HttpStatusCode.BadGateway
                            or HttpStatusCode.GatewayTimeout)
                    {
                        logger.LogWarning(ex, "Claude API unavailable ({StatusCode}) for session {SessionId}",
                            ex.StatusCode, sessionId);
                        errorMessage = ServiceUnavailableMessage;
                        current = default!;
                    }
                    catch (TaskCanceledException ex) when (ex.InnerException is TimeoutException)
                    {
                        logger.LogWarning(ex, "Claude API timed out for session {SessionId}", sessionId);
                        errorMessage = TimeoutMessage;
                        current = default!;
                    }

                    if (errorMessage is not null)
                    {
                        yield return new AgentResponseUpdate(
                            ChatRole.Assistant,
                            [new TextContent(errorMessage)]);
                        yield break;
                    }

                    yield return current;
                }
            }
            finally
            {
                if (enumerator is not null)
                    await enumerator.DisposeAsync();
            }
        }
    }
}
