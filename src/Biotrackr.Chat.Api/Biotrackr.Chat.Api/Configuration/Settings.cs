namespace Biotrackr.Chat.Api.Configuration
{
    public class Settings
    {
        public string DatabaseName { get; set; }
        public string ConversationsContainerName { get; set; }
        public string CosmosEndpoint { get; set; }
        public string AgentIdentityId { get; set; }
        public string ApiBaseUrl { get; set; }
        public string ApiSubscriptionKey { get; set; }
        public string AnthropicApiKey { get; set; }
        public string ChatAgentModel { get; set; }
        public string ChatSystemPrompt { get; set; }
        public int ToolCallBudgetPerSession { get; set; } = 20;

        /// <summary>
        /// TTL in seconds for conversation documents in Cosmos DB. Defaults to 90 days.
        /// </summary>
        public int ConversationTtlSeconds { get; set; } = 7_776_000;
    }
}
