using System.Net;
using Biotrackr.Reporting.Api.IntegrationTests.Fixtures;
using FluentAssertions;

namespace Biotrackr.Reporting.Api.IntegrationTests.Contract;

/// <summary>
/// Contract tests verifying the ChatApiAgent authorization policy accepts
/// multiple authorized caller identities (Chat.Api and Reporting.Svc).
/// Each test creates its own factory to isolate configuration.
/// Serialized via collection to avoid static ConfigurableAuthHandler race conditions.
/// </summary>
[Collection("AuthorizationPolicyTests")]
public class AuthorizationPolicyShould
{
    [Fact]
    public async Task RequireAuthentication_ShouldRejectUnauthenticatedRequests()
    {
        // Arrange
        await using var factory = new AuthorizationTestWebApplicationFactory
        {
            ChatApiAgentIdentityId = "chat-api-identity",
            ReportingSvcAgentIdentityId = "reporting-svc-identity"
        };
        var client = factory.CreateClient();
        ConfigurableAuthHandler.AzpClaimValue = string.Empty;

        // Act — request with no azp claim should be rejected by policy
        var response = await client.GetAsync("/api/reports");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task AcceptChatApiIdentity_ShouldReturn200_WhenBothIdentitiesConfigured()
    {
        // Arrange
        await using var factory = new AuthorizationTestWebApplicationFactory
        {
            ChatApiAgentIdentityId = "chat-api-identity",
            ReportingSvcAgentIdentityId = "reporting-svc-identity"
        };
        var client = factory.CreateClient();
        ConfigurableAuthHandler.AzpClaimValue = "chat-api-identity";

        // Act
        var response = await client.GetAsync("/api/reports");

        // Assert
        response.StatusCode.Should().NotBe(HttpStatusCode.Unauthorized);
        response.StatusCode.Should().NotBe(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task AcceptReportingSvcIdentity_ShouldReturn200_WhenBothIdentitiesConfigured()
    {
        // Arrange
        await using var factory = new AuthorizationTestWebApplicationFactory
        {
            ChatApiAgentIdentityId = "chat-api-identity",
            ReportingSvcAgentIdentityId = "reporting-svc-identity"
        };
        var client = factory.CreateClient();
        ConfigurableAuthHandler.AzpClaimValue = "reporting-svc-identity";

        // Act
        var response = await client.GetAsync("/api/reports");

        // Assert
        response.StatusCode.Should().NotBe(HttpStatusCode.Unauthorized);
        response.StatusCode.Should().NotBe(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task AcceptChatApiIdentity_ShouldReturn200_WhenOnlyChatApiIdentityConfigured()
    {
        // Arrange
        await using var factory = new AuthorizationTestWebApplicationFactory
        {
            ChatApiAgentIdentityId = "chat-api-identity",
            ReportingSvcAgentIdentityId = null
        };
        var client = factory.CreateClient();
        ConfigurableAuthHandler.AzpClaimValue = "chat-api-identity";

        // Act
        var response = await client.GetAsync("/api/reports");

        // Assert
        response.StatusCode.Should().NotBe(HttpStatusCode.Unauthorized);
        response.StatusCode.Should().NotBe(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task RejectUnknownIdentity_ShouldReturn403_WhenAzpDoesNotMatch()
    {
        // Arrange
        await using var factory = new AuthorizationTestWebApplicationFactory
        {
            ChatApiAgentIdentityId = "chat-api-identity",
            ReportingSvcAgentIdentityId = "reporting-svc-identity"
        };
        var client = factory.CreateClient();
        ConfigurableAuthHandler.AzpClaimValue = "unknown-identity";

        // Act
        var response = await client.GetAsync("/api/reports");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }
}
