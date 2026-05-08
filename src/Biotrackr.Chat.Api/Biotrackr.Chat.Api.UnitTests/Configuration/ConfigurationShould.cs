using Biotrackr.Chat.Api.Configuration;
using FluentAssertions;

namespace Biotrackr.Chat.Api.UnitTests.Configuration;

public class ConfigurationShould
{
    [Fact]
    public void ConversationPolicyOptions_ShouldHaveCorrectDefaults()
    {
        // Arrange & Act
        var options = new ConversationPolicyOptions();

        // Assert
        options.MaxHydrationMessageCount.Should().Be(50);
        options.MaxMessageContentLength.Should().Be(10_000);
        options.MaxMessagesPerConversation.Should().Be(100);
    }

    [Fact]
    public void ConversationPolicyOptions_ShouldAllowCustomValues()
    {
        // Arrange & Act
        var options = new ConversationPolicyOptions
        {
            MaxHydrationMessageCount = 25,
            MaxMessageContentLength = 5000,
            MaxMessagesPerConversation = 50
        };

        // Assert
        options.MaxHydrationMessageCount.Should().Be(25);
        options.MaxMessageContentLength.Should().Be(5000);
        options.MaxMessagesPerConversation.Should().Be(50);
    }

    [Fact]
    public void ToolPolicyOptions_ShouldHaveCorrectDefaults()
    {
        // Arrange & Act
        var options = new ToolPolicyOptions();

        // Assert
        options.MaxToolCallsPerSession.Should().Be(20);
        options.AllowedToolNames.Should().NotBeEmpty();
        options.AllowedToolNames.Should().Contain("get_activity_by_date");
        options.AllowedToolNames.Should().Contain("get_sleep_by_date");
        options.AllowedToolNames.Should().Contain("get_vitals_by_date");
        options.AllowedToolNames.Should().Contain("get_food_by_date");
        options.AllowedToolNames.Should().Contain("GenerateReport");
        options.AllowedToolNames.Should().Contain("CheckReportStatus");
    }

    [Fact]
    public void ToolPolicyOptions_ShouldAllowCustomToolCallLimit()
    {
        // Arrange & Act
        var options = new ToolPolicyOptions
        {
            MaxToolCallsPerSession = 50
        };

        // Assert
        options.MaxToolCallsPerSession.Should().Be(50);
    }

    [Fact]
    public void ToolPolicyOptions_AllowedToolNames_ShouldContainAllMcpTools()
    {
        // Arrange & Act
        var options = new ToolPolicyOptions();

        // Assert
        options.AllowedToolNames.Should().Contain("get_activity_by_date_range");
        options.AllowedToolNames.Should().Contain("get_activity_records");
        options.AllowedToolNames.Should().Contain("get_sleep_by_date_range");
        options.AllowedToolNames.Should().Contain("get_sleep_records");
        options.AllowedToolNames.Should().Contain("get_vitals_by_date_range");
        options.AllowedToolNames.Should().Contain("get_vitals_records");
        options.AllowedToolNames.Should().Contain("get_food_by_date_range");
        options.AllowedToolNames.Should().Contain("get_food_records");
    }

    [Fact]
    public void Settings_ShouldHaveCorrectDefaults()
    {
        // Arrange & Act
        var settings = new Settings();

        // Assert
        settings.ToolCallBudgetPerSession.Should().Be(20);
        settings.ReportingApiScope.Should().BeEmpty();
        settings.ConversationTtlSeconds.Should().Be(7_776_000);
    }

    [Fact]
    public void Settings_ShouldAllowSettingAllProperties()
    {
        // Arrange & Act
        var settings = new Settings
        {
            DatabaseName = "biotrackr-db",
            ConversationsContainerName = "conversations",
            CosmosEndpoint = "https://cosmos.example.com",
            AgentIdentityId = "agent-123",
            McpServerUrl = "https://mcp.example.com",
            ApiSubscriptionKey = "sub-key",
            McpServerApiKey = "api-key",
            AnthropicApiKey = "anthropic-key",
            ChatAgentModel = "claude-sonnet-4-20250514",
            ChatSystemPrompt = "You are a health assistant",
            ToolCallBudgetPerSession = 30,
            ReportingApiUrl = "https://reporting.example.com",
            ReportingApiScope = "api://reporting/.default",
            ReportingBlobStorageEndpoint = "https://blob.example.com",
            ReviewerSystemPrompt = "You are a reviewer",
            ConversationTtlSeconds = 86400
        };

        // Assert
        settings.DatabaseName.Should().Be("biotrackr-db");
        settings.ConversationsContainerName.Should().Be("conversations");
        settings.CosmosEndpoint.Should().Be("https://cosmos.example.com");
        settings.AgentIdentityId.Should().Be("agent-123");
        settings.McpServerUrl.Should().Be("https://mcp.example.com");
        settings.ApiSubscriptionKey.Should().Be("sub-key");
        settings.McpServerApiKey.Should().Be("api-key");
        settings.AnthropicApiKey.Should().Be("anthropic-key");
        settings.ChatAgentModel.Should().Be("claude-sonnet-4-20250514");
        settings.ChatSystemPrompt.Should().Be("You are a health assistant");
        settings.ToolCallBudgetPerSession.Should().Be(30);
        settings.ReportingApiUrl.Should().Be("https://reporting.example.com");
        settings.ReportingApiScope.Should().Be("api://reporting/.default");
        settings.ReportingBlobStorageEndpoint.Should().Be("https://blob.example.com");
        settings.ReviewerSystemPrompt.Should().Be("You are a reviewer");
        settings.ConversationTtlSeconds.Should().Be(86400);
    }
}
