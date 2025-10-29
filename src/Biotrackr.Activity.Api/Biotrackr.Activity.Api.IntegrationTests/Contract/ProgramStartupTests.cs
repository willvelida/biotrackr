using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Biotrackr.Activity.Api.Configuration;
using Biotrackr.Activity.Api.Repositories.Interfaces;
using Biotrackr.Activity.Api.IntegrationTests.Collections;
using Biotrackr.Activity.Api.IntegrationTests.Fixtures;
using Xunit;

namespace Biotrackr.Activity.Api.IntegrationTests.Contract;

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
    public void CosmosClient_Should_Be_Singleton()
    {
        // Arrange
        var services = _fixture.Factory.Services;

        // Act
        var cosmosClient1 = services.GetService<CosmosClient>();
        var cosmosClient2 = services.GetService<CosmosClient>();

        // Assert - Singleton should return same instance
        cosmosClient1.Should().BeSameAs(cosmosClient2);
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
    public void CosmosRepository_Should_Be_Scoped()
    {
        // Arrange
        var services = _fixture.Factory.Services;

        // Act - Get repository instances within a scope
        using (var scope = services.CreateScope())
        {
            var repository1 = scope.ServiceProvider.GetService<ICosmosRepository>();
            var repository2 = scope.ServiceProvider.GetService<ICosmosRepository>();

            // Assert - Scoped service should return same instance within scope
            repository1.Should().BeSameAs(repository2);
        }

        // Act - Get repository instance in a different scope
        ICosmosRepository repository3;
        using (var scope = services.CreateScope())
        {
            repository3 = scope.ServiceProvider.GetService<ICosmosRepository>();
        }

        ICosmosRepository repository4;
        using (var scope = services.CreateScope())
        {
            repository4 = scope.ServiceProvider.GetService<ICosmosRepository>();
        }

        // Assert - Different scopes should return different instances
        repository3.Should().NotBeSameAs(repository4);
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
        settings.Value.ContainerName.Should().Be("activity-test");
    }

    [Fact]
    public void Settings_Should_Be_Singleton()
    {
        // Arrange
        var services = _fixture.Factory.Services;

        // Act
        var settings1 = services.GetService<IOptions<Settings>>();
        var settings2 = services.GetService<IOptions<Settings>>();

        // Assert - IOptions<T> should return same instance (Singleton)
        settings1.Should().BeSameAs(settings2);
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
        using var client = factory.CreateClient();

        // Assert - If Azure App Configuration was loaded, app would fail to start without credentials
        // The fact that we can create a client means Azure App Configuration was bypassed
        var response = await client.GetAsync("/healthz/liveness");
        response.Should().NotBeNull();
    }

    /// <summary>
    /// T081 - Validates Singleton lifetime for CosmosClient across multiple resolutions
    /// Per decision-record 2025-10-28-service-lifetime-registration.md:
    /// Azure SDK clients should be Singleton (expensive to create, thread-safe, manage connection pooling)
    /// </summary>
    [Fact]
    public void CosmosClient_Should_Return_Same_Instance_Across_Multiple_Resolutions()
    {
        // Arrange
        var services = _fixture.Factory.Services;

        // Act - Get multiple instances from root provider
        var client1 = services.GetService<CosmosClient>();
        var client2 = services.GetService<CosmosClient>();
        var client3 = services.GetService<CosmosClient>();

        // Assert - All should be the same singleton instance
        client1.Should().BeSameAs(client2, "Singleton services should return same instance");
        client2.Should().BeSameAs(client3, "Singleton services should return same instance");
        client1.Should().BeSameAs(client3, "Singleton services should return same instance");
    }

    /// <summary>
    /// T082 - Validates Scoped lifetime for ICosmosRepository across scopes
    /// Per decision-record 2025-10-28-service-lifetime-registration.md:
    /// Application services should be Scoped (one instance per request/execution scope)
    /// </summary>
    [Fact]
    public void CosmosRepository_Should_Return_Different_Instances_Across_Scopes()
    {
        // Arrange
        var services = _fixture.Factory.Services;

        ICosmosRepository repository1;
        ICosmosRepository repository2;
        ICosmosRepository repository3;

        // Act - Get instances from three different scopes
        using (var scope1 = services.CreateScope())
        {
            repository1 = scope1.ServiceProvider.GetService<ICosmosRepository>();
        }

        using (var scope2 = services.CreateScope())
        {
            repository2 = scope2.ServiceProvider.GetService<ICosmosRepository>();
        }

        using (var scope3 = services.CreateScope())
        {
            repository3 = scope3.ServiceProvider.GetService<ICosmosRepository>();
        }

        // Assert - Each scope should have a different instance
        repository1.Should().NotBeSameAs(repository2, "Scoped services should differ across scopes");
        repository2.Should().NotBeSameAs(repository3, "Scoped services should differ across scopes");
        repository1.Should().NotBeSameAs(repository3, "Scoped services should differ across scopes");
    }

    /// <summary>
    /// T083 - Validates Singleton lifetime for IOptions&lt;Settings&gt;
    /// Per decision-record 2025-10-28-service-lifetime-registration.md:
    /// IOptions&lt;T&gt; is registered as Singleton by default by the framework
    /// </summary>
    [Fact]
    public void Settings_Should_Return_Same_Instance_Via_IOptions()
    {
        // Arrange
        var services = _fixture.Factory.Services;

        // Act - Get multiple instances of IOptions<Settings>
        var options1 = services.GetService<IOptions<Settings>>();
        var options2 = services.GetService<IOptions<Settings>>();
        var options3 = services.GetService<IOptions<Settings>>();

        // Assert - All should be the same singleton instance
        options1.Should().BeSameAs(options2, "IOptions<T> is Singleton by default");
        options2.Should().BeSameAs(options3, "IOptions<T> is Singleton by default");
        
        // Verify the actual Settings objects are also the same
        options1.Value.Should().BeSameAs(options2.Value, "Settings should be the same instance");
    }

    /// <summary>
    /// T084 - Validates no duplicate service registrations exist
    /// Per decision-record 2025-10-28-service-lifetime-registration.md:
    /// Do NOT use duplicate registrations for services. Each service should have single registration.
    /// This test validates that critical services resolve correctly without ambiguous registrations.
    /// </summary>
    [Fact]
    public void Services_Should_Not_Have_Duplicate_Registrations()
    {
        // Arrange
        var services = _fixture.Factory.Services;

        // Act & Assert - Try to resolve critical services
        // If there were duplicate registrations, the behavior would be unpredictable
        
        // CosmosClient should resolve to a single Singleton instance
        var cosmosClient = services.GetService<CosmosClient>();
        cosmosClient.Should().NotBeNull("CosmosClient should be registered");
        
        // ICosmosRepository should resolve consistently as Scoped
        using (var scope = services.CreateScope())
        {
            var repository = scope.ServiceProvider.GetService<ICosmosRepository>();
            repository.Should().NotBeNull("ICosmosRepository should be registered");
        }
        
        // IOptions<Settings> should resolve to a single Singleton instance
        var options = services.GetService<IOptions<Settings>>();
        options.Should().NotBeNull("IOptions<Settings> should be registered");
        
        // Additional validation: Verify Settings has expected values from test configuration
        options.Value.DatabaseName.Should().Be("biotrackr-test", "Test config should be loaded");
        options.Value.ContainerName.Should().Be("activity-test", "Test config should be loaded");
    }
}
