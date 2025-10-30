using Biotrackr.Activity.Svc.IntegrationTests.Collections;
using Biotrackr.Activity.Svc.IntegrationTests.Fixtures;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Biotrackr.Activity.Svc.IntegrationTests.Contract;

/// <summary>
/// Contract tests verifying the application can start successfully and configuration is valid.
/// These tests do not require external dependencies like Cosmos DB.
/// </summary>
[Collection(nameof(ContractTestCollection))]
public class ProgramStartupTests(ContractTestFixture fixture)
{
    private readonly ContractTestFixture _fixture = fixture;

    [Fact]
    public void ApplicationHost_Should_BuildSuccessfully()
    {
        // Arrange & Act - fixture initializes in constructor
        
        // Assert
        _fixture.ServiceProvider.Should().NotBeNull("application host should build successfully");
    }

    [Fact]
    public void ServiceProvider_Should_ResolveAllRequiredServices()
    {
        // Arrange
        var serviceProvider = _fixture.ServiceProvider;

        // Act & Assert - Verify all critical services can be resolved
        var cosmosClient = serviceProvider.GetService<Microsoft.Azure.Cosmos.CosmosClient>();
        cosmosClient.Should().NotBeNull("CosmosClient should be registered");

        var secretClient = serviceProvider.GetService<Azure.Security.KeyVault.Secrets.SecretClient>();
        secretClient.Should().NotBeNull("SecretClient should be registered");

        var cosmosRepository = serviceProvider.GetService<Biotrackr.Activity.Svc.Repositories.Interfaces.ICosmosRepository>();
        cosmosRepository.Should().NotBeNull("ICosmosRepository should be registered");

        var activityService = serviceProvider.GetService<Biotrackr.Activity.Svc.Services.Interfaces.IActivityService>();
        activityService.Should().NotBeNull("IActivityService should be registered");

        var fitbitService = serviceProvider.GetService<Biotrackr.Activity.Svc.Services.Interfaces.IFitbitService>();
        fitbitService.Should().NotBeNull("IFitbitService should be registered");
    }

    [Fact]
    public void Configuration_Should_ContainAllRequiredKeys()
    {
        // Arrange
        var configuration = _fixture.Configuration;

        // Act & Assert - Verify all required configuration keys are present
        var keyVaultUrl = configuration["keyvaulturl"];
        keyVaultUrl.Should().NotBeNullOrEmpty("keyvaulturl configuration should be present");

        var managedIdentityClientId = configuration["managedidentityclientid"];
        managedIdentityClientId.Should().NotBeNullOrEmpty("managedidentityclientid configuration should be present");

        var cosmosDbEndpoint = configuration["cosmosdbendpoint"];
        cosmosDbEndpoint.Should().NotBeNullOrEmpty("cosmosdbendpoint configuration should be present");

        var appInsightsConnection = configuration["applicationinsightsconnectionstring"];
        appInsightsConnection.Should().NotBeNullOrEmpty("applicationinsightsconnectionstring configuration should be present");
    }
}
