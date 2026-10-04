using Biotrackr.Sleep.Api.Configuration;
using Biotrackr.Sleep.Api.IntegrationTests.Fixtures;
using Biotrackr.Sleep.Api.Repositories.Interfaces;
using Biotrackr.Sleep.Api.Services;
using Biotrackr.Sleep.Api.Services.Interfaces;
using FluentAssertions;
using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Xunit;

namespace Biotrackr.Sleep.Api.IntegrationTests.Contract;

[Collection("Contract Tests")]
public class ServiceRegistrationTests
{
    private readonly ContractTestFixture _fixture;

    public ServiceRegistrationTests(ContractTestFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public void CosmosClient_ShouldBeRegisteredAsSingleton()
    {
        // Arrange
        var serviceProvider = _fixture.Factory.Services;

        // Act
        var cosmosClient1 = serviceProvider.GetService<CosmosClient>();
        var cosmosClient2 = serviceProvider.GetService<CosmosClient>();

        // Assert - Same instance across the application
        cosmosClient1.Should().NotBeNull();
        cosmosClient2.Should().NotBeNull();
        cosmosClient1.Should().BeSameAs(cosmosClient2);
    }

    [Fact]
    public void CosmosRepository_ShouldBeRegisteredAsTransient()
    {
        // Arrange
        var serviceProvider = _fixture.Factory.Services;

        // Act - Create two scopes to test scoped behavior
        using var scope1 = serviceProvider.CreateScope();
        using var scope2 = serviceProvider.CreateScope();
        
        var repository1 = scope1.ServiceProvider.GetService<ICosmosRepository>();
        var repository2 = scope2.ServiceProvider.GetService<ICosmosRepository>();

        // Assert - Different instances across scopes (Transient behavior)
        repository1.Should().NotBeNull();
        repository2.Should().NotBeNull();
        repository1.Should().NotBeSameAs(repository2);
    }

    [Fact]
    public void Settings_ShouldBeRegisteredViaIOptions()
    {
        // Arrange
        var serviceProvider = _fixture.Factory.Services;

        // Act
        var settings = serviceProvider.GetService<IOptions<Settings>>();

        // Assert
        settings.Should().NotBeNull();
        settings!.Value.Should().NotBeNull();
    }

    [Fact]
    public void HealthChecks_ShouldBeRegistered()
    {
        // Arrange
        var serviceProvider = _fixture.Factory.Services;

        // Act
        var healthCheckService = serviceProvider.GetService<Microsoft.Extensions.Diagnostics.HealthChecks.HealthCheckService>();

        // Assert
        healthCheckService.Should().NotBeNull();
    }

    [Fact]
    public void SleepDocumentTranslator_ShouldBeRegisteredAsSingleton()
    {
        // Arrange
        var serviceProvider = _fixture.Factory.Services;

        // Act
        using var scope1 = serviceProvider.CreateScope();
        using var scope2 = serviceProvider.CreateScope();
        var translator1 = scope1.ServiceProvider.GetService<ISleepDocumentTranslator>();
        var translator2 = scope2.ServiceProvider.GetService<ISleepDocumentTranslator>();

        // Assert
        translator1.Should().NotBeNull(
            "AGENT FIX: register ISleepDocumentTranslator in Program.cs with AddSingleton<ISleepDocumentTranslator, SleepDocumentTranslator>(); handlers resolve it from DI.");
        translator1.Should().BeOfType<SleepDocumentTranslator>();
        translator1.Should().BeSameAs(translator2,
            "AGENT FIX: SleepDocumentTranslator is stateless and must be a singleton. See .github/instructions/csharp-conventions.instructions.md.");
    }

    [Fact]
    public void Services_ShouldNotHaveDuplicateRegistrations()
    {
        // Arrange
        var serviceProvider = _fixture.Factory.Services;

        // Act - Get all registrations for ICosmosRepository
        var repositoryDescriptors = serviceProvider
            .GetServices<ICosmosRepository>()
            .ToList();

        // Assert - Should have exactly one registration
        repositoryDescriptors.Should().HaveCount(1);
        serviceProvider.GetServices<ISleepDocumentTranslator>().Should().HaveCount(1);
    }
}
