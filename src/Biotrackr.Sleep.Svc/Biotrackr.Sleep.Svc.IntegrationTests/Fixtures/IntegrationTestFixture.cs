using Biotrackr.Sleep.Svc.Configuration;
using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.Configuration;

namespace Biotrackr.Sleep.Svc.IntegrationTests.Fixtures
{
    public class IntegrationTestFixture : IAsyncLifetime
    {
        public CosmosClient CosmosClient { get; private set; } = null!;
        public Container Container { get; private set; } = null!;

        private const string DatabaseName = "BiotrackrTestDb";
        private const string ContainerName = "SleepTestContainer";
        private const string CosmosDbEndpoint = "https://localhost:8081";
        private const string CosmosDbAccountKey = "C2y6yDjf5/R+ob0N8A7Cgv30VRDJIWEHLM+4QDU5DE2nQ9nDuVTqobD4b8mGGyPMbIZnqyMsEcaGQy67XIw/Jw==";

        public async Task InitializeAsync()
        {
            // Create CosmosClient with Gateway mode for Emulator compatibility
            CosmosClient = new CosmosClient(
                CosmosDbEndpoint,
                CosmosDbAccountKey,
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
            var databaseResponse = await CosmosClient.CreateDatabaseIfNotExistsAsync(DatabaseName);
            var database = databaseResponse.Database;

            // Create container with partition key
            var containerResponse = await database.CreateContainerIfNotExistsAsync(
                new ContainerProperties
                {
                    Id = ContainerName,
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
                    var database = CosmosClient.GetDatabase(DatabaseName);
                    await database.DeleteAsync();
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
