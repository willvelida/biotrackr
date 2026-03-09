using Biotrackr.Chat.Api.Models;

namespace Biotrackr.Chat.Api.Services
{
    public interface IChatHistoryRepository
    {
        Task<ChatConversationDocument?> GetConversationAsync(string sessionId);
        Task<PaginationResponse<ChatConversationSummary>> GetConversationsAsync(PaginationRequest pagination);
        Task<ChatConversationDocument> SaveMessageAsync(string sessionId, string role, string content, List<string>? toolCalls = null);
        Task DeleteConversationAsync(string sessionId);
    }
}
