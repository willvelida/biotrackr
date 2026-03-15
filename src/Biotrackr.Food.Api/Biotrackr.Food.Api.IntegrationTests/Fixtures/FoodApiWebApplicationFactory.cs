using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.DependencyInjection;

namespace Biotrackr.Food.Api.IntegrationTests.Fixtures;

/// <summary>
/// Custom WebApplicationFactory for integration testing with local Cosmos DB Emulator
/// Configures test-specific settings and overrides production services
/// </summary>
public class FoodApiWebApplicationFactory : WebApplicationFactory<Program>
{
    // Constants for local Cosmos DB Emulator
    private const string CosmosDbEndpoint = "https://localhost:8081";
    private const string CosmosDbAccountKey = "C2y6yDjf5/R+ob0N8A7Cgv30VRDJIWEHLM+4QDU5DE2nQ9nDuVTqobD4b8mGGyPMbIZnqyMsEcaGQy67XIw/Jw==";

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        // Set environment variables BEFORE host builds (so Program.cs can read them)
        // Using colon-separated format per decision-record 2025-10-28-dotnet-configuration-format.md
        Environment.SetEnvironmentVariable("cosmosdbendpoint", CosmosDbEndpoint);
        Environment.SetEnvironmentVariable("Biotrackr:CosmosDb:AccountKey", CosmosDbAccountKey);
        Environment.SetEnvironmentVariable("Biotrackr:DatabaseName", "biotrackr-test");
        Environment.SetEnvironmentVariable("Biotrackr:ContainerName", "food-test");
        Environment.SetEnvironmentVariable("azureappconfigendpoint", string.Empty);
        Environment.SetEnvironmentVariable("managedidentityclientid", string.Empty);
        Environment.SetEnvironmentVariable("applicationinsightsconnectionstring", "InstrumentationKey=00000000-0000-0000-0000-000000000000");
        
        // Set environment to Test
        builder.UseEnvironment("Test");
        
        builder.ConfigureServices(services =>
        {
            // Remove existing Cosmos Client registration if it exists
            var cosmosDescriptor = services.SingleOrDefault(d => d.ServiceType == typeof(CosmosClient));
            if (cosmosDescriptor != null)
            {
                services.Remove(cosmosDescriptor);
            }

            // Register Cosmos Client with local emulator connection
            // Per common-resolutions.md: Use Gateway mode to avoid TCP+SSL issues with local emulator
            services.AddSingleton<CosmosClient>(sp =>
            {
                return new CosmosClient(CosmosDbEndpoint, CosmosDbAccountKey, new CosmosClientOptions
                {
                    ConnectionMode = ConnectionMode.Gateway, // Force Gateway mode (HTTPS only) to avoid TCP+SSL issues
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
