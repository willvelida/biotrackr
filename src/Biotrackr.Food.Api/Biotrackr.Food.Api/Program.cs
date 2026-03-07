using Azure.Identity;
using Biotrackr.Food.Api.Extensions;
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

// Add Cosmos DB services
builder.Services.AddCosmosDb(builder.Configuration);

builder.Services.AddOpenApi();

// Add health checks
builder.Services.AddHealthChecks();

var app = builder.Build();

app.MapOpenApi();

// Map endpoints
app.RegisterFoodEndpoints();
app.RegisterHealthCheckEndpoints();

app.Run();
