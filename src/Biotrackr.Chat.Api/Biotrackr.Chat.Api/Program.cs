using Anthropic;
using Azure.Identity;
using Azure.Monitor.OpenTelemetry.Exporter;
using Biotrackr.Chat.Api.Configuration;
using Biotrackr.Chat.Api.Extensions;
using Biotrackr.Chat.Api.Handlers;
using Biotrackr.Chat.Api.Middleware;
using Biotrackr.Chat.Api.Services;
using Biotrackr.Chat.Api.Tools;
using Microsoft.Agents.AI;
using Microsoft.Agents.AI.Hosting.AGUI.AspNetCore;
using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Configuration.AzureAppConfiguration;
using Microsoft.Identity.Web;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using System.Diagnostics.CodeAnalysis;

var resourceAttributes = new Dictionary<string, object>
{
    { "service.name", "Biotrackr.Chat.Api" },
    { "service.version", "1.0.0" }
};
var resourceBuilder = ResourceBuilder.CreateDefault().AddAttributes(resourceAttributes);

// Approach B reverted — redeploy with original RequestReportTool plain text return
var builder = WebApplication.CreateBuilder(args);

builder.Configuration.AddEnvironmentVariables();
var managedIdentityClientId = builder.Configuration.GetValue<string>("managedidentityclientid");
var azureAppConfigEndpoint = builder.Configuration.GetValue<string>("azureappconfigendpoint");

// Azure App Configuration (skipped in test environment)
if (!string.IsNullOrWhiteSpace(azureAppConfigEndpoint))
{
    builder.Configuration.AddAzureAppConfiguration(config =>
    {
        var credential = new ManagedIdentityCredential(managedIdentityClientId);
        config.Connect(new Uri(azureAppConfigEndpoint), credential)
        .Select(KeyFilter.Any, LabelFilter.Null)
        .ConfigureKeyVault(kv =>
        {
            kv.SetCredential(credential);
        });
    });
}

var defaultCredentialOptions = new DefaultAzureCredentialOptions()
{
    ManagedIdentityClientId = managedIdentityClientId
};

builder.Services.Configure<Settings>(builder.Configuration.GetSection("Biotrackr"));

// Authentication with Microsoft Identity Web + agent identity token credential
builder.Services.AddAuthentication()
    .AddMicrosoftIdentityWebApi(builder.Configuration.GetSection("AzureAd"))
    .EnableTokenAcquisitionToCallDownstreamApi()
    .AddInMemoryTokenCaches();

builder.Services.AddMicrosoftIdentityAzureTokenCredential();
builder.Services.AddAgentIdentities();

// Cosmos DB via agent identity
builder.Services.AddScoped<ICosmosClientFactory, AgentIdentityCosmosClientFactory>();

// Services
builder.Services.AddScoped<IChatHistoryRepository, ChatHistoryRepository>();
builder.Services.AddMemoryCache();

// MCP tool service — manages MCP client lifecycle and tool listing
builder.Services.AddSingleton<IMcpToolService, McpToolService>();
builder.Services.AddHostedService(sp => (McpToolService)sp.GetRequiredService<IMcpToolService>());

// Reporting tools — native function tools for report generation and retrieval
builder.Services.AddSingleton<IAgentTokenProvider, AgentTokenProvider>();
builder.Services.AddTransient<AgentIdentityTokenHandler>();
builder.Services.AddHttpClient("ReportingApi", client =>
{
    var reportingApiUrl = builder.Configuration.GetValue<string>("Biotrackr:ReportingApiUrl");
    if (!string.IsNullOrWhiteSpace(reportingApiUrl))
    {
        client.BaseAddress = new Uri(reportingApiUrl);
    }
}).AddHttpMessageHandler<AgentIdentityTokenHandler>()
.AddStandardResilienceHandler(options =>
{
    // Allow longer timeouts for Reporting.Api cold starts (scale-to-zero Container App + sidecar)
    options.TotalRequestTimeout.Timeout = TimeSpan.FromMinutes(3);
    options.AttemptTimeout.Timeout = TimeSpan.FromSeconds(90);
    options.CircuitBreaker.SamplingDuration = TimeSpan.FromMinutes(3);
    options.Retry.MaxRetryAttempts = 5;
    options.Retry.Delay = TimeSpan.FromSeconds(15);
    options.Retry.BackoffType = Polly.DelayBackoffType.Linear;
});

