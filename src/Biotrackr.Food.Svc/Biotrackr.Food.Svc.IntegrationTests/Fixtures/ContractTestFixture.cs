namespace Biotrackr.Food.Svc.IntegrationTests.Fixtures;

/// <summary>
/// Test fixture for contract tests that verify service configuration and DI setup
/// without requiring external dependencies like Cosmos DB.
/// </summary>
public class ContractTestFixture : IDisposable
{
    public IServiceProvider ServiceProvider { get; }
    public IConfiguration Configuration { get; }
    private readonly CosmosClient _cosmosClient;
    private readonly SecretClient _secretClient;

    public ContractTestFixture()
    {
        // Build in-memory configuration for contract tests
        var configData = new Dictionary<string, string?>
        {
            ["keyvaulturl"] = "https://test-keyvault.vault.azure.net/",
            ["cosmosdbendpoint"] = "https://localhost:8081",
            ["Biotrackr:DatabaseName"] = "biotrackr-test",
            ["Biotrackr:ContainerName"] = "food-test",
            ["Biotrackr:FitbitApiBaseUrl"] = "https://api.fitbit.com",
            ["CosmosDbAccountKey"] = "C2y6yDjf5/R+ob0N8A7Cgv30VRDJIWEHLM+4QDU5DE2nQ9nDuVTqobD4b8mGGyPMbIZnqyMsEcaGQy67XIw/Jw=="
        };

        Configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(configData)
            .Build();

        // Build service collection mimicking Program.cs registration
        var services = new ServiceCollection();

        // Register configuration
        services.AddSingleton<IConfiguration>(Configuration);

        // Add Settings
        services.AddOptions<Biotrackr.Food.Svc.Configuration.Settings>().Configure<IConfiguration>((settings, configuration) =>
        {
            configuration.GetSection("Biotrackr").Bind(settings);
        });

        // Add logging
        services.AddLogging();

        var keyVaultUrl = Configuration["keyvaulturl"] ?? throw new InvalidOperationException("keyvaulturl not configured");
        var cosmosDbEndpoint = Configuration["cosmosdbendpoint"] ?? throw new InvalidOperationException("cosmosdbendpoint not configured");

        var cosmosClientOptions = new CosmosClientOptions
        {
            SerializerOptions = new CosmosSerializationOptions
            {
                PropertyNamingPolicy = CosmosPropertyNamingPolicy.CamelCase
            }
        };

        // Register Azure SDK clients (real instances for contract tests, won't connect)
        _secretClient = new SecretClient(new Uri(keyVaultUrl), new Azure.Identity.DefaultAzureCredential());
        services.AddSingleton(_secretClient);

        _cosmosClient = new CosmosClient(cosmosDbEndpoint, new Azure.Identity.DefaultAzureCredential(), cosmosClientOptions);
        services.AddSingleton(_cosmosClient);

        // Register application services
        services.AddScoped<ICosmosRepository, CosmosRepository>();
        services.AddScoped<IFoodService, FoodService>();

        // Register HttpClient-based services
        services.AddHttpClient<IFitbitService, FitbitService>()
            .AddStandardResilienceHandler();

        ServiceProvider = services.BuildServiceProvider();
    }

    public void Dispose()
    {
        if (ServiceProvider is IDisposable disposable)
        {
            disposable.Dispose();
        }

        _cosmosClient?.Dispose();
    }
}
