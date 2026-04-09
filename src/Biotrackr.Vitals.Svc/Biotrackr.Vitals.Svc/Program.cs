using Azure.Identity;
using Azure.Monitor.OpenTelemetry.Exporter;
using Azure.Security.KeyVault.Secrets;
using Biotrackr.Vitals.Svc.Configuration;
using Biotrackr.Vitals.Svc.Repositories;
using Biotrackr.Vitals.Svc.Repositories.Interfaces;
using Biotrackr.Vitals.Svc.Services;
using Biotrackr.Vitals.Svc.Services.Interfaces;
using Biotrackr.Vitals.Svc.Workers;
using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.Configuration.AzureAppConfiguration;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

var resourceAttributes = new Dictionary<string, object>
{
    { "service.name", "Biotrackr.Vitals.Svc" },
    { "service.version", "1.0.0" }
};

var resourceBuilder = ResourceBuilder.CreateDefault().AddAttributes(resourceAttributes);

IHost host = Host.CreateDefaultBuilder(args)
    .ConfigureAppConfiguration(config =>
    {
        config.AddEnvironmentVariables();
        var credential = new ManagedIdentityCredential(Environment.GetEnvironmentVariable("managedidentityclientid"));
        config.AddAzureAppConfiguration(options =>
        {
            options.Connect(
                new Uri(Environment.GetEnvironmentVariable("azureappconfigendpoint")),
                credential)
            .Select(keyFilter: KeyFilter.Any, labelFilter: LabelFilter.Null)
            .ConfigureKeyVault(kv =>
            {
                kv.SetCredential(credential);
            });
        });
    })
    .ConfigureServices((context, services) =>
    {
        var keyVaultUrl = context.Configuration["keyvaulturl"];
        var managedIdentityClientId = context.Configuration["managedidentityclientid"];
        var defaultCredentialOptions = new DefaultAzureCredentialOptions()
        {
            ManagedIdentityClientId = managedIdentityClientId
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

        services.AddSingleton(new SecretClient(new Uri(keyVaultUrl!), new DefaultAzureCredential(defaultCredentialOptions)));
        services.AddSingleton(new CosmosClient(cosmosDbEndpoint!, new DefaultAzureCredential(defaultCredentialOptions), cosmosClientOptions));

        services.AddScoped<ICosmosRepository, CosmosRepository>();
        services.AddScoped<IVitalsService, VitalsService>();

        services.AddHttpClient<IWithingsService, WithingsService>()
            .AddStandardResilienceHandler();

        services.AddHostedService<VitalsWorker>();

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