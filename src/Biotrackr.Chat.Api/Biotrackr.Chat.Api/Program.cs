using Anthropic;
using Azure.Identity;
using Azure.Monitor.OpenTelemetry.Exporter;
using Biotrackr.Chat.Api.Configuration;
using Biotrackr.Chat.Api.Extensions;
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

// OpenTelemetry
var appInsightsConnectionString = builder.Configuration["applicationinsightsconnectionstring"];

builder.Services.AddOpenTelemetry()
    .WithTracing(tracing =>
    {
        tracing.SetResourceBuilder(resourceBuilder)
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

var app = builder.Build();

// Retrieve MCP tools (may be empty if MCP Server is unreachable — degraded mode)
var mcpToolService = app.Services.GetRequiredService<IMcpToolService>();
var memoryCache = app.Services.GetRequiredService<Microsoft.Extensions.Caching.Memory.IMemoryCache>();
var mcpTools = await mcpToolService.GetToolsAsync();

// Wrap each MCP tool with caching
var cachingLogger = app.Services.GetRequiredService<ILoggerFactory>().CreateLogger("CachingMcpToolWrapper");
var wrappedTools = mcpTools
    .Select(tool => CachingMcpToolWrapper.Wrap(tool, memoryCache, cachingLogger))
    .ToList();

// Build the Anthropic-backed AIAgent
var anthropicApiKey = builder.Configuration.GetValue<string>("Biotrackr:AnthropicApiKey");
var modelName = builder.Configuration.GetValue<string>("Biotrackr:ChatAgentModel");
var systemPrompt = builder.Configuration.GetValue<string>("Biotrackr:ChatSystemPrompt")!;

AnthropicClient anthropicClient = new() { ApiKey = anthropicApiKey };

AIAgent chatAgent = anthropicClient.AsAIAgent(
    model: modelName,
    name: "BiotrackrChatAgent",
    instructions: systemPrompt,
    tools: [.. wrappedTools]);

// Wrap agent with conversation persistence middleware
var chatHistoryRepository = app.Services.GetRequiredService<IChatHistoryRepository>();
var persistenceLogger = app.Services.GetRequiredService<ILogger<ConversationPersistenceMiddleware>>();
var conversationPolicyOptions = Microsoft.Extensions.Options.Options.Create(new ConversationPolicyOptions());
var persistenceMiddleware = new ConversationPersistenceMiddleware(chatHistoryRepository, conversationPolicyOptions, persistenceLogger);

// Wrap agent with tool policy enforcement middleware (rate limiting, allowed tool validation)
var biotrackrSettings = app.Services.GetRequiredService<Microsoft.Extensions.Options.IOptions<Settings>>().Value;
var toolPolicyOptions = Microsoft.Extensions.Options.Options.Create(new ToolPolicyOptions
{
    MaxToolCallsPerSession = biotrackrSettings.ToolCallBudgetPerSession
});
var toolPolicyLogger = app.Services.GetRequiredService<ILogger<ToolPolicyMiddleware>>();
var toolPolicyMiddleware = new ToolPolicyMiddleware(memoryCache, toolPolicyOptions, toolPolicyLogger);

// Wrap agent with graceful degradation middleware (catches Claude API failures)
var degradationLogger = app.Services.GetRequiredService<ILogger<GracefulDegradationMiddleware>>();
var degradationMiddleware = new GracefulDegradationMiddleware(degradationLogger);

AIAgent persistentAgent = chatAgent
    .AsBuilder()
        .Use(runFunc: null, runStreamingFunc: toolPolicyMiddleware.HandleAsync)
        .Use(runFunc: null, runStreamingFunc: persistenceMiddleware.HandleAsync)
        .Use(runFunc: null, runStreamingFunc: degradationMiddleware.HandleAsync)
    .Build();

app.MapOpenApi();

// AG-UI endpoint — SSE streaming, session management, protocol events
app.MapAGUI("/", persistentAgent);

// Conversation history + health check endpoints
app.RegisterChatEndpoints();
app.RegisterHealthCheckEndpoints();

app.Run();

[ExcludeFromCodeCoverage]
public partial class Program { }
