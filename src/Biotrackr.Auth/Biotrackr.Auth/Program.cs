using Azure.Identity;
using Azure.Security.KeyVault.Secrets;
using Biotrackr.Auth;
using Biotrackr.Auth.Services;
using Biotrackr.Auth.Services.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

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
    })
    .Build();

host.Run();