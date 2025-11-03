using System.Diagnostics.CodeAnalysis;
using Azure.Identity;
using Azure.Security.KeyVault.Secrets;
using Biotrackr.Auth.Svc;
using Biotrackr.Auth.Svc.Services;
using Biotrackr.Auth.Svc.Services.Interfaces;

[ExcludeFromCodeCoverage]
internal class Program
{
    private static void Main(string[] args)
    {
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

        services.AddHttpClient<IRefreshTokenService, RefreshTokenService>()
            .AddStandardResilienceHandler();

        services.AddApplicationInsightsTelemetryWorkerService(options =>
        {
            options.ConnectionString = context.Configuration["applicationinsightsconnectionstring"];
        });

        services.AddHostedService<AuthWorker>();
    })
    .Build();

        host.Run();
    }
}
