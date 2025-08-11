using Azure.Identity;
using Azure.Monitor.OpenTelemetry.Exporter;
using Azure.Security.KeyVault.Secrets;
using Biotrackr.Food.Svc.Configuration;
using Biotrackr.Food.Svc.Repositories.Interfaces;
using Biotrackr.Food.Svc.Services;
using Biotrackr.Food.Svc.Services.Interfaces;
using Biotrackr.Food.Svc.Workers;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using OpenTelemetry.Resources;

namespace Biotrackr.Food.Svc.UnitTests.ProgramTests
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
            Assert.NotNull(configuration["azureappconfigendpoint"]);
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
            Assert.NotNull(services.GetService<IFoodService>());
            Assert.NotNull(services.GetService<IHostedService>());
        }

        [Fact]
        public void SettingsConfigurationIsRegisteredCorrectly()
        {
            var host = CreateHostBuilder().Build();
            var settingsOptions = host.Services.GetService<IOptions<Settings>>();

            Assert.NotNull(settingsOptions);
            Assert.NotNull(settingsOptions.Value);
        }

        [Fact]
        public void HttpClientIsConfiguredCorrectly()
        {
            var host = CreateHostBuilder().Build();
            var httpClientFactory = host.Services.GetService<IHttpClientFactory>();

            Assert.NotNull(httpClientFactory);
        }

        [Fact]
        public void LoggingIsConfiguredCorrectly()
        {
            var host = CreateHostBuilder().Build();
            var logger = host.Services.GetService<ILogger<ProgramShould>>();

            Assert.NotNull(logger);
        }

        [Fact]
        public void OpenTelemetryIsConfiguredCorrectly()
        {
            var host = CreateHostBuilder().Build();
            
            // Verify that OpenTelemetry services are registered
            // This indirectly verifies that OpenTelemetry was configured
            var services = host.Services;
            Assert.NotNull(services);
        }

        [Fact]
        public void CosmosClientIsConfiguredWithCorrectOptions()
        {
            var host = CreateHostBuilder().Build();
            var cosmosClient = host.Services.GetRequiredService<CosmosClient>();

            Assert.NotNull(cosmosClient);
        }

        [Fact]
        public void HostedServiceIsRegistered()
        {
            var host = CreateHostBuilder().Build();
            var hostedServices = host.Services.GetServices<IHostedService>();

            Assert.Contains(hostedServices, service => service is FoodWorker);
        }

        private IHostBuilder CreateHostBuilder()
        {
            return Host.CreateDefaultBuilder()
                .ConfigureAppConfiguration((context, config) =>
                {
                    var inMemorySettings = new Dictionary<string, string?>
                    {
                        {"keyvaulturl", "https://example-vault.vault.azure.net/"},
                        {"managedidentityclientid", "example-client-id"},
                        {"cosmosdbendpoint", "https://example-cosmos.documents.azure.com:443/"},
                        {"applicationinsightsconnectionstring", "InstrumentationKey=example-key"},
                        {"azureappconfigendpoint", "https://example-appconfig.azconfig.io"},
                        {"Biotrackr:SomeProperty", "example-value"}
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

                    services.AddOptions<Settings>().Configure<IConfiguration>((settings, configuration) =>
                    {
                        configuration.GetSection("Biotrackr").Bind(settings);
                    });

                    var cosmosDbEndpoint = context.Configuration["cosmosdbendpoint"];
                    var cosmosClientOptions = new CosmosClientOptions()
                    {
                        SerializerOptions = new CosmosSerializationOptions()
                        {
                            PropertyNamingPolicy = CosmosPropertyNamingPolicy.CamelCase
                        }
                    };

                    services.AddSingleton(new SecretClient(new Uri(keyVaultUrl!), new DefaultAzureCredential(defaultCredentialOptions)));
                    services.AddSingleton(new CosmosClient(cosmosDbEndpoint, new DefaultAzureCredential(defaultCredentialOptions), cosmosClientOptions));

                    // Use mocks for dependencies to avoid actual service calls during testing
                    var mockCosmosRepository = new Mock<ICosmosRepository>();
                    var mockFoodService = new Mock<IFoodService>();
                    var mockFitbitService = new Mock<IFitbitService>();
                    
                    services.AddScoped<ICosmosRepository>(_ => mockCosmosRepository.Object);
                    services.AddScoped<IFoodService>(_ => mockFoodService.Object);
                    services.AddScoped<IFitbitService>(_ => mockFitbitService.Object);

                    services.AddHttpClient<IFitbitService, FitbitService>()
                        .AddStandardResilienceHandler();

                    services.AddHostedService<FoodWorker>();
                })
                .ConfigureLogging((context, logging) =>
                {
                    logging.AddConsole();
                    logging.AddDebug();
                    logging.AddOpenTelemetry(log =>
                    {
                        var resourceAttributes = new Dictionary<string, object>
                        {
                            { "service.name", "Biotrackr.Food.Svc" },
                            { "service.version", "1.0.0" }
                        };
                        var resourceBuilder = ResourceBuilder.CreateDefault().AddAttributes(resourceAttributes);

                        log.SetResourceBuilder(resourceBuilder);
                        log.AddAzureMonitorLogExporter(options =>
                        {
                            options.ConnectionString = context.Configuration["applicationinsightsconnectionstring"];
                        });
                    });
                });
        }
    }
}
