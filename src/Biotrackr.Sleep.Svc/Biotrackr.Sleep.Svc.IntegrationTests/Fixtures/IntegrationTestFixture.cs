using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.Configuration;

namespace Biotrackr.Sleep.Svc.IntegrationTests.Fixtures
{
    public class IntegrationTestFixture : IAsyncLifetime
    {
        public CosmosClient CosmosClient { get; private set; } = null!;
        public Container Container { get; private set; } = null!;
        public Database Database { get; private set; } = null!;
        public IConfiguration Configuration { get; private set; } = null!;

        public async Task InitializeAsync()
        {
            // Load configuration from appsettings.Test.json
            Configuration = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.Test.json", optional: false)
                .Build();

            var cosmosDbEndpoint = Configuration["cosmosdbendpoint"] ?? throw new InvalidOperationException("cosmosdbendpoint not configured");
            var cosmosDbAccountKey = Configuration["cosmosdbaccountkey"] ?? throw new InvalidOperationException("cosmosdbaccountkey not configured");
            var databaseId = Configuration["databaseId"] ?? "BiotrackrTestDb";
            var containerId = Configuration["containerId"] ?? "SleepTestContainer";

            // Create CosmosClient with Gateway mode for Emulator compatibility
            CosmosClient = new CosmosClient(
                cosmosDbEndpoint,
                cosmosDbAccountKey,
                new CosmosClientOptions
                {
                    ConnectionMode = ConnectionMode.Gateway, // CRITICAL: Gateway mode for Cosmos DB Emulator
                    SerializerOptions = new CosmosSerializationOptions
                    {
                        PropertyNamingPolicy = CosmosPropertyNamingPolicy.CamelCase
                    },
                    HttpClientFactory = () => new HttpClient(new HttpClientHandler
                    {
                        ServerCertificateCustomValidationCallback = (_, _, _, _) => true
                    })
                });

            // Create database
            var databaseResponse = await CosmosClient.CreateDatabaseIfNotExistsAsync(databaseId);
            Database = databaseResponse.Database;

            // Create container with partition key
            var containerResponse = await Database.CreateContainerIfNotExistsAsync(
                new ContainerProperties
                {
                    Id = containerId,
                    PartitionKeyPath = "/documentType"
                });

            Container = containerResponse.Container;
        }

        public async Task DisposeAsync()
        {
            if (CosmosClient != null)
            {
                try
                {
                    // Delete test database
                    await Database.DeleteAsync();
                }
                catch
                {
                    // Ignore cleanup errors
                }
                finally
                {
                    CosmosClient.Dispose();
                }
            }
        }
    }
}
