using Azure.Communication.Email;
using Azure.Identity;
using Azure.Monitor.OpenTelemetry.Exporter;
using Azure.Security.KeyVault.Secrets;
using Biotrackr.Reporting.Svc.Configuration;
using Biotrackr.Reporting.Svc.Services;
using Biotrackr.Reporting.Svc.Services.Interfaces;
using Biotrackr.Reporting.Svc.Workers;
using Microsoft.Extensions.Configuration.AzureAppConfiguration;
using Microsoft.Identity.Web;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

var resourceAttributes = new Dictionary<string, object>
{
    { "service.name", "Biotrackr.Reporting.Svc" },
    { "service.version", "1.0.0" }
};

var resourceBuilder = ResourceBuilder.CreateDefault().AddAttributes(resourceAttributes);

IHost host = Host.CreateDefaultBuilder(args)
    .ConfigureAppConfiguration(config =>
    {
        var credential = new ManagedIdentityCredential(Environment.GetEnvironmentVariable("managedidentityclientid"));
        config.AddAzureAppConfiguration(options =>
        {
            options.Connect(
                new Uri(Environment.GetEnvironmentVariable("azureappconfigendpoint")!),
                credential)
            .Select(keyFilter: KeyFilter.Any, LabelFilter.Null)
            .ConfigureKeyVault(kv =>
            {
                kv.SetCredential(credential);
            });
        });
        // Load env vars AFTER App Config so container env vars (AzureAd__ClientId,
        // AzureAd__TenantId) override shared App Config keys set by Chat.Api
        config.AddEnvironmentVariables();
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
            settings.SummaryCadence = Environment.GetEnvironmentVariable("summarycadence") ?? string.Empty;
        });

        services.AddSingleton(new SecretClient(new Uri(keyVaultUrl!), new DefaultAzureCredential(defaultCredentialOptions)));

        var acsEndpoint = context.Configuration["Biotrackr:AcsEndpoint"];
        services.AddSingleton(new EmailClient(new Uri(acsEndpoint!), new DefaultAzureCredential(defaultCredentialOptions)));
        services.AddSingleton<IEmailClientWrapper, EmailClientWrapper>();

        // Register Microsoft Identity token acquisition infrastructure
        // Required for MicrosoftIdentityTokenCredential used by AgentTokenProvider
        // Matches Chat.Api pattern: AddMicrosoftIdentityWebApi → EnableTokenAcquisition → TokenCaches → AgentIdentities
        services.AddAuthentication()
            .AddMicrosoftIdentityWebApi(context.Configuration.GetSection("AzureAd"))
            .EnableTokenAcquisitionToCallDownstreamApi()
            .AddInMemoryTokenCaches();
        services.AddMicrosoftIdentityAzureTokenCredential();
        services.AddAgentIdentities();
        services.AddSingleton<IAgentTokenProvider, AgentTokenProvider>();
        services.AddTransient<AgentIdentityTokenHandler>();

        services.AddHttpClient("ReportingApi", (sp, client) =>
        {
            var reportingApiUrl = context.Configuration["Biotrackr:ReportingApiUrl"];
            client.BaseAddress = new Uri(reportingApiUrl!);
        })
        .AddHttpMessageHandler<AgentIdentityTokenHandler>()
        .AddStandardResilienceHandler(options =>
        {
            // Reporting.Api has cold starts that exceed the default 10s attempt timeout.
            // Increase to 60s to accommodate container scale-up latency.
            options.AttemptTimeout.Timeout = TimeSpan.FromSeconds(60);
        });

        services.AddHttpClient("ArtifactDownload");

        services.AddSingleton<IMcpClientFactory, McpClientFactory>();
        services.AddScoped<IHealthDataService, HealthDataService>();
        services.AddScoped<IReportingApiService, ReportingApiService>();
        services.AddScoped<IEmailService, EmailService>();
        services.AddScoped<ISummaryService, SummaryService>();

        services.AddHostedService<ReportingWorker>();

        services.AddOpenTelemetry()
            .WithTracing(tracing =>
            {
                tracing.SetResourceBuilder(resourceBuilder)
                        .AddAzureMonitorTraceExporter(options =>
                        {
                            options.ConnectionString = context.Configuration["applicationinsightsconnectionstring"];
                        });
            })
            .WithMetrics(metrics =>
            {
                metrics.SetResourceBuilder(resourceBuilder)
                        .AddAzureMonitorMetricExporter(options =>
                        {
                            options.ConnectionString = context.Configuration["applicationinsightsconnectionstring"];
                        });
            });
    })
    .ConfigureLogging((context, logging) =>
    {
        logging.AddOpenTelemetry(log =>
        {
            log.SetResourceBuilder(resourceBuilder);
            log.AddAzureMonitorLogExporter(options =>
            {
                options.ConnectionString = context.Configuration["applicationinsightsconnectionstring"];
            });
        });
    })
    .Build();

host.Run();
