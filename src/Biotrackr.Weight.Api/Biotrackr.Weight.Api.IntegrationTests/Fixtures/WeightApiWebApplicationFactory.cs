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
        // Set environment variables BEFORE host builds (so Program.cs can read them)
        Environment.SetEnvironmentVariable("cosmosdbendpoint", CosmosDbEndpoint);
        Environment.SetEnvironmentVariable("Biotrackr:CosmosDb:AccountKey", CosmosDbAccountKey);
        Environment.SetEnvironmentVariable("Biotrackr:DatabaseName", "biotrackr-test");
        Environment.SetEnvironmentVariable("Biotrackr:ContainerName", "weight-test");
        Environment.SetEnvironmentVariable("azureappconfigendpoint", string.Empty);
        Environment.SetEnvironmentVariable("managedidentityclientid", string.Empty);
        
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
