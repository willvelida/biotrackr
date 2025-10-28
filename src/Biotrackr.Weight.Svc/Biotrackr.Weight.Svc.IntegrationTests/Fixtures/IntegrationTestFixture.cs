using Azure.Security.KeyVault.Secrets;
using Biotrackr.Weight.Svc.Configuration;
using Biotrackr.Weight.Svc.IntegrationTests.Helpers;
using Biotrackr.Weight.Svc.Repositories;
using Biotrackr.Weight.Svc.Repositories.Interfaces;
using Biotrackr.Weight.Svc.Services;
using Biotrackr.Weight.Svc.Services.Interfaces;
using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Xunit;

namespace Biotrackr.Weight.Svc.IntegrationTests.Fixtures
{
    /// <summary>
    /// Fixture for E2E integration tests that provides full test infrastructure
    /// including Cosmos DB Emulator connection and mocked HTTP/Key Vault dependencies.
    /// </summary>
    public class IntegrationTestFixture : IAsyncLifetime
    {
        private const string EmulatorEndpoint = "https://localhost:8081";
        private const string EmulatorKey = "C2y6yDjf5/R+ob0N8A7Cgv30VRDJIWEHLM+4QDU5DE2nQ9nDuVTqobD4b8mGGyPMbIZnqyMsEcaGQy67XIw/Jw==";
        
        public CosmosClient CosmosClient { get; private set; } = null!;
        public Database Database { get; private set; } = null!;
        public Container Container { get; private set; } = null!;
        public MockHttpMessageHandler MockHttpMessageHandler { get; }
        public IServiceProvider ServiceProvider { get; private set; } = null!;
        public IConfiguration Configuration { get; }

        private string _databaseName = null!;
        private string _containerName = null!;

        public IntegrationTestFixture()
        {
            // Initialize mocks
            MockHttpMessageHandler = new MockHttpMessageHandler();

            // Build test configuration
            var configBuilder = new ConfigurationBuilder()
                .AddJsonFile("appsettings.Test.json", optional: false)
                .AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["cosmosdbendpoint"] = EmulatorEndpoint,
                    ["keyvaulturl"] = "https://test-keyvault.vault.azure.net/",
                    ["managedidentityclientid"] = "test-client-id"
                });

            Configuration = configBuilder.Build();
        }

        /// <summary>
        /// Initializes test infrastructure - creates Cosmos DB database and container.
        /// </summary>
        public async Task InitializeAsync()
        {
            try
            {
                // Generate unique database name for this test run
                _databaseName = $"biotrackr-weight-test-{Guid.NewGuid():N}";
                _containerName = "weights";

                // Create Cosmos client options
                var cosmosClientOptions = new CosmosClientOptions
                {
                    SerializerOptions = new CosmosSerializationOptions
                    {
                        PropertyNamingPolicy = CosmosPropertyNamingPolicy.CamelCase
                    },
                    // For emulator, disable SSL validation
                    HttpClientFactory = () =>
                    {
                        var httpMessageHandler = new HttpClientHandler
                        {
                            ServerCertificateCustomValidationCallback = HttpClientHandler.DangerousAcceptAnyServerCertificateValidator
                        };
                        return new HttpClient(httpMessageHandler);
                    },
                    ConnectionMode = ConnectionMode.Gateway
                };

                // Initialize Cosmos client
                CosmosClient = new CosmosClient(EmulatorEndpoint, EmulatorKey, cosmosClientOptions);

                // Create test database
                var databaseResponse = await CosmosClient.CreateDatabaseIfNotExistsAsync(_databaseName);
                Database = databaseResponse.Database;

                // Create test container with partition key
                var containerResponse = await Database.CreateContainerIfNotExistsAsync(
                    _containerName,
                    "/documentType");
                Container = containerResponse.Container;

                // Build service provider with test dependencies
                BuildServiceProvider();
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException(
                    "Failed to initialize Cosmos DB Emulator connection. Ensure the emulator is running at https://localhost:8081. " +
                    "Run: docker run -d -p 8081:8081 mcr.microsoft.com/cosmosdb/linux/azure-cosmos-emulator:latest", ex);
            }
        }

        /// <summary>
        /// Cleans up test infrastructure - deletes test database.
        /// </summary>
        public async Task DisposeAsync()
        {
            if (Database != null)
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

        private void BuildServiceProvider()
        {
            var services = new ServiceCollection();

            // Add configuration
            services.AddSingleton(Configuration);

            // Add Options pattern for Settings
            services.AddOptions<Settings>().Configure<IConfiguration>((settings, configuration) =>
            {
                configuration.GetSection("Biotrackr").Bind(settings);
            });

            // Override settings with test database name
            services.Configure<Settings>(options =>
            {
                options.DatabaseName = _databaseName;
                options.ContainerName = _containerName;
            });

            // Add real Cosmos client
            services.AddSingleton(CosmosClient);

            // Create a fake SecretClient for testing - we don't use it because HTTP is mocked
            // In real tests, HTTP responses will be mocked so SecretClient won't be called
            var fakeSecretClient = (SecretClient?)null!;
            services.AddSingleton(fakeSecretClient);

            // Register services
            services.AddScoped<ICosmosRepository, CosmosRepository>();
            services.AddScoped<IFitbitService, FitbitService>();
            services.AddScoped<IWeightService, WeightService>();

            // Add HttpClient with mocked handler
            services.AddHttpClient<IFitbitService, FitbitService>()
                .ConfigurePrimaryHttpMessageHandler(() => MockHttpMessageHandler);

            ServiceProvider = services.BuildServiceProvider();
        }
    }
}
