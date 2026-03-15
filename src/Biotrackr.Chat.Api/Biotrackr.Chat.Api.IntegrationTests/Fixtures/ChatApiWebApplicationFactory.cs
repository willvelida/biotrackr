using Biotrackr.Chat.Api.Services;
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
        Environment.SetEnvironmentVariable("Biotrackr:CosmosEndpoint", "https://localhost:8081");
        Environment.SetEnvironmentVariable("Biotrackr:DatabaseName", "biotrackr-test");
        Environment.SetEnvironmentVariable("Biotrackr:ConversationsContainerName", "conversations-test");
        Environment.SetEnvironmentVariable("Biotrackr:AgentIdentityId", "00000000-0000-0000-0000-000000000000");
        Environment.SetEnvironmentVariable("Biotrackr:ApiBaseUrl", "https://localhost:9999");
        Environment.SetEnvironmentVariable("Biotrackr:ApiSubscriptionKey", "test-subscription-key");
        Environment.SetEnvironmentVariable("Biotrackr:AnthropicApiKey", "test-key");
        Environment.SetEnvironmentVariable("Biotrackr:ChatAgentModel", "claude-haiku-4-5");
        Environment.SetEnvironmentVariable("Biotrackr:ChatSystemPrompt", "You are a test assistant.");
        Environment.SetEnvironmentVariable("azureappconfigendpoint", string.Empty);
        Environment.SetEnvironmentVariable("managedidentityclientid", string.Empty);
        Environment.SetEnvironmentVariable("applicationinsightsconnectionstring", "InstrumentationKey=00000000-0000-0000-0000-000000000000");
        Environment.SetEnvironmentVariable("AzureAd:TenantId", "test-tenant");
        Environment.SetEnvironmentVariable("AzureAd:ClientId", "test-client");

        builder.UseEnvironment("Test");

        builder.ConfigureServices(services =>
        {
            // Remove existing ICosmosClientFactory registration
            var factoryDescriptor = services.SingleOrDefault(d => d.ServiceType == typeof(ICosmosClientFactory));
            if (factoryDescriptor != null)
            {
                services.Remove(factoryDescriptor);
            }

            // Register a test factory that returns a Cosmos Client connected to the local emulator
            services.AddScoped<ICosmosClientFactory, EmulatorCosmosClientFactory>();
        });
    }

    /// <summary>
    /// Test factory that creates a CosmosClient connected to the local Cosmos DB emulator
    /// using key-based authentication (no agent identity needed for tests).
    /// </summary>
    private class EmulatorCosmosClientFactory : ICosmosClientFactory
    {
        public CosmosClient Create()
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
        }
    }
}
