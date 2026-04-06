using System.Diagnostics;
using System.Net.Http.Json;
using System.Runtime.CompilerServices;
using System.Text.Json;
using Biotrackr.UI.Models;
using Biotrackr.UI.Models.Chat;
using Biotrackr.UI.Telemetry;

namespace Biotrackr.UI.Services
{
    public class ChatApiService : IChatApiService
    {
        private readonly HttpClient _httpClient;
        private readonly ILogger<ChatApiService> _logger;

        private static readonly JsonSerializerOptions JsonOptions = new()
        {
            PropertyNameCaseInsensitive = true
        };

        public ChatApiService(HttpClient httpClient, ILogger<ChatApiService> logger)
        {
            _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async IAsyncEnumerable<AGUIEvent> SendMessageAsync(
            string? conversationId,
            string message,
            [EnumeratorCancellation] CancellationToken ct = default)
        {
            using var activity = ChatTelemetry.StartSendMessage(conversationId, message.Length);
            var stopwatch = Stopwatch.StartNew();
            bool firstTokenReceived = false;
            int tokenCount = 0;

            ChatTelemetry.MessagesSent.Add(1);
            _logger.LogInformation("Sending chat message: SessionId={SessionId}, Length={Length}, IsNew={IsNew}",
                conversationId ?? "new", message.Length, conversationId is null);

            HttpResponseMessage response;
            try
            {
                var requestBody = new
                {
                    threadId = conversationId,
                    messages = new[]
                    {
                        new { id = Guid.NewGuid().ToString(), role = "user", content = message }
                    },
                    runId = Guid.NewGuid().ToString()
                };

                var request = new HttpRequestMessage(HttpMethod.Post, "")
                {
                    Content = JsonContent.Create(requestBody)
                };

                response = await _httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, ct);
                response.EnsureSuccessStatusCode();
            }
            catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
            {
                _logger.LogWarning(ex, "Failed to send chat message: SessionId={SessionId}, ErrorType={ErrorType}, Duration={DurationMs}ms",
                    conversationId ?? "new", ex.GetType().Name, stopwatch.Elapsed.TotalMilliseconds);
                ChatTelemetry.StreamErrors.Add(1, new KeyValuePair<string, object?>("error.type", ex.GetType().Name));
                activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
                yield break;
            }

            _logger.LogInformation("Streaming started: SessionId={SessionId}", conversationId ?? "new");

            using var stream = await response.Content.ReadAsStreamAsync(ct);
            using var reader = new StreamReader(stream);

            string? line;
            while ((line = await reader.ReadLineAsync(ct)) is not null && !ct.IsCancellationRequested)
            {
                if (!line.StartsWith("data: "))
                    continue;

                var json = line[6..];
                AGUIEvent? evt;
                try
                {
                    evt = JsonSerializer.Deserialize<AGUIEvent>(json, JsonOptions);
                }
                catch (JsonException)
                {
                    _logger.LogWarning("SSE parse error: SessionId={SessionId}, RawLine={RawLine}",
                        conversationId ?? "new", line);
                    ChatTelemetry.StreamErrors.Add(1, new KeyValuePair<string, object?>("error.type", "JsonException"));
                    continue;
                }

                if (evt is null)
                    continue;

                if (evt.Type == "TEXT_MESSAGE_CONTENT")
                {
                    if (!firstTokenReceived)
                    {
                        var ttft = stopwatch.Elapsed.TotalMilliseconds;
                        ChatTelemetry.TimeToFirstToken.Record(ttft);
                        _logger.LogInformation("First token received: SessionId={SessionId}, TTFT={TimeToFirstTokenMs}ms",
                            conversationId ?? "new", ttft);
                        firstTokenReceived = true;
                    }
                    tokenCount++;
                }
                else if (evt.Type == "TOOL_CALL_START")
                {
                    ChatTelemetry.ToolCalls.Add(1, new KeyValuePair<string, object?>("tool.name", evt.Delta ?? "unknown"));
                }

                yield return evt;
            }

            ChatTelemetry.StreamDuration.Record(stopwatch.Elapsed.TotalMilliseconds);
            ChatTelemetry.TokenCount.Record(tokenCount);
            ChatTelemetry.MessagesReceived.Add(1);
            activity?.SetTag("chat.token_count", tokenCount);
            _logger.LogInformation("Streaming completed: SessionId={SessionId}, Tokens={TokenCount}, Duration={DurationMs}ms",
                conversationId ?? "new", tokenCount, stopwatch.Elapsed.TotalMilliseconds);
        }

        public async Task<PaginatedResponse<ChatConversationSummary>> GetConversationsAsync(
            int pageNumber = 1, int pageSize = 20)
        {
            pageNumber = Math.Max(1, pageNumber);
            pageSize = Math.Clamp(pageSize, 1, 100);
            var endpoint = $"conversations?pageNumber={pageNumber}&pageSize={pageSize}";

            return await GetAsync<PaginatedResponse<ChatConversationSummary>>(endpoint)
                   ?? new PaginatedResponse<ChatConversationSummary>();
        }

        public async Task<ChatConversationDocument?> GetConversationAsync(string sessionId)
        {
            using var activity = ChatTelemetry.StartLoadConversation(sessionId);
            ChatTelemetry.ConversationsLoaded.Add(1);

            var result = await GetAsync<ChatConversationDocument>($"conversations/{sessionId}");
            if (result is not null)
            {
                _logger.LogInformation("Conversation loaded: SessionId={SessionId}, MessageCount={MessageCount}",
                    sessionId, result.Messages.Count);
                activity?.SetTag("chat.message_count", result.Messages.Count);
            }
            return result;
        }

        public async Task DeleteConversationAsync(string sessionId)
        {
            try
            {
                _logger.LogInformation("Deleting conversation: SessionId={SessionId}", sessionId);
                var response = await _httpClient.DeleteAsync($"conversations/{sessionId}");

                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogWarning("Delete conversation returned {StatusCode}: SessionId={SessionId}",
                        response.StatusCode, sessionId);
                    return;
                }

                ChatTelemetry.ConversationsDeleted.Add(1);
                _logger.LogInformation("Conversation deleted: SessionId={SessionId}", sessionId);
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError(ex, "Network error deleting conversation: SessionId={SessionId}", sessionId);
            }
            catch (TaskCanceledException ex)
            {
                _logger.LogError(ex, "Timeout deleting conversation: SessionId={SessionId}", sessionId);
            }
        }

        public async Task<ReportStatusResponse?> GetReportStatusAsync(string jobId)
        {
            return await GetAsync<ReportStatusResponse>($"reports/{jobId}/status");
        }

        private async Task<T?> GetAsync<T>(string endpoint) where T : class
        {
            try
            {
                _logger.LogInformation("Fetching data from {Endpoint}", endpoint);
                var response = await _httpClient.GetAsync(endpoint);

                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogWarning("API call to {Endpoint} returned {StatusCode}", endpoint, response.StatusCode);
                    return null;
                }

                var content = await response.Content.ReadAsStringAsync();
                return JsonSerializer.Deserialize<T>(content, JsonOptions);
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError(ex, "Network error calling {Endpoint}", endpoint);
                return null;
            }
            catch (TaskCanceledException ex)
            {
                _logger.LogError(ex, "Request to {Endpoint} timed out", endpoint);
                return null;
            }
            catch (JsonException ex)
            {
                _logger.LogError(ex, "Failed to deserialize response from {Endpoint}", endpoint);
                return null;
            }
        }
    }
}
