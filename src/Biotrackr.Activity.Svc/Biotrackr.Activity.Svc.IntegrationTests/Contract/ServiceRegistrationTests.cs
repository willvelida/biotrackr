using Biotrackr.Activity.Svc.IntegrationTests.Collections;
using Biotrackr.Activity.Svc.IntegrationTests.Fixtures;
using Biotrackr.Activity.Svc.Repositories.Interfaces;
using Biotrackr.Activity.Svc.Services.Interfaces;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;

namespace Biotrackr.Activity.Svc.IntegrationTests.Contract;

/// <summary>
/// Contract tests verifying service lifetime registrations match expected patterns.
/// Tests follow guidelines from docs/decision-records/2025-10-28-service-lifetime-registration.md
/// </summary>
[Collection(nameof(ContractTestCollection))]
public class ServiceRegistrationTests(ContractTestFixture fixture)
{
    private readonly ContractTestFixture _fixture = fixture;

    [Fact]
    public void CosmosClient_Should_BeRegisteredAsSingleton()
    {
        // Arrange
        var serviceProvider = _fixture.ServiceProvider;

        // Act - Resolve service twice
        var instance1 = serviceProvider.GetService<Microsoft.Azure.Cosmos.CosmosClient>();
        var instance2 = serviceProvider.GetService<Microsoft.Azure.Cosmos.CosmosClient>();

        // Assert - Singleton services return same instance
        instance1.Should().NotBeNull("CosmosClient should be registered");
        instance2.Should().NotBeNull("CosmosClient should be registered");
        instance1.Should().BeSameAs(instance2, "CosmosClient should be registered as Singleton");
    }

    [Fact]
    public void SecretClient_Should_BeRegisteredAsSingleton()
    {
        // Arrange
        var serviceProvider = _fixture.ServiceProvider;

        // Act - Resolve service twice
        var instance1 = serviceProvider.GetService<Azure.Security.KeyVault.Secrets.SecretClient>();
        var instance2 = serviceProvider.GetService<Azure.Security.KeyVault.Secrets.SecretClient>();

        // Assert - Singleton services return same instance
        instance1.Should().NotBeNull("SecretClient should be registered");
        instance2.Should().NotBeNull("SecretClient should be registered");
        instance1.Should().BeSameAs(instance2, "SecretClient should be registered as Singleton");
    }

    [Fact]
    public void CosmosRepository_Should_BeRegisteredAsScoped()
    {
        // Arrange
        var serviceProvider = _fixture.ServiceProvider;

        // Act & Assert - Scoped services return same instance within scope
        using (var scope = serviceProvider.CreateScope())
        {
            var instance1 = scope.ServiceProvider.GetService<ICosmosRepository>();
            var instance2 = scope.ServiceProvider.GetService<ICosmosRepository>();

            instance1.Should().NotBeNull("ICosmosRepository should be registered");
            instance2.Should().NotBeNull("ICosmosRepository should be registered");
            instance1.Should().BeSameAs(instance2, "ICosmosRepository should return same instance within scope");
        }

        // Verify different instances across scopes
        ICosmosRepository? scopedInstance1;
        ICosmosRepository? scopedInstance2;

        using (var scope1 = serviceProvider.CreateScope())
        {
            scopedInstance1 = scope1.ServiceProvider.GetService<ICosmosRepository>();
        }

        using (var scope2 = serviceProvider.CreateScope())
        {
            scopedInstance2 = scope2.ServiceProvider.GetService<ICosmosRepository>();
        }

        ReferenceEquals(scopedInstance1, scopedInstance2).Should().BeFalse("ICosmosRepository should return different instances across scopes");
    }

    [Fact]
    public void ActivityService_Should_BeRegisteredAsScoped()
    {
        // Arrange
        var serviceProvider = _fixture.ServiceProvider;

        // Act & Assert - Scoped services return same instance within scope
        using (var scope = serviceProvider.CreateScope())
        {
            var instance1 = scope.ServiceProvider.GetService<IActivityService>();
            var instance2 = scope.ServiceProvider.GetService<IActivityService>();

            instance1.Should().NotBeNull("IActivityService should be registered");
            instance2.Should().NotBeNull("IActivityService should be registered");
            instance1.Should().BeSameAs(instance2, "IActivityService should return same instance within scope");
        }

        // Verify different instances across scopes
        IActivityService? scopedInstance1;
        IActivityService? scopedInstance2;

        using (var scope1 = serviceProvider.CreateScope())
        {
            scopedInstance1 = scope1.ServiceProvider.GetService<IActivityService>();
        }

        using (var scope2 = serviceProvider.CreateScope())
        {
            scopedInstance2 = scope2.ServiceProvider.GetService<IActivityService>();
        }

        ReferenceEquals(scopedInstance1, scopedInstance2).Should().BeFalse("IActivityService should return different instances across scopes");
    }

    [Fact]
    public void FitbitService_Should_BeRegisteredAsTransient()
    {
        // Arrange
        var serviceProvider = _fixture.ServiceProvider;

        // Act - Resolve service twice within same scope
        using var scope = serviceProvider.CreateScope();
        var instance1 = scope.ServiceProvider.GetService<IFitbitService>();
        var instance2 = scope.ServiceProvider.GetService<IFitbitService>();

        // Assert - Transient services return different instances every time
        instance1.Should().NotBeNull("IFitbitService should be registered");
        instance2.Should().NotBeNull("IFitbitService should be registered");
        instance1.Should().NotBeSameAs(instance2, "IFitbitService should be registered as Transient (HttpClient-based service)");
    }
}
