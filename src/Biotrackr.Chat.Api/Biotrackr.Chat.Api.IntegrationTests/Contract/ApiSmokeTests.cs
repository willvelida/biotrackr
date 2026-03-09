using Biotrackr.Chat.Api.IntegrationTests.Fixtures;
using FluentAssertions;
using System.Net;

namespace Biotrackr.Chat.Api.IntegrationTests.Contract;

/// <summary>
/// Smoke tests that verify the API starts up correctly and endpoints are registered.
/// These tests do NOT require a running Cosmos DB emulator — they only verify
/// that the application builds, DI resolves, and routes are mapped.
/// </summary>
public class ApiSmokeTests : IClassFixture<ChatApiWebApplicationFactory>
{
    private readonly HttpClient _client;

    public ApiSmokeTests(ChatApiWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task HealthCheck_ShouldReturn200()
    {
        // Act
        var response = await _client.GetAsync("/healthz/liveness");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task OpenApi_ShouldReturn200()
    {
        // Act
        var response = await _client.GetAsync("/openapi/v1.json");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var content = await response.Content.ReadAsStringAsync();
        content.Should().Contain("GetConversations");
    }
}
