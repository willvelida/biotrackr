using Azure.Identity;
using Biotrackr.Weight.Api.Configuration;
using Biotrackr.Weight.Api.Extensions;
using Biotrackr.Weight.Api.Repositories;
using Biotrackr.Weight.Api.Repositories.Interfaces;
using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.Configuration.AzureAppConfiguration;

var builder = WebApplication.CreateBuilder(args);

builder.Configuration.AddEnvironmentVariables();
var managedIdentityClientId = builder.Configuration.GetValue<string>("managedidentityclientid");
var azureAppConfigEndpoint = builder.Configuration.GetValue<string>("azureappconfigendpoint");

// Only load Azure App Configuration if endpoint is provided (not in test environment)
if (!string.IsNullOrWhiteSpace(azureAppConfigEndpoint))
{
    builder.Configuration.AddAzureAppConfiguration(config =>
    {
        config.Connect(new Uri(azureAppConfigEndpoint),
            new ManagedIdentityCredential(managedIdentityClientId))
        .Select(KeyFilter.Any, LabelFilter.Null);
    });
}

builder.Services.Configure<Settings>(builder.Configuration.GetSection("Biotrackr"));
var cosmosClientOptions = new CosmosClientOptions
{
    SerializerOptions = new CosmosSerializationOptions
    {
        PropertyNamingPolicy = CosmosPropertyNamingPolicy.CamelCase
    }
};

var cosmosDbEndpoint = builder.Configuration.GetValue<string>("cosmosdbendpoint");
var cosmosDbAccountKey = builder.Configuration.GetValue<string>("Biotrackr:CosmosDb:AccountKey");

CosmosClient cosmosClient;
if (!string.IsNullOrWhiteSpace(cosmosDbAccountKey))
{
    // Use account key for local/test environments
    cosmosClient = new CosmosClient(cosmosDbEndpoint, cosmosDbAccountKey, cosmosClientOptions);
}
else
{
    // Use Managed Identity for production
    var defaultCredentialOptions = new DefaultAzureCredentialOptions()
    {
        ManagedIdentityClientId = managedIdentityClientId
    };
    cosmosClient = new CosmosClient(cosmosDbEndpoint,
        new DefaultAzureCredential(defaultCredentialOptions),
        cosmosClientOptions);
}

builder.Services.AddSingleton(cosmosClient);
builder.Services.AddTransient<ICosmosRepository, CosmosRepository>();

builder.Services.AddOpenApi();

builder.Services.AddHealthChecks();

var app = builder.Build();

app.MapOpenApi();

app.RegisterWeightEndpoints();
app.RegisterHealthCheckEndpoints();

app.Run();