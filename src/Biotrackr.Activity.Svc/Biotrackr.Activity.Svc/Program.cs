using System.Diagnostics.CodeAnalysis;
using Azure.Identity;
using Azure.Monitor.OpenTelemetry.Exporter;
using Azure.Security.KeyVault.Secrets;
using Biotrackr.Activity.Svc.Configuration;
using Biotrackr.Activity.Svc.Repositories;
using Biotrackr.Activity.Svc.Repositories.Interfaces;
using Biotrackr.Activity.Svc.Services;
using Biotrackr.Activity.Svc.Services.Interfaces;
using Biotrackr.Activity.Svc.Workers;
using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.Configuration.AzureAppConfiguration;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

[ExcludeFromCodeCoverage]
internal class Program
{
    private static void Main(string[] args)
    {
        var resourceAttributes = new Dictionary<string, object>
        {
            { "service.name", "Biotrackr.Activity.Svc" },
            { "service.version", "1.0.0" }
        };

        var resourceBuilder = ResourceBuilder.CreateDefault().AddAttributes(resourceAttributes);

        IHost host = Host.CreateDefaultBuilder(args)
    .ConfigureAppConfiguration(config =>
    {
        config.AddEnvironmentVariables();
        config.AddAzureAppConfiguration(options =>
        {
            options.Connect(
                new Uri(Environment.GetEnvironmentVariable("azureappconfigendpoint")),
                new ManagedIdentityCredential(Environment.GetEnvironmentVariable("managedidentityclientid")))
            .Select(keyFilter: KeyFilter.Any, LabelFilter.Null);
        });
    })
    .ConfigureServices((context, services) =>
    {
        var keyVaultUrl = context.Configuration["keyvaulturl"];
        var managedIdentityClient = context.Configuration["managedidentityclientid"];
        var defaultCredentialOptions = new DefaultAzureCredentialOptions()
        {
            ManagedIdentityClientId = managedIdentityClient
        };

        services.AddOptions<Settings>().Configure<IConfiguration>((settings, configuration) =>
        {
            configuration.GetSection("Biotrackr").Bind(settings);
        });

        var cosmosDbEndpoint = context.Configuration["cosmosdbendpoint"];
        var cosmosClientOptions = new CosmosClientOptions()
        {
            SerializerOptions = new CosmosSerializationOptions()
            {
                PropertyNamingPolicy = CosmosPropertyNamingPolicy.CamelCase
            }
        };

        services.AddSingleton(new SecretClient(new Uri(keyVaultUrl), new DefaultAzureCredential(defaultCredentialOptions)));
        services.AddSingleton(new CosmosClient(cosmosDbEndpoint, new DefaultAzureCredential(defaultCredentialOptions), cosmosClientOptions));

        services.AddScoped<ICosmosRepository, CosmosRepository>();

        services.AddScoped<IFitbitService, FitbitService>();
        services.AddScoped<IActivityService, ActivityService>();

        services.AddHttpClient<IFitbitService, FitbitService>()
            .AddStandardResilienceHandler();

        services.AddHostedService<ActivityWorker>();

        services.AddOpenTelemetry()
            .WithTracing(tracing =>
            {
                tracing.SetResourceBuilder(resourceBuilder)
                        .AddAzureMonitorTraceExporter(options =>
                        {
                            options.ConnectionString = context.Configuration["applicationinsightsconnectionstring"];
                        });
            })
            .WithMetrics(metrics =>
            {
                metrics.SetResourceBuilder(resourceBuilder)
                        .AddAzureMonitorMetricExporter(options =>
                        {
                            options.ConnectionString = context.Configuration["applicationinsightsconnectionstring"];
                        });
            });
    })
    .ConfigureLogging((context, logging) =>
    {
        logging.AddOpenTelemetry(log =>
        {
            log.SetResourceBuilder(resourceBuilder);
            log.AddAzureMonitorLogExporter(options =>
            {
                options.ConnectionString = context.Configuration["applicationinsightsconnectionstring"];
            });

        });
    })
    .Build();

        host.Run();
    }
}

