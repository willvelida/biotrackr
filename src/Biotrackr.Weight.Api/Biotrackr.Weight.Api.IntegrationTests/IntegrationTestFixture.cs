using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Biotrackr.Weight.Api.IntegrationTests;

/// <summary>
/// Base fixture for integration tests
/// Provides shared WebApplicationFactory instance and Cosmos DB setup across tests
/// </summary>
public class IntegrationTestFixture : IAsyncLifetime
{
    public WeightApiWebApplicationFactory Factory { get; private set; } = null!;
    public HttpClient Client { get; private set; } = null!;

    public async Task InitializeAsync()
    {
        Factory = new WeightApiWebApplicationFactory();
        Client = Factory.CreateClient();
        
        // Initialize database and container
        await InitializeDatabaseAsync();
    }

    private async Task InitializeDatabaseAsync()
    {
        // Get Cosmos client from the factory's services
        var cosmosClient = Factory.Services.GetRequiredService<CosmosClient>();
        
        // Create database if it doesn't exist
        var databaseResponse = await cosmosClient.CreateDatabaseIfNotExistsAsync("biotrackr-test");
        var database = databaseResponse.Database;
        
        // Create container if it doesn't exist
        await database.CreateContainerIfNotExistsAsync(
            new ContainerProperties
            {
                Id = "weight-test",
                PartitionKeyPath = "/documentType"
            },
            throughput: 400);
    }

    public async Task DisposeAsync()
    {
        Client?.Dispose();
        await Factory.DisposeAsync();
    }
}

/// <summary>
/// Collection definition for sharing fixture across test classes
/// </summary>
[CollectionDefinition(nameof(IntegrationTestCollection))]
public class IntegrationTestCollection : ICollectionFixture<IntegrationTestFixture>
{
}