builder.Services.AddSingleton<ReportReviewerService>();

// A2A client for Reporting.Api — uses same base URL, different path prefix (/a2a/report)
builder.Services.AddHttpClient("A2AReportingClient", client =>
{
    var reportingApiUrl = builder.Configuration.GetValue<string>("Biotrackr:ReportingApiUrl");
    if (!string.IsNullOrWhiteSpace(reportingApiUrl))
    {
        client.BaseAddress = new Uri(reportingApiUrl);
    }
}).AddHttpMessageHandler<AgentIdentityTokenHandler>()
.AddStandardResilienceHandler(options =>
{
    options.TotalRequestTimeout.Timeout = TimeSpan.FromMinutes(3);
    options.AttemptTimeout.Timeout = TimeSpan.FromSeconds(90);
    options.CircuitBreaker.SamplingDuration = TimeSpan.FromMinutes(3);
    options.Retry.MaxRetryAttempts = 3;
    options.Retry.Delay = TimeSpan.FromSeconds(5);
    options.Retry.BackoffType = Polly.DelayBackoffType.Exponential;
});

builder.Services.AddTransient<AnthropicMetricsHandler>();
builder.Services.AddHttpClient("Anthropic")
    .AddHttpMessageHandler<AnthropicMetricsHandler>();

builder.Services.AddSingleton<A2AReportTool>();
builder.Services.AddSingleton<AIFunction>(sp => sp.GetRequiredService<A2AReportTool>().AsGenerateReportFunction());
builder.Services.AddSingleton<AIFunction>(sp => sp.GetRequiredService<A2AReportTool>().AsCheckReportStatusFunction());

// OpenTelemetry
var appInsightsConnectionString = builder.Configuration["applicationinsightsconnectionstring"];

builder.Services.AddOpenTelemetry()
    .WithTracing(tracing =>
    {
        tracing.SetResourceBuilder(resourceBuilder)
            .AddSource("gen_ai.anthropic")
            .AddAspNetCoreInstrumentation()
            .AddHttpClientInstrumentation()
            .AddAzureMonitorTraceExporter(options =>
            {
                options.ConnectionString = appInsightsConnectionString;
            });
    })
    .WithMetrics(metrics =>
    {
        metrics.SetResourceBuilder(resourceBuilder)
            .AddAspNetCoreInstrumentation()
            .AddHttpClientInstrumentation()
            .AddMeter("Biotrackr.Chat.Anthropic")
            .AddAzureMonitorMetricExporter(options =>
            {
                options.ConnectionString = appInsightsConnectionString;
            });
    });

builder.Logging.AddOpenTelemetry(log =>
{
    log.SetResourceBuilder(resourceBuilder);
    log.AddAzureMonitorLogExporter(options =>
    {
        options.ConnectionString = appInsightsConnectionString;
    });
});

// OpenAPI
builder.Services.AddOpenApi();

// Health checks
builder.Services.AddHealthChecks();

// AG-UI services
builder.Services.AddAGUI();

// ChatAgentProvider — builds/rebuilds AIAgent when MCP tools change
builder.Services.AddSingleton<ChatAgentProvider>();

var app = builder.Build();

// Create a delegating agent that resolves the real agent per-request.
// This ensures the agent is rebuilt when MCP tools become available after a degraded start.
var agentProvider = app.Services.GetRequiredService<ChatAgentProvider>();

// Build an initial agent (may have 0 tools if MCP Server is unreachable)
var initialAgent = await agentProvider.GetAgentAsync();

// Wrap with a delegating layer that always fetches the latest agent
AIAgent dynamicAgent = initialAgent
    .AsBuilder()
    .Use(runFunc: null, runStreamingFunc: (messages, session, options, innerNext, cancellationToken) =>
    {
        return agentProvider.RunStreamingWithLatestAgentAsync(messages, session, options, cancellationToken);
    })
    .Build();

app.MapOpenApi();

// AG-UI endpoint — SSE streaming, session management, protocol events
app.MapAGUI("/", dynamicAgent);

// Conversation history + health check endpoints
app.RegisterChatEndpoints();
app.RegisterHealthCheckEndpoints();

app.Run();

[ExcludeFromCodeCoverage]
public partial class Program { }
