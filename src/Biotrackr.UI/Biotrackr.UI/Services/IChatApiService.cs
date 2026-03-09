using Biotrackr.UI.Models;
using Biotrackr.UI.Models.Chat;

namespace Biotrackr.UI.Services
{
    public interface IChatApiService
    {
        IAsyncEnumerable<AGUIEvent> SendMessageAsync(
            string? conversationId,
            string message,
            CancellationToken ct = default);

        Task<PaginatedResponse<ChatConversationSummary>> GetConversationsAsync(
            int pageNumber = 1,
            int pageSize = 20);

        Task<ChatConversationDocument?> GetConversationAsync(string sessionId);

        Task DeleteConversationAsync(string sessionId);
    }
}
