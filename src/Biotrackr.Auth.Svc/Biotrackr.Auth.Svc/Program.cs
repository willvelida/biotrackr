using Azure.Identity;
using Azure.Security.KeyVault.Secrets;
using Biotrackr.Auth.Svc;
using Biotrackr.Auth.Svc.Services;
using Biotrackr.Auth.Svc.Services.Interfaces;

IHost host = Host.CreateDefaultBuilder(args)
    .ConfigureAppConfiguration(config =>
    {
        config.AddEnvironmentVariables();
    })
    .ConfigureServices((context, services) =>
    {
        var keyVaultUrl = context.Configuration["keyvaulturl"];
        var defaultCredentialOptions = new DefaultAzureCredentialOptions()
        {
            ManagedIdentityClientId = context.Configuration["managedidentityclientid"]
        };

        services.AddSingleton(new SecretClient(new Uri(keyVaultUrl), new DefaultAzureCredential(defaultCredentialOptions)));

        services.AddScoped<IRefreshTokenService, RefreshTokenService>();

        services.AddHttpClient<IRefreshTokenService, RefreshTokenService>()
            .AddStandardResilienceHandler();

        services.AddHostedService<AuthWorker>();

        services.AddApplicationInsightsTelemetryWorkerService(options =>
        {
            options.ConnectionString = context.Configuration["applicationinsightsconnectionstring"];
        });
    })
    .Build();

host.Run();
