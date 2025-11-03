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
    /// <summary>
    /// Full integration test fixture with mocked external dependencies.
    /// Provides mocked SecretClient and HttpClient for E2E tests.
    /// Stateless mocks are configured per-test via Setup calls - no cleanup needed between tests.
    /// </summary>
    public class IntegrationTestFixture : IDisposable
    {
        public IServiceProvider ServiceProvider { get; private set; }
        public IConfiguration Configuration { get; private set; }
        public Mock<SecretClient> MockSecretClient { get; private set; }
        public Mock<HttpMessageHandler> MockHttpMessageHandler { get; private set; }

        public IntegrationTestFixture()
        {
            // Build test configuration
            var configurationBuilder = new ConfigurationBuilder()
                .AddJsonFile("appsettings.Test.json", optional: false)
                .AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["keyvaulturl"] = "https://test-keyvault.vault.azure.net/",
                    ["managedidentityclientid"] = "00000000-0000-0000-0000-000000000000",
                    ["applicationinsightsconnectionstring"] = "InstrumentationKey=00000000-0000-0000-0000-000000000000"
                });

            Configuration = configurationBuilder.Build();

            // Create mocked dependencies
            MockSecretClient = new Mock<SecretClient>();
            MockHttpMessageHandler = new Mock<HttpMessageHandler>();

            // Build service provider with mocked dependencies
            var hostBuilder = Host.CreateDefaultBuilder()
                .ConfigureAppConfiguration(config =>
                {
                    config.AddConfiguration(Configuration);
                })
                .ConfigureServices((context, services) =>
                {
                    // Register mocked Azure SDK clients
                    services.AddSingleton(MockSecretClient.Object);
                    
                    // Register application services (mirroring Program.cs)
                    services.AddHttpClient<IRefreshTokenService, RefreshTokenService>()
                        .ConfigurePrimaryHttpMessageHandler(() => MockHttpMessageHandler.Object);
                    
                    // Register background worker service
                    services.AddHostedService<AuthWorker>();
                    
                    // Add logging for tests
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
