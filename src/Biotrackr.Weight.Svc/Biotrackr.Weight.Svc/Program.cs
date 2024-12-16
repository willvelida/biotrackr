using Azure.Identity;
using Azure.Security.KeyVault.Secrets;
using Biotrackr.Weight.Svc.Repositories;
using Biotrackr.Weight.Svc.Repositories.Interfaces;
using Biotrackr.Weight.Svc.Services;
using Biotrackr.Weight.Svc.Services.Interfaces;
using Biotrackr.Weight.Svc.Workers;
using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.Configuration.AzureAppConfiguration;

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
        var managedIdentityClientId = context.Configuration["managedidentityclientid"];
        var defaultCredentialOptions = new DefaultAzureCredentialOptions()
        {
            ManagedIdentityClientId = managedIdentityClientId
        };

        services.AddApplicationInsightsTelemetryWorkerService();

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
        services.AddScoped<IWeightService, WeightService>();

        services.AddHttpClient<IFitbitService, FitbitService>()
            .AddStandardResilienceHandler();

        services.AddHostedService<WeightWorker>();
    })
    .Build();

host.Run();