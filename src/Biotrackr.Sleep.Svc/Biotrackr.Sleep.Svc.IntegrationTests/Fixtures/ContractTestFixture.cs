using Azure.Identity;
using Azure.Security.KeyVault.Secrets;
using Biotrackr.Sleep.Svc.Configuration;
using Biotrackr.Sleep.Svc.Repositories;
using Biotrackr.Sleep.Svc.Repositories.Interfaces;
using Biotrackr.Sleep.Svc.Services;
using Biotrackr.Sleep.Svc.Services.Interfaces;
using Biotrackr.Sleep.Svc.Worker;
using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Moq;

namespace Biotrackr.Sleep.Svc.IntegrationTests.Fixtures
{
    public class ContractTestFixture : IDisposable
    {
        public IServiceProvider ServiceProvider { get; private set; }
        private IHost? _host;

        public ContractTestFixture()
        {
            // Set environment variables before building host (configuration timing issue)
            Environment.SetEnvironmentVariable("keyvaulturl", "https://test-keyvault.vault.azure.net/");
            Environment.SetEnvironmentVariable("managedidentityclientid", "00000000-0000-0000-0000-000000000000");
            Environment.SetEnvironmentVariable("cosmosdbendpoint", "https://localhost:8081");
            Environment.SetEnvironmentVariable("applicationinsightsconnectionstring", "InstrumentationKey=00000000-0000-0000-0000-000000000000");
            Environment.SetEnvironmentVariable("azureappconfigendpoint", "https://test-appconfig.azconfig.io");

            // Build host without database initialization
            var hostBuilder = Host.CreateDefaultBuilder()
                .ConfigureAppConfiguration(config =>
                {
                    config.AddJsonFile("appsettings.Test.json", optional: false);
                    config.AddEnvironmentVariables();
                })
                .ConfigureServices((context, services) =>
                {
                    // Configure settings
                    services.AddOptions<Settings>().Configure<IConfiguration>((settings, configuration) =>
                    {
                        configuration.GetSection("Biotrackr").Bind(settings);
                    });

                    // Mock external dependencies
                    var mockSecretClient = new Mock<SecretClient>();
                    services.AddSingleton(mockSecretClient.Object);

                    var mockCosmosClient = new Mock<CosmosClient>();
                    services.AddSingleton(mockCosmosClient.Object);

                    // Register application services
                    services.AddScoped<ICosmosRepository, CosmosRepository>();
                    services.AddScoped<ISleepService, SleepService>();
                    
                    services.AddHttpClient<IFitbitService, FitbitService>()
                        .AddStandardResilienceHandler();

                    services.AddHostedService<SleepWorker>();
                });

            _host = hostBuilder.Build();
            ServiceProvider = _host.Services;
        }

        public void Dispose()
        {
            _host?.Dispose();
        }
    }
}
