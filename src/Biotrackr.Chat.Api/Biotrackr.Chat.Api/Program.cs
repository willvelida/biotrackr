using Anthropic;
using Azure.Identity;
using Azure.Monitor.OpenTelemetry.Exporter;
using Biotrackr.Chat.Api.Configuration;
using Biotrackr.Chat.Api.Configuration;
using Biotrackr.Chat.Api.Extensions;
using Biotrackr.Chat.Api.Middleware;
using Biotrackr.Chat.Api.Services;
using Biotrackr.Chat.Api.Tools;
using Microsoft.Agents.AI;
using Microsoft.Agents.AI.Hosting;
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

// HttpClient for calling Biotrackr APIs via APIM
builder.Services.AddTransient<ApiKeyDelegatingHandler>();

builder.Services.AddHttpClient("BiotrackrApi", (sp, client) =>
{
    var settings = sp.GetRequiredService<Microsoft.Extensions.Options.IOptions<Settings>>().Value;
    client.BaseAddress = new Uri(settings.ApiBaseUrl ?? throw new InvalidOperationException("Biotrackr:ApiBaseUrl is not configured."));
})
.AddHttpMessageHandler<ApiKeyDelegatingHandler>()
.AddStandardResilienceHandler();

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

// Resolve tool dependencies from DI
var httpClientFactory = app.Services.GetRequiredService<IHttpClientFactory>();
var memoryCache = app.Services.GetRequiredService<Microsoft.Extensions.Caching.Memory.IMemoryCache>();
var activityTools = new ActivityTools(httpClientFactory, memoryCache);
var sleepTools = new SleepTools(httpClientFactory, memoryCache);
var weightTools = new WeightTools(httpClientFactory, memoryCache);
var foodTools = new FoodTools(httpClientFactory, memoryCache);

// Build the Anthropic-backed AIAgent
var anthropicApiKey = builder.Configuration.GetValue<string>("Biotrackr:AnthropicApiKey");
var modelName = builder.Configuration.GetValue<string>("Biotrackr:ChatAgentModel");
var systemPrompt = builder.Configuration.GetValue<string>("Biotrackr:ChatSystemPrompt")!;

AnthropicClient anthropicClient = new() { ApiKey = anthropicApiKey };

AIAgent chatAgent = anthropicClient.AsAIAgent(
    model: modelName,
    name: "BiotrackrChatAgent",
    instructions: systemPrompt,
    tools:
    [
        AIFunctionFactory.Create(activityTools.GetActivityByDate),
        AIFunctionFactory.Create(activityTools.GetActivityByDateRange),
        AIFunctionFactory.Create(activityTools.GetActivityRecords),
        AIFunctionFactory.Create(sleepTools.GetSleepByDate),
        AIFunctionFactory.Create(sleepTools.GetSleepByDateRange),
        AIFunctionFactory.Create(sleepTools.GetSleepRecords),
        AIFunctionFactory.Create(weightTools.GetWeightByDate),
        AIFunctionFactory.Create(weightTools.GetWeightByDateRange),
        AIFunctionFactory.Create(weightTools.GetWeightRecords),
        AIFunctionFactory.Create(foodTools.GetFoodByDate),
        AIFunctionFactory.Create(foodTools.GetFoodByDateRange),
        AIFunctionFactory.Create(foodTools.GetFoodRecords),
    ]);

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

// Wrap with AIHostAgent so sessions (and their ConversationId) persist between requests
var hostedAgent = new AIHostAgent(persistentAgent, new InMemoryAgentSessionStore());

app.MapOpenApi();

// AG-UI endpoint — SSE streaming, session management, protocol events
app.MapAGUI("/", hostedAgent);

// Conversation history + health check endpoints
app.RegisterChatEndpoints();
app.RegisterHealthCheckEndpoints();

app.Run();

[ExcludeFromCodeCoverage]
public partial class Program { }
