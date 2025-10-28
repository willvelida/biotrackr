using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Biotrackr.Weight.Api.IntegrationTests;

/// <summary>
/// Custom WebApplicationFactory for integration testing with local Cosmos DB Emulator
/// Configures test-specific settings and overrides production services
/// </summary>
public class WeightApiWebApplicationFactory : WebApplicationFactory<Program>
{
    // Constants for local Cosmos DB Emulator
    private const string CosmosDbEndpoint = "https://localhost:8081";
    private const string CosmosDbAccountKey = "C2y6yDjf5/R+ob0N8A7Cgv30VRDJIWEHLM+4QDU5DE2nQ9nDuVTqobD4b8mGGyPMbIZnqyMsEcaGQy67XIw/Jw==";

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        // Set environment to Test first
        builder.UseEnvironment("Test");
        
        builder.ConfigureAppConfiguration((context, config) =>
        {
            // Add test-specific in-memory configuration - will override other sources
            config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                // Prevent Azure App Configuration loading
                ["azureappconfigendpoint"] = string.Empty,
                ["managedidentityclientid"] = string.Empty,
                
                // Cosmos DB configuration for tests (using local emulator)
                ["cosmosdbendpoint"] = CosmosDbEndpoint,
                ["Biotrackr:CosmosDb:AccountKey"] = CosmosDbAccountKey,
                ["Biotrackr:DatabaseName"] = "biotrackr-test",
                ["Biotrackr:ContainerName"] = "weight-test"
            });
        });

        builder.ConfigureServices(services =>
        {
            // Remove existing Cosmos Client registration if it exists
            var cosmosDescriptor = services.SingleOrDefault(d => d.ServiceType == typeof(CosmosClient));
            if (cosmosDescriptor != null)
            {
                services.Remove(cosmosDescriptor);
            }

            // Register Cosmos Client with local emulator connection
            services.AddSingleton<CosmosClient>(sp =>
            {
                return new CosmosClient(CosmosDbEndpoint, CosmosDbAccountKey, new CosmosClientOptions
                {
                    SerializerOptions = new CosmosSerializationOptions
                    {
                        PropertyNamingPolicy = CosmosPropertyNamingPolicy.CamelCase
                    },
                    HttpClientFactory = () => new HttpClient(new HttpClientHandler
                    {
                        ServerCertificateCustomValidationCallback = (_, _, _, _) => true
                    })
                });
            });
        });
    }
}
