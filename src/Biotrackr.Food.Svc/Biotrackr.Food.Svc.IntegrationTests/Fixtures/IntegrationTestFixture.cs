namespace Biotrackr.Food.Svc.IntegrationTests.Fixtures;

/// <summary>
/// Base test fixture for E2E integration tests requiring Cosmos DB Emulator.
/// Provides database initialization, container setup, and cleanup functionality.
/// </summary>
public class IntegrationTestFixture : IAsyncLifetime
{
    protected virtual bool InitializeDatabase => true;

    public CosmosClient? CosmosClient { get; private set; }
    public Database? Database { get; private set; }
    public Container? Container { get; private set; }
    public IConfiguration? Configuration { get; protected set; }

    public virtual async Task InitializeAsync()
    {
        if (InitializeDatabase)
        {
            // Load configuration from appsettings.Test.json
            Configuration = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.Test.json", optional: false)
                .Build();

            var cosmosDbEndpoint = Configuration["cosmosdbendpoint"] ?? throw new InvalidOperationException("cosmosdbendpoint not configured");
            var cosmosDbAccountKey = Configuration["CosmosDbAccountKey"] ?? throw new InvalidOperationException("CosmosDbAccountKey not configured");
            var databaseId = Configuration["Biotrackr:CosmosDbDatabaseId"] ?? "biotrackr-test";
            var containerId = Configuration["Biotrackr:CosmosDbContainerId"] ?? "food-test";

            // Create Cosmos DB client with Gateway mode (required for Emulator)
            CosmosClient = new CosmosClient(
                cosmosDbEndpoint,
                cosmosDbAccountKey,
                new CosmosClientOptions
                {
                    ConnectionMode = ConnectionMode.Gateway, // Required for Cosmos DB Emulator
                    SerializerOptions = new CosmosSerializationOptions
                    {
                        PropertyNamingPolicy = CosmosPropertyNamingPolicy.CamelCase
                    },
                    HttpClientFactory = () => new HttpClient(new HttpClientHandler
                    {
                        ServerCertificateCustomValidationCallback = (_, _, _, _) => true
                    })
                });

            // Create test database and container
            Database = await CosmosClient.CreateDatabaseIfNotExistsAsync(databaseId);
            Container = await Database.CreateContainerIfNotExistsAsync(
                containerId,
                "/documentType");
        }
    }

    /// <summary>
    /// Clears all documents from the test container to ensure test isolation.
    /// Uses dynamic type for flexible document deletion across different document types.
    /// </summary>
    public async Task ClearContainerAsync()
    {
        if (Container == null)
        {
            throw new InvalidOperationException("Container not initialized");
        }

        var query = new QueryDefinition("SELECT c.id, c.documentType FROM c");
        var iterator = Container.GetItemQueryIterator<dynamic>(query);

        while (iterator.HasMoreResults)
        {
            var response = await iterator.ReadNextAsync();
            foreach (var item in response)
            {
                await Container.DeleteItemAsync<dynamic>(
                    item.id.ToString(),
                    new PartitionKey(item.documentType.ToString()));
            }
        }
    }

    public virtual async Task DisposeAsync()
    {
        if (InitializeDatabase && Database != null)
        {
            try
            {
                await Database.DeleteAsync();
            }
            catch (Exception)
            {
                // Ignore cleanup errors
            }
        }

        CosmosClient?.Dispose();
    }
}
