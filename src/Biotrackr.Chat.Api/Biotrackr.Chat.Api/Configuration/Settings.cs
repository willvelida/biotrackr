namespace Biotrackr.Chat.Api.Configuration
{
    public class Settings
    {
        public string DatabaseName { get; set; }
        public string ConversationsContainerName { get; set; }
        public string CosmosEndpoint { get; set; }
        public string AgentIdentityId { get; set; }

        /// <summary>
        /// APIM gateway URL of the Biotrackr MCP Server (e.g., https://api-biotrackr-dev.azure-api.net/mcp).
        /// Used by McpToolService with HttpTransportMode.AutoDetect (Streamable HTTP preferred, SSE fallback).
        /// </summary>
        public string McpServerUrl { get; set; }
        public string ApiSubscriptionKey { get; set; }
        public string McpServerApiKey { get; set; }
        public string AnthropicApiKey { get; set; }
        public string ChatAgentModel { get; set; }
        public string ChatSystemPrompt { get; set; }
        public int ToolCallBudgetPerSession { get; set; } = 20;

        /// <summary>
        /// APIM gateway URL of the Reporting API (e.g., https://api-biotrackr-dev.azure-api.net/reporting).
        /// Used by RequestReport tool to submit report generation jobs.
        /// </summary>
        public string ReportingApiUrl { get; set; }

        /// <summary>
        /// OAuth scope for acquiring an agent identity token to call Reporting.Api (ASI07).
        /// Typically the Reporting.Api's application ID URI + /.default.
        /// </summary>
        public string ReportingApiScope { get; set; } = string.Empty;

        /// <summary>
        /// Azure Blob Storage endpoint for report artifacts.
        /// Used by GetReportStatus tool to read metadata.json and generate SAS URLs.
        /// </summary>
        public string ReportingBlobStorageEndpoint { get; set; }

        /// <summary>
        /// System prompt for the Reviewer Agent (loaded from Key Vault).
        /// Used by GetReportStatus tool to validate reports before presenting to user.
        /// </summary>
        public string ReviewerSystemPrompt { get; set; }

        /// <summary>
        /// TTL in seconds for conversation documents in Cosmos DB. Defaults to 90 days.
        /// </summary>
        public int ConversationTtlSeconds { get; set; } = 7_776_000;
    }
}
