using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Biotrackr.Sleep.Api.IntegrationTests.Fixtures;

/// <summary>
/// Base fixture for integration tests
/// Provides shared WebApplicationFactory instance and Cosmos DB setup across tests
/// Per decision-record 2025-10-28-contract-test-architecture.md
/// </summary>
public class IntegrationTestFixture : IAsyncLifetime
{
    public SleepApiWebApplicationFactory Factory { get; private set; } = null!;
    public HttpClient Client { get; private set; } = null!;
    public Container Container { get; private set; } = null!;
    
    /// <summary>
    /// Set to false to skip database initialization (for contract/smoke tests)
    /// Per decision-record 2025-10-28-contract-test-architecture.md
    /// </summary>
    protected virtual bool InitializeDatabase => true;

    public async Task InitializeAsync()
    {
        Factory = new SleepApiWebApplicationFactory();
        Client = Factory.CreateClient();
        
        // Initialize database and container only if requested
        if (InitializeDatabase)
        {
            await InitializeDatabaseAsync();
        }
    }

    private async Task InitializeDatabaseAsync()
    {
        // CRITICAL: Never register null service instances per common-resolutions.md
        // Get Cosmos client from the factory's services
        var cosmosClient = Factory.Services.GetRequiredService<CosmosClient>();
        
        // Create database if it doesn't exist
        var databaseResponse = await cosmosClient.CreateDatabaseIfNotExistsAsync("biotrackr-test");
        var database = databaseResponse.Database;
        
        // Create container if it doesn't exist
        var containerResponse = await database.CreateContainerIfNotExistsAsync(
            new ContainerProperties
            {
                Id = "sleep-test",
                PartitionKeyPath = "/documentType"
            },
            throughput: 400);
        
        Container = containerResponse.Container;
    }

    public async Task DisposeAsync()
    {
        Client?.Dispose();
        await Factory.DisposeAsync();
    }
}

/// <summary>
/// Collection definition for sharing fixture across test classes
/// Per decision-record 2025-10-28-integration-test-project-structure.md
/// </summary>
[CollectionDefinition(nameof(IntegrationTestCollection))]
public class IntegrationTestCollection : ICollectionFixture<IntegrationTestFixture>
{
}
