using Azure.Identity;
using Biotrackr.Food.Api.Configuration;
using Biotrackr.Food.Api.Repositories;
using Biotrackr.Food.Api.Repositories.Interfaces;
using Microsoft.Azure.Cosmos;

namespace Biotrackr.Food.Api.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddCosmosDb(this IServiceCollection services, IConfiguration configuration)
    {
        // Configure Settings
        services.Configure<Settings>(configuration.GetSection("Biotrackr"));

        // Get Cosmos DB configuration
        var cosmosDbEndpoint = configuration.GetValue<string>("cosmosdbendpoint") 
            ?? configuration.GetValue<string>("CosmosDb:Endpoint");
        var cosmosDbAccountKey = configuration.GetValue<string>("Biotrackr:CosmosDb:AccountKey");
        var managedIdentityClientId = configuration.GetValue<string>("managedidentityclientid");

        // Configure Cosmos Client options
        var cosmosClientOptions = new CosmosClientOptions
        {
            SerializerOptions = new CosmosSerializationOptions
            {
                PropertyNamingPolicy = CosmosPropertyNamingPolicy.CamelCase
            }
        };

        // Create CosmosClient (Singleton)
        CosmosClient cosmosClient;
        if (!string.IsNullOrWhiteSpace(cosmosDbAccountKey))
        {
            // Use account key for local/test environments
            cosmosClient = new CosmosClient(cosmosDbEndpoint, cosmosDbAccountKey, cosmosClientOptions);
        }
        else
        {
            // Use Managed Identity for production
            var defaultCredentialOptions = new DefaultAzureCredentialOptions
            {
                ManagedIdentityClientId = managedIdentityClientId
            };
            cosmosClient = new CosmosClient(
                cosmosDbEndpoint,
                new DefaultAzureCredential(defaultCredentialOptions),
                cosmosClientOptions);
        }

        services.AddSingleton(cosmosClient);

        // Register repository as Scoped
        services.AddScoped<ICosmosRepository, CosmosRepository>();

        return services;
    }
}
