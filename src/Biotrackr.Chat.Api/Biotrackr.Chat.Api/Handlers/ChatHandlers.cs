using Biotrackr.Chat.Api.Models;
using Biotrackr.Chat.Api.Services;
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
    }
}
