using Biotrackr.Reporting.Svc.Configuration;
using Biotrackr.Reporting.Svc.Services;
using Biotrackr.Reporting.Svc.Services.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;

namespace Biotrackr.Reporting.Svc.IntegrationTests.Fixtures;

public class ContractTestFixture : IntegrationTestFixture
{
    protected override bool InitializeDatabase => false;

    public IServiceProvider? ServiceProvider { get; private set; }

    public override async Task InitializeAsync()
    {
        await base.InitializeAsync();

        var inMemoryConfig = new Dictionary<string, string?>
        {
            { "keyvaulturl", "https://test-vault.vault.azure.net/" },
            { "managedidentityclientid", "00000000-0000-0000-0000-000000000000" },
            { "applicationinsightsconnectionstring", "InstrumentationKey=test-key" },
            { "azureappconfigendpoint", "https://test-appconfig.azconfig.io" },
            { "Biotrackr:McpServerUrl", "https://test-mcp.example.com" },
            { "Biotrackr:McpServerApiKey", "test-api-key" },
            { "Biotrackr:ApiSubscriptionKey", "test-sub-key" },
            { "Biotrackr:ReportingApiUrl", "https://test-reporting.example.com" },
            { "Biotrackr:ReportingApiScope", "api://test-scope/.default" },
            { "Biotrackr:AgentIdentityId", "00000000-0000-0000-0000-000000000000" },
            { "Biotrackr:EmailSenderAddress", "test@example.com" },
            { "Biotrackr:EmailRecipientAddress", "recipient@example.com" },
            { "Biotrackr:AcsEndpoint", "https://test-acs.communication.azure.com" },
            { "Biotrackr:ReportPollIntervalSeconds", "5" },
            { "Biotrackr:ReportTimeoutMinutes", "10" },
        };

        Configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(inMemoryConfig)
            .Build();

        var services = new ServiceCollection();

        services.AddSingleton<IConfiguration>(Configuration);
        services.AddLogging(builder => builder.AddConsole());

        services.AddOptions<Settings>().Configure<IConfiguration>((settings, configuration) =>
        {
            configuration.GetSection("Biotrackr").Bind(settings);
        });

        // Mock external Azure dependencies that cannot be instantiated without real infrastructure
        var mockAgentTokenProvider = new Mock<IAgentTokenProvider>();
        services.AddSingleton(mockAgentTokenProvider.Object);

        var mockEmailClientWrapper = new Mock<IEmailClientWrapper>();
        services.AddSingleton(mockEmailClientWrapper.Object);

        services.AddSingleton<IMcpClientFactory, McpClientFactory>();

        services.AddHttpClient("ReportingApi", client =>
        {
            client.BaseAddress = new Uri(Configuration["Biotrackr:ReportingApiUrl"]!);
        })
        .AddStandardResilienceHandler();

        services.AddHttpClient("ArtifactDownload");

        services.AddScoped<IHealthDataService, HealthDataService>();
        services.AddScoped<IReportingApiService, ReportingApiService>();
        services.AddScoped<IEmailService, EmailService>();
        services.AddScoped<ISummaryService, SummaryService>();

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
