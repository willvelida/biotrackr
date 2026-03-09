using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.DependencyInjection;

namespace Biotrackr.Chat.Api.IntegrationTests.Fixtures;

/// <summary>
/// Custom WebApplicationFactory for integration testing.
/// Overrides Cosmos DB with local emulator and sets empty config values
/// so Program.cs skips Azure App Configuration.
/// </summary>
public class ChatApiWebApplicationFactory : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        // Set environment variables before host builds
        Environment.SetEnvironmentVariable("cosmosdbendpoint", "https://localhost:8081");
        Environment.SetEnvironmentVariable("Biotrackr:DatabaseName", "biotrackr-test");
        Environment.SetEnvironmentVariable("Biotrackr:ConversationsContainerName", "conversations-test");
        Environment.SetEnvironmentVariable("Biotrackr:ApiBaseUrl", "https://localhost:9999");
        Environment.SetEnvironmentVariable("Biotrackr:AnthropicApiKey", "test-key");
        Environment.SetEnvironmentVariable("Biotrackr:ChatAgentModel", "claude-haiku-4-5");
        Environment.SetEnvironmentVariable("Biotrackr:ChatSystemPrompt", "You are a test assistant.");
        Environment.SetEnvironmentVariable("azureappconfigendpoint", string.Empty);
        Environment.SetEnvironmentVariable("managedidentityclientid", string.Empty);

        builder.UseEnvironment("Test");

        builder.ConfigureServices(services =>
        {
            // Remove existing Cosmos Client registration
            var cosmosDescriptor = services.SingleOrDefault(d => d.ServiceType == typeof(CosmosClient));
            if (cosmosDescriptor != null)
            {
                services.Remove(cosmosDescriptor);
            }

            // Register Cosmos Client with local emulator
            services.AddSingleton<CosmosClient>(sp =>
            {
                return new CosmosClient(
                    "https://localhost:8081",
                    "C2y6yDjf5/R+ob0N8A7Cgv30VRDJIWEHLM+4QDU5DE2nQ9nDuVTqobD4b8mGGyPMbIZnqyMsEcaGQy67XIw/Jw==",
                    new CosmosClientOptions
                    {
                        ConnectionMode = ConnectionMode.Gateway,
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
