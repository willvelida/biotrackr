using Azure.Identity;
using Azure.Monitor.OpenTelemetry.Exporter;
using Azure.Security.KeyVault.Secrets;
using Biotrackr.Activity.Svc.Services.Interfaces;
using Biotrackr.Activity.Svc.Workers;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using OpenTelemetry.Resources;

namespace Biotrackr.Activity.Svc.UnitTests.ProgramTests
{
    public class ProgramShould
    {
        [Fact]
        public void ConfigurationIsLoadedCorrectly()
        {
            var host = CreateHostBuilder().Build();
            var configuration = host.Services.GetRequiredService<IConfiguration>();

            Assert.NotNull(configuration["keyvaulturl"]);
            Assert.NotNull(configuration["managedidentityclientid"]);
            Assert.NotNull(configuration["cosmosdbendpoint"]);
            Assert.NotNull(configuration["applicationinsightsconnectionstring"]);
        }

        [Fact]
        public void ServicesAreRegisteredCorrectly()
        {
            var host = CreateHostBuilder().Build();
            var services = host.Services;

            Assert.NotNull(services.GetService<SecretClient>());
            Assert.NotNull(services.GetService<CosmosClient>());
            Assert.NotNull(services.GetService<ICosmosRepository>());
            Assert.NotNull(services.GetService<IFitbitService>());
            Assert.NotNull(services.GetService<IActivityService>());
            Assert.NotNull(services.GetService<IHostedService>());
        }

        [Fact]
        public void LoggingIsConfiguredCorrectly()
        {
            var host = CreateHostBuilder().Build();
            var logger = host.Services.GetService<ILogger<ProgramShould>>();

            Assert.NotNull(logger);
        }

        private IHostBuilder CreateHostBuilder()
        {
            return Host.CreateDefaultBuilder()
                .ConfigureAppConfiguration((context, config) =>
                {
                    var inMemorySettings = new Dictionary<string, string>
                    {
                        {"keyvaulturl", "https://example-vault.vault.azure.net/"},
                        {"managedidentityclientid", "example-client-id"},
                        {"cosmosdbendpoint", "https://example-cosmos.documents.azure.com:443/"},
                        {"applicationinsightsconnectionstring", "InstrumentationKey=example-key"}
                    };

                    config.AddInMemoryCollection(inMemorySettings);
                })
                .ConfigureServices((context, services) =>
                {
                    var keyVaultUrl = context.Configuration["keyvaulturl"];
                    var managedIdentityClient = context.Configuration["managedidentityclientid"];
                    var defaultCredentialOptions = new DefaultAzureCredentialOptions()
                    {
                        ManagedIdentityClientId = managedIdentityClient
                    };

                    services.AddSingleton(new SecretClient(new Uri(keyVaultUrl), new DefaultAzureCredential(defaultCredentialOptions)));
                    services.AddSingleton(new CosmosClient(context.Configuration["cosmosdbendpoint"], new DefaultAzureCredential(defaultCredentialOptions), new CosmosClientOptions()));

                    var mockCosmosRepository = new Mock<ICosmosRepository>();
                    var mockActivityService = new Mock<IActivityService>();
                    var mockFitbitService = new Mock<IFitbitService>();
                    services.AddScoped<ICosmosRepository>(_ => mockCosmosRepository.Object);
                    services.AddScoped<IActivityService>(_ => mockActivityService.Object);
                    services.AddScoped<IFitbitService>(_ => mockFitbitService.Object);

                    services.AddHostedService<ActivityWorker>();
                })
                .ConfigureLogging((context, logging) =>
                {
                    logging.AddConsole();
                    logging.AddDebug();
                    logging.AddOpenTelemetry(log =>
                    {
                        var resourceAttributes = new Dictionary<string, object>
                        {
                            { "service.name", "Biotrackr.Activity.Svc" },
                            { "service.version", "1.0.0" }
                        };
                        var resourceBuilder = ResourceBuilder.CreateDefault().AddAttributes(resourceAttributes);

                        log.SetResourceBuilder(resourceBuilder);
                        log.AddAzureMonitorLogExporter(options =>
                        {
                            options.ConnectionString = context.Configuration["applicationinsightsconnectionstring"];
                        });
                    });
                }); ;
        }
    }
}
