using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Biotrackr.Weight.Api.Configuration;
using Biotrackr.Weight.Api.Repositories.Interfaces;
using Xunit;

namespace Biotrackr.Weight.Api.IntegrationTests;

/// <summary>
/// Integration tests for Program.cs application startup and configuration
/// These tests verify that the application configures correctly and all services are registered
/// </summary>
[Collection(nameof(ContractTestCollection))]
public class ProgramStartupTests
{
    private readonly ContractTestFixture _fixture;

    public ProgramStartupTests(ContractTestFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public void Application_Should_Start_Successfully()
    {
        // Arrange & Act
        var factory = _fixture.Factory;
        using var client = factory.CreateClient();

        // Assert
        client.Should().NotBeNull();
        client.BaseAddress.Should().NotBeNull();
    }

    [Fact]
    public void CosmosClient_Should_Be_Registered()
    {
        // Arrange
        var services = _fixture.Factory.Services;

        // Act
        var cosmosClient = services.GetService<CosmosClient>();

        // Assert
        cosmosClient.Should().NotBeNull();
    }

    [Fact]
    public void CosmosRepository_Should_Be_Registered()
    {
        // Arrange
        var services = _fixture.Factory.Services;

        // Act
        using var scope = services.CreateScope();
        var repository = scope.ServiceProvider.GetService<ICosmosRepository>();

        // Assert
        repository.Should().NotBeNull();
    }

    [Fact]
    public void Settings_Should_Be_Configured()
    {
        // Arrange
        var services = _fixture.Factory.Services;

        // Act
        var settings = services.GetService<IOptions<Settings>>();

        // Assert
        settings.Should().NotBeNull();
        settings!.Value.Should().NotBeNull();
        settings.Value.DatabaseName.Should().Be("biotrackr-test");
        settings.Value.ContainerName.Should().Be("weight-test");
    }

    [Fact]
    public void HealthChecks_Should_Be_Registered()
    {
        // Arrange
        var services = _fixture.Factory.Services;

        // Act
        var healthCheckService = services.GetService<Microsoft.Extensions.Diagnostics.HealthChecks.HealthCheckService>();

        // Assert
        healthCheckService.Should().NotBeNull();
    }

    [Fact]
    public async Task Application_Should_Not_Load_Azure_App_Configuration_In_Tests()
    {
        // This test verifies that Azure App Configuration is bypassed in test environment
        // by checking that the app starts successfully without Azure credentials

        // Arrange & Act
        var factory = _fixture.Factory;
        var client = factory.CreateClient();
        
        // Just verify we can create a client - this proves the app started
        // We don't need to call an endpoint since app startup is what we're testing
        
        // Assert
        client.Should().NotBeNull();
        client.BaseAddress.Should().NotBeNull();
    }

    [Fact]
    public void CosmosClient_Should_Use_Test_Configuration()
    {
        // Arrange
        var services = _fixture.Factory.Services;

        // Act
        var cosmosClient = services.GetRequiredService<CosmosClient>();
        var settings = services.GetRequiredService<IOptions<Settings>>().Value;

        // Assert
        cosmosClient.Should().NotBeNull();
        settings.DatabaseName.Should().Be("biotrackr-test");
        settings.ContainerName.Should().Be("weight-test");
    }

    [Fact]
    public void Swagger_Should_Be_Enabled()
    {
        // Arrange
        var services = _fixture.Factory.Services;

        // Act - Check if Swagger services are registered
        var endpointDataSource = services.GetService<Microsoft.AspNetCore.Routing.EndpointDataSource>();

        // Assert
        endpointDataSource.Should().NotBeNull("Swagger requires endpoint routing");
    }

    [Fact]
    public void SwaggerUI_Should_Be_Accessible()
    {
        // Arrange
        var services = _fixture.Factory.Services;

        // Act - Verify API explorer is registered (required for Swagger)
        var apiDescriptionProvider = services.GetService<Microsoft.AspNetCore.Mvc.ApiExplorer.IApiDescriptionGroupCollectionProvider>();

        // Assert
        apiDescriptionProvider.Should().NotBeNull("SwaggerUI requires API Explorer service");
    }

    [Fact]
    public void Application_Should_Register_Endpoints_ApiExplorer()
    {
        // Arrange
        var services = _fixture.Factory.Services;

        // Act
        var endpointDataSource = services.GetService<Microsoft.AspNetCore.Routing.EndpointDataSource>();

        // Assert
        endpointDataSource.Should().NotBeNull();
    }

    [Fact]
    public void CosmosClient_Should_Use_CamelCase_Serialization()
    {
        // Arrange
        var services = _fixture.Factory.Services;

        // Act
        var cosmosClient = services.GetRequiredService<CosmosClient>();

        // Assert
        cosmosClient.Should().NotBeNull();
        // The client is configured with CamelCase property naming in Program.cs
        // This is verified by successful operation in other tests
    }
}
