using Biotrackr.Chat.Api.Configuration;
using Biotrackr.Chat.Api.Models;
using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.Options;

namespace Biotrackr.Chat.Api.Services
{
    public class ChatHistoryRepository : IChatHistoryRepository
    {
        private readonly ICosmosClientFactory _cosmosClientFactory;
        private readonly Settings _settings;
        private readonly ILogger<ChatHistoryRepository> _logger;

        public ChatHistoryRepository(
            ICosmosClientFactory cosmosClientFactory,
            IOptions<Settings> options,
            ILogger<ChatHistoryRepository> logger)
        {
            _cosmosClientFactory = cosmosClientFactory;
            _settings = options.Value;
            _logger = logger;
        }

        private Container GetContainer()
        {
            var client = _cosmosClientFactory.Create();
            return client.GetContainer(_settings.DatabaseName, _settings.ConversationsContainerName);
        }

        public async Task<ChatConversationDocument?> GetConversationAsync(string sessionId)
        {
            _logger.LogInformation("Getting conversation {SessionId}", sessionId);

            try
            {
                var container = GetContainer();
                var response = await container.ReadItemAsync<ChatConversationDocument>(
                    sessionId, new PartitionKey(sessionId));
                return response.Resource;
            }
            catch (CosmosException ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                _logger.LogInformation("Conversation {SessionId} not found", sessionId);
                return null;
            }
        }

        public async Task<PaginationResponse<ChatConversationSummary>> GetConversationsAsync(
            PaginationRequest pagination)
        {
            _logger.LogInformation("Fetching conversations: Page={PageNumber}, Size={PageSize}",
                pagination.PageNumber, pagination.PageSize);

            var totalCount = await GetTotalConversationCount();

            var queryDefinition = new QueryDefinition(
                "SELECT c.sessionId, c.title, c.lastUpdated FROM c ORDER BY c.lastUpdated DESC OFFSET @offset LIMIT @limit")
                .WithParameter("@offset", pagination.Skip)
                .WithParameter("@limit", pagination.PageSize);

            var container = GetContainer();
            var iterator = container.GetItemQueryIterator<ChatConversationSummary>(queryDefinition);
            var results = new List<ChatConversationSummary>();

            while (iterator.HasMoreResults)
            {
                var response = await iterator.ReadNextAsync();
                results.AddRange(response);
            }

            _logger.LogInformation("Found {Count} conversations (page {PageNumber})",
                results.Count, pagination.PageNumber);

            return new PaginationResponse<ChatConversationSummary>
            {
                Items = results,
                TotalCount = totalCount,
                PageNumber = pagination.PageNumber,
                PageSize = pagination.PageSize
            };
        }

        public async Task<ChatConversationDocument> SaveMessageAsync(
            string sessionId, string role, string content, List<string>? toolCalls = null)
        {
            _logger.LogInformation("Saving {Role} message to conversation {SessionId}", role, sessionId);

            ChatConversationDocument conversation;
            var container = GetContainer();
            try
            {
                var response = await container.ReadItemAsync<ChatConversationDocument>(
                    sessionId, new PartitionKey(sessionId));
                conversation = response.Resource;
            }
            catch (CosmosException ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                conversation = new ChatConversationDocument
                {
                    Id = sessionId,
                    SessionId = sessionId,
                    Title = "New conversation"
                };
            }

            conversation.Messages.Add(new ChatMessage
            {
                Role = role,
                Content = content,
                Timestamp = DateTime.UtcNow,
                ToolCalls = toolCalls
            });
            conversation.LastUpdated = DateTime.UtcNow;

            // Auto-title from first user message
            if (conversation.Title == "New conversation" && role == "user")
            {
                conversation.Title = content.Length > 50 ? content[..50] + "..." : content;
            }

            await container.UpsertItemAsync(conversation, new PartitionKey(sessionId));

            _logger.LogInformation("Saved message to conversation {SessionId}, total messages: {Count}",
                sessionId, conversation.Messages.Count);

            return conversation;
        }

        public async Task DeleteConversationAsync(string sessionId)
        {
            _logger.LogInformation("Deleting conversation {SessionId}", sessionId);
            var container = GetContainer();
            await container.DeleteItemAsync<ChatConversationDocument>(
                sessionId, new PartitionKey(sessionId));
        }

        private async Task<int> GetTotalConversationCount()
        {
            var countQuery = new QueryDefinition("SELECT VALUE COUNT(1) FROM c");
            var container = GetContainer();
            var iterator = container.GetItemQueryIterator<int>(countQuery);

            while (iterator.HasMoreResults)
            {
                var response = await iterator.ReadNextAsync();
                return response.FirstOrDefault();
            }

            return 0;
        }
    }
}
