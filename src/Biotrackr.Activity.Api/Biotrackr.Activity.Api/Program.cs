using Azure.Identity;
using Azure.Monitor.OpenTelemetry.Exporter;
using Biotrackr.Activity.Api.Configuration;
using Biotrackr.Activity.Api.Extensions;
using Biotrackr.Activity.Api.Repositories;
using Biotrackr.Activity.Api.Repositories.Interfaces;
using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.Configuration.AzureAppConfiguration;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

var resourceAttributes = new Dictionary<string, object>
{
    { "service.name", "Biotrackr.Activity.Api" },
    { "service.version", "1.0.0" }
};
var resourceBuilder = ResourceBuilder.CreateDefault().AddAttributes(resourceAttributes);

var builder = WebApplication.CreateBuilder(args);

builder.Configuration.AddEnvironmentVariables();
var managedIdentityClientId = builder.Configuration.GetValue<string>("managedidentityclientid");
var azureAppConfigEndpoint = builder.Configuration.GetValue<string>("azureappconfigendpoint");

// Only load Azure App Configuration if endpoint is provided (not in test environment)
if (!string.IsNullOrWhiteSpace(azureAppConfigEndpoint))
{
    var credential = new ManagedIdentityCredential(managedIdentityClientId);
    builder.Configuration.AddAzureAppConfiguration(config =>
    {
        config.Connect(new Uri(azureAppConfigEndpoint), credential)
        .Select(KeyFilter.Any, LabelFilter.Null)
        .ConfigureKeyVault(kv =>
        {
            kv.SetCredential(credential);
        });
    });
}

var defaultCredentialOptions = new DefaultAzureCredentialOptions()
{
    ManagedIdentityClientId = managedIdentityClientId
};

builder.Services.Configure<Settings>(builder.Configuration.GetSection("Biotrackr"));

var cosmosClientOptions = new CosmosClientOptions
{
    SerializerOptions = new CosmosSerializationOptions
    {
        PropertyNamingPolicy = CosmosPropertyNamingPolicy.CamelCase
    }
};
var cosmosClient = new CosmosClient(
    builder.Configuration.GetValue<string>("cosmosdbendpoint"),
    new DefaultAzureCredential(defaultCredentialOptions),
    cosmosClientOptions);
builder.Services.AddSingleton(cosmosClient);
builder.Services.AddScoped<ICosmosRepository, CosmosRepository>();

builder.Services.AddOpenApi();

builder.Services.AddHealthChecks();

var appInsightsConnectionString = builder.Configuration["applicationinsightsconnectionstring"];

builder.Services.AddOpenTelemetry()
    .WithTracing(tracing =>
    {
        tracing.SetResourceBuilder(resourceBuilder)
            .AddAspNetCoreInstrumentation()
            .AddHttpClientInstrumentation()
            .AddAzureMonitorTraceExporter(options =>
            {
                options.ConnectionString = appInsightsConnectionString;
            });
    })
    .WithMetrics(metrics =>
    {
        metrics.SetResourceBuilder(resourceBuilder)
            .AddAspNetCoreInstrumentation()
            .AddHttpClientInstrumentation()
            .AddAzureMonitorMetricExporter(options =>
            {
                options.ConnectionString = appInsightsConnectionString;
            });
    });

builder.Logging.AddOpenTelemetry(log =>
{
    log.SetResourceBuilder(resourceBuilder);
    log.AddAzureMonitorLogExporter(options =>
    {
        options.ConnectionString = appInsightsConnectionString;
    });
});

var app = builder.Build();

app.MapOpenApi();

app.RegisterActivityEndpoints();
app.RegisterHealthCheckEndpoints();

app.Run();
