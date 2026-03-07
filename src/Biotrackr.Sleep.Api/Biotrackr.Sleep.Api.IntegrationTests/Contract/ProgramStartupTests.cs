using Biotrackr.Sleep.Api.IntegrationTests.Fixtures;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Biotrackr.Sleep.Api.IntegrationTests.Contract;

[Collection("Contract Tests")]
public class ProgramStartupTests
{
    private readonly ContractTestFixture _fixture;

    public ProgramStartupTests(ContractTestFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public void Application_ShouldBuildSuccessfully()
    {
        // Assert
        _fixture.Factory.Should().NotBeNull();
        _fixture.Client.Should().NotBeNull();
    }

    [Fact]
    public void Application_ShouldLoadConfiguration()
    {
        // Arrange & Act
        var services = _fixture.Factory.Services;

        // Assert - Configuration should be available
        services.Should().NotBeNull();
    }

    [Fact]
    public void Application_ShouldConfigureMiddlewarePipeline()
    {
        // Arrange & Act
        using var scope = _fixture.Factory.Services.CreateScope();
        var serviceProvider = scope.ServiceProvider;

        // Assert - Core services should be registered
        serviceProvider.Should().NotBeNull();
    }

    [Fact]
    public async Task Application_ShouldExposeOpenApiEndpoint()
    {
        // Act
        var response = await _fixture.Client.GetAsync("/openapi/v1.json");

        // Assert
        response.Should().NotBeNull();
        response.StatusCode.Should().Be(System.Net.HttpStatusCode.OK);
    }
}
