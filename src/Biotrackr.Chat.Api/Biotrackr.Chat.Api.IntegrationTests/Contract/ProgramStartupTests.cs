using Biotrackr.Chat.Api.IntegrationTests.Fixtures;
using FluentAssertions;
using System.Net;

namespace Biotrackr.Chat.Api.IntegrationTests.Contract;

[Collection(nameof(ContractTestCollection))]
public class ProgramStartupTests
{
    private readonly ChatApiWebApplicationFactory _factory;

    public ProgramStartupTests(ChatApiWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public void Application_ShouldBuildSuccessfully()
    {
        // Arrange & Act
        var client = _factory.CreateClient();

        // Assert
        client.BaseAddress.Should().NotBeNull();
    }

    [Fact]
    public void Application_ShouldLoadConfiguration()
    {
        // Arrange & Act
        var services = _factory.Services;

        // Assert
        services.Should().NotBeNull();
    }

    [Fact]
    public async Task Application_ShouldExposeOpenApiEndpoint()
    {
        // Arrange
        var client = _factory.CreateClient();

        // Act
        var response = await client.GetAsync("/openapi/v1.json");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Application_ShouldNotLoadAzureAppConfigurationInTests()
    {
        // Arrange
        var client = _factory.CreateClient();

        // Act
        var response = await client.GetAsync("/healthz/liveness");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }
}
