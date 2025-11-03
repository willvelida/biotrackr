using Azure.Identity;
using Azure.Security.KeyVault.Secrets;
using Biotrackr.Auth.Svc;
using Biotrackr.Auth.Svc.Services;
using Biotrackr.Auth.Svc.Services.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Biotrackr.Auth.Svc.IntegrationTests.Fixtures
{
    /// <summary>
    /// Lightweight test fixture for contract tests that verify DI and startup behavior.
    /// Does not initialize external services - uses in-memory configuration only.
    /// </summary>
    public class ContractTestFixture : IDisposable
    {
        public IServiceProvider ServiceProvider { get; private set; }
        public IConfiguration Configuration { get; private set; }

        public ContractTestFixture()
        {
            // Build in-memory configuration for testing
            var configurationBuilder = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["keyvaulturl"] = "https://test-keyvault.vault.azure.net/",
                    ["managedidentityclientid"] = "00000000-0000-0000-0000-000000000000",
                    ["applicationinsightsconnectionstring"] = "InstrumentationKey=00000000-0000-0000-0000-000000000000"
                });

            Configuration = configurationBuilder.Build();

            // Build service provider with test configuration
            // Mirrors Program.cs service registration for contract validation
            var hostBuilder = Host.CreateDefaultBuilder()
                .ConfigureAppConfiguration(config =>
                {
                    config.AddConfiguration(Configuration);
                })
                .ConfigureServices((context, services) =>
                {
                    var keyVaultUrl = context.Configuration["keyvaulturl"];
                    var defaultCredentialOptions = new DefaultAzureCredentialOptions()
                    {
                        ManagedIdentityClientId = context.Configuration["managedidentityclientid"]
                    };

                    // Register services as they are in Program.cs
                    services.AddSingleton(new SecretClient(new Uri(keyVaultUrl!), new DefaultAzureCredential(defaultCredentialOptions)));

                    services.AddHttpClient<IRefreshTokenService, RefreshTokenService>()
                        .AddStandardResilienceHandler();

                    services.AddApplicationInsightsTelemetryWorkerService(options =>
                    {
                        options.ConnectionString = context.Configuration["applicationinsightsconnectionstring"];
                    });

                    services.AddHostedService<AuthWorker>();
                });

            var host = hostBuilder.Build();
            ServiceProvider = host.Services;
        }

        public void Dispose()
        {
            if (ServiceProvider is IDisposable disposable)
            {
                disposable.Dispose();
            }
        }
    }
}
