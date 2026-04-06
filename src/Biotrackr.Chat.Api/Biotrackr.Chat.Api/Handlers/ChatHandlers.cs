using Biotrackr.Chat.Api.Models;
using Biotrackr.Chat.Api.Services;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Http.HttpResults;

namespace Biotrackr.Chat.Api.Handlers
{
    public static class ChatHandlers
    {
        public static async Task<Ok<PaginationResponse<ChatConversationSummary>>> GetConversations(
            IChatHistoryRepository repository,
            int? pageNumber = null,
            int? pageSize = null)
        {
            var paginationRequest = new PaginationRequest
            {
                PageNumber = pageNumber ?? 1,
                PageSize = pageSize ?? 20
            };

            var conversations = await repository.GetConversationsAsync(paginationRequest);
            return TypedResults.Ok(conversations);
        }

        public static async Task<Results<NotFound, Ok<ChatConversationDocument>>> GetConversation(
            IChatHistoryRepository repository,
            string sessionId)
        {
            var conversation = await repository.GetConversationAsync(sessionId);
            if (conversation is null)
            {
                return TypedResults.NotFound();
            }
            return TypedResults.Ok(conversation);
        }

        public static async Task<NoContent> DeleteConversation(
            IChatHistoryRepository repository,
            string sessionId)
        {
            await repository.DeleteConversationAsync(sessionId);
            return TypedResults.NoContent();
        }

        public static async Task<Results<NotFound, StatusCodeHttpResult, Ok<ReportStatusProxyResponse>>> GetReportStatus(
            IHttpClientFactory httpClientFactory,
            string jobId,
            ILoggerFactory loggerFactory)
        {
            try
            {
                var client = httpClientFactory.CreateClient("ReportingApi");
                var response = await client.GetAsync($"/api/reports/{jobId}");

                if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
                {
                    return TypedResults.NotFound();
                }

                response.EnsureSuccessStatusCode();

                var content = await response.Content.ReadFromJsonAsync<ReportingApiResponse>();
                return TypedResults.Ok(new ReportStatusProxyResponse
                {
                    JobId = content?.Metadata?.JobId ?? jobId,
                    Status = content?.Metadata?.Status ?? "unknown"
                });
            }
            catch (HttpRequestException ex)
            {
                var logger = loggerFactory.CreateLogger("ChatHandlers.GetReportStatus");
                logger.LogWarning(ex, "Failed to get report status for job {JobId}", jobId);
                return TypedResults.StatusCode(502);
            }
        }
    }

    public sealed class ReportStatusProxyResponse
    {
        public string JobId { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
    }

    internal sealed class ReportingApiResponse
    {
        public ReportingApiMetadata? Metadata { get; set; }
    }

    internal sealed class ReportingApiMetadata
    {
        public string? JobId { get; set; }
        public string? Status { get; set; }
    }
}
