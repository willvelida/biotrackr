using Azure.Identity;
using Azure.Security.KeyVault.Secrets;
using Biotrackr.Activity.Svc.Configuration;
using Biotrackr.Activity.Svc.Repositories;
using Biotrackr.Activity.Svc.Repositories.Interfaces;
using Biotrackr.Activity.Svc.Services;
using Biotrackr.Activity.Svc.Services.Interfaces;
using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Biotrackr.Activity.Svc.IntegrationTests.Fixtures;

public class ContractTestFixture : IntegrationTestFixture
{
    protected override bool InitializeDatabase => false;

    public IServiceProvider? ServiceProvider { get; private set; }

    public override async Task InitializeAsync()
    {
        await base.InitializeAsync();

        // Create in-memory configuration for contract tests
        var inMemoryConfig = new Dictionary<string, string?>
        {
            { "keyvaulturl", "https://test-vault.vault.azure.net/" },
            { "managedidentityclientid", "00000000-0000-0000-0000-000000000000" },
            { "cosmosdbendpoint", "https://localhost:8081" },
            { "applicationinsightsconnectionstring", "InstrumentationKey=test-key" },
            { "Biotrackr:DatabaseName", "TestDatabase" },
            { "Biotrackr:ContainerName", "TestContainer" }
        };

        Configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(inMemoryConfig)
            .Build();

        // Setup services matching Program.cs
        var services = new ServiceCollection();

        // Add configuration
        services.AddSingleton<IConfiguration>(Configuration);

        // Add Settings
        services.AddOptions<Settings>().Configure<IConfiguration>((settings, configuration) =>
        {
            configuration.GetSection("Biotrackr").Bind(settings);
        });

        var keyVaultUrl = Configuration["keyvaulturl"] ?? throw new InvalidOperationException("keyvaulturl not configured");
        var managedIdentityClientId = Configuration["managedidentityclientid"];
        var cosmosDbEndpoint = Configuration["cosmosdbendpoint"] ?? throw new InvalidOperationException("cosmosdbendpoint not configured");

        var defaultCredentialOptions = new DefaultAzureCredentialOptions()
        {
            ManagedIdentityClientId = managedIdentityClientId
        };

        var cosmosClientOptions = new CosmosClientOptions()
        {
            SerializerOptions = new CosmosSerializationOptions()
            {
                PropertyNamingPolicy = CosmosPropertyNamingPolicy.CamelCase
            }
        };

        // Register services following Program.cs pattern
        services.AddSingleton(new SecretClient(new Uri(keyVaultUrl), new DefaultAzureCredential(defaultCredentialOptions)));
        services.AddSingleton(new CosmosClient(cosmosDbEndpoint, new DefaultAzureCredential(defaultCredentialOptions), cosmosClientOptions));

        services.AddScoped<ICosmosRepository, CosmosRepository>();
        services.AddScoped<IActivityService, ActivityService>();

        services.AddHttpClient<IFitbitService, FitbitService>()
            .AddStandardResilienceHandler();

        ServiceProvider = services.BuildServiceProvider();
    }

    public override async Task DisposeAsync()
    {
        if (ServiceProvider is IDisposable disposable)
        {
            disposable.Dispose();
        }

        await base.DisposeAsync();
    }
}
