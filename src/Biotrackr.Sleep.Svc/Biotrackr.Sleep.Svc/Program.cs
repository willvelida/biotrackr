// Test coverage implementation complete - triggering CI/CD workflow
using System.Diagnostics.CodeAnalysis;
using Azure.Identity;
using Azure.Monitor.OpenTelemetry.Exporter;
using Azure.Security.KeyVault.Secrets;
using Biotrackr.Sleep.Svc.Configuration;
using Biotrackr.Sleep.Svc.Repositories;
using Biotrackr.Sleep.Svc.Repositories.Interfaces;
using Biotrackr.Sleep.Svc.Services;
using Biotrackr.Sleep.Svc.Services.Interfaces;
using Biotrackr.Sleep.Svc.Worker;
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
            { "service.name", "Biotrackr.Sleep.Svc" },
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
            .Select(keyFilter: KeyFilter.Any, labelFilter: LabelFilter.Null);
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

        services.AddScoped<ISleepService, SleepService>();

        services.AddHttpClient<IFitbitService, FitbitService>()
            .AddStandardResilienceHandler();

        services.AddHostedService<SleepWorker>();

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