using Azure.Identity;
using Azure.Messaging.ServiceBus;
using Azure.Security.KeyVault.Secrets;
using Biotrackr.FitbitApi.Configuration;
using Biotrackr.FitbitApi.Services;
using Biotrackr.FitbitApi.Services.Interfaces;
using Biotrackr.FitbitApi.Workers;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Configuration.AzureAppConfiguration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

IHost host = Host.CreateDefaultBuilder(args)
    .ConfigureAppConfiguration((context, config) =>
    {
        config.AddEnvironmentVariables();
        var managedIdentityClientId = context.Configuration["managedidentityclientid"];
        var defaultCredentialOptions = new DefaultAzureCredentialOptions()
        {
            ManagedIdentityClientId = managedIdentityClientId
        };
        config.AddAzureAppConfiguration(options =>
        {
            options
            .Connect(new Uri(Environment.GetEnvironmentVariable("azureappconfigendpoint")), new DefaultAzureCredential(defaultCredentialOptions))
            .Select(KeyFilter.Any, LabelFilter.Null);
        });
    })
    .ConfigureServices((context, services) =>
    {
        var keyVaultUri = context.Configuration["keyvaulturi"];
        var serviceBusEndpoint = context.Configuration["servicebusendpoint"];
        var managedIdentityClientId = context.Configuration["managedidentityclientid"];
        var defaultCredentialOptions = new DefaultAzureCredentialOptions()
        {
            ManagedIdentityClientId = managedIdentityClientId
        };

        services.AddOptions<Settings>().Configure<IConfiguration>((settings, configuration) =>
        {
            configuration.GetSection("biotrackr").Bind(settings);
        });

        services.AddApplicationInsightsTelemetryWorkerService();

        services.AddSingleton(new SecretClient(new Uri(keyVaultUri), new DefaultAzureCredential(defaultCredentialOptions)));
        services.AddSingleton(new ServiceBusClient(serviceBusEndpoint, new DefaultAzureCredential(defaultCredentialOptions)));

        services.AddScoped<ISecretService, SecretService>();
        services.AddScoped<IQueueService, QueueService>();
        services.AddScoped<IFitbitService, FitbitService>();

        services.AddHttpClient<IFitbitService, FitbitService>()
            .AddStandardResilienceHandler();

        services.AddHostedService<FitbitActivityWorker>();
    })
    .Build();