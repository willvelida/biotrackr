using Azure.Identity;
using Azure.Security.KeyVault.Secrets;
using Biotrackr.Vitals.Svc.Configuration;
using Biotrackr.Vitals.Svc.Repositories;
using Biotrackr.Vitals.Svc.Repositories.Interfaces;
using Biotrackr.Vitals.Svc.Services;
using Biotrackr.Vitals.Svc.Services.Interfaces;
using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Moq;

namespace Biotrackr.Vitals.Svc.IntegrationTests.Fixtures
{
    /// <summary>
    /// Fixture for contract tests that verify dependency injection configuration.
    /// Does not connect to external dependencies - only verifies service registration.
    /// </summary>
    public class ContractTestFixture
    {
        public IServiceProvider ServiceProvider { get; }
        public IConfiguration Configuration { get; }

        public ContractTestFixture()
        {
            // Build test configuration
            var configBuilder = new ConfigurationBuilder()
                .AddJsonFile("appsettings.Test.json", optional: false)
                .AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["Biotrackr:DatabaseName"] = "test-db",
                    ["Biotrackr:ContainerName"] = "test-container",
                    ["cosmosdbendpoint"] = "https://localhost:8081",
                    ["keyvaulturl"] = "https://test-keyvault.vault.azure.net/",
                    ["managedidentityclientid"] = "test-client-id"
                });

            Configuration = configBuilder.Build();

            // Build minimal service collection
            var services = new ServiceCollection();

            // Add configuration
            services.AddSingleton(Configuration);

            // Add Options pattern for Settings
            services.AddOptions<Settings>().Configure<IConfiguration>((settings, configuration) =>
            {
                configuration.GetSection("Biotrackr").Bind(settings);
            });

            // Mock external dependencies
            var mockSecretClient = new Mock<SecretClient>();
            services.AddSingleton(mockSecretClient.Object);

            var cosmosClientOptions = new CosmosClientOptions()
            {
                SerializerOptions = new CosmosSerializationOptions()
                {
                    PropertyNamingPolicy = CosmosPropertyNamingPolicy.CamelCase
                }
            };

            // Use mock for CosmosClient in contract tests
            var mockCosmosClient = new Mock<CosmosClient>();
            services.AddSingleton(mockCosmosClient.Object);

            // Register services with their interfaces (matching Program.cs)
            services.AddScoped<ICosmosRepository, CosmosRepository>();
            services.AddScoped<IVitalsService, VitalsService>();

            services.AddHttpClient<IWithingsService, WithingsService>();

            ServiceProvider = services.BuildServiceProvider();
        }
    }
}
