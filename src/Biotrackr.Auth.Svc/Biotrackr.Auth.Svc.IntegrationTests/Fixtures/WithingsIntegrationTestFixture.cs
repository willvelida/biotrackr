using Azure.Security.KeyVault.Secrets;
using Biotrackr.Auth.Svc;
using Biotrackr.Auth.Svc.Services;
using Biotrackr.Auth.Svc.Services.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Moq;

namespace Biotrackr.Auth.Svc.IntegrationTests.Fixtures
{
    public class WithingsIntegrationTestFixture : IDisposable
    {
        public IServiceProvider ServiceProvider { get; private set; }
        public IConfiguration Configuration { get; private set; }
        public Mock<SecretClient> MockSecretClient { get; private set; }
        public Mock<HttpMessageHandler> MockHttpMessageHandler { get; private set; }

        public WithingsIntegrationTestFixture()
        {
            var configurationBuilder = new ConfigurationBuilder()
                .AddJsonFile("appsettings.Test.json", optional: false)
                .AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["keyvaulturl"] = "https://test-keyvault.vault.azure.net/",
                    ["managedidentityclientid"] = "00000000-0000-0000-0000-000000000000",
                    ["applicationinsightsconnectionstring"] = "InstrumentationKey=00000000-0000-0000-0000-000000000000",
                    ["provider"] = "withings"
                });

            Configuration = configurationBuilder.Build();

            MockSecretClient = new Mock<SecretClient>();
            MockHttpMessageHandler = new Mock<HttpMessageHandler>();

            var hostBuilder = Host.CreateDefaultBuilder()
                .ConfigureAppConfiguration(config =>
                {
                    config.AddConfiguration(Configuration);
                })
                .ConfigureServices((context, services) =>
                {
                    services.AddSingleton(MockSecretClient.Object);

                    services.AddHttpClient<IWithingsRefreshTokenService, WithingsRefreshTokenService>()
                        .ConfigurePrimaryHttpMessageHandler(() => MockHttpMessageHandler.Object);

                    services.AddHostedService<WithingsAuthWorker>();

                    services.AddLogging(logging =>
                    {
                        logging.ClearProviders();
                        logging.AddConsole();
                        logging.SetMinimumLevel(LogLevel.Warning);
                    });
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
