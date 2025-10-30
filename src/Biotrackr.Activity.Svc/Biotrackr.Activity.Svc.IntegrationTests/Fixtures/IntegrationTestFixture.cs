using Azure.Identity;
using Azure.Security.KeyVault.Secrets;
using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.Configuration;

namespace Biotrackr.Activity.Svc.IntegrationTests.Fixtures;

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
            var cosmosDbAccountKey = Configuration["cosmosdbaccountkey"] ?? throw new InvalidOperationException("cosmosdbaccountkey not configured");
            var databaseId = Configuration["databaseId"] ?? "BiotrackrTestDb";
            var containerId = Configuration["containerId"] ?? "ActivityTestContainer";

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

    public virtual async Task DisposeAsync()
    {
        if (InitializeDatabase && Database != null)
        {
            try
            {
                await Database.DeleteAsync();
            }
            catch
            {
                // Ignore cleanup errors
            }
        }

        CosmosClient?.Dispose();
    }
}
