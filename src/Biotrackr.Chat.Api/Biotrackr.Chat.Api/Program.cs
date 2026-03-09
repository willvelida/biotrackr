using Anthropic;
using Azure.Identity;
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
using OpenTelemetry.Metrics;
using OpenTelemetry.Trace;

var builder = WebApplication.CreateBuilder(args);

builder.Configuration.AddEnvironmentVariables();
var managedIdentityClientId = builder.Configuration.GetValue<string>("managedidentityclientid");
var azureAppConfigEndpoint = builder.Configuration.GetValue<string>("azureappconfigendpoint");

// Azure App Configuration (skipped in test environment)
if (!string.IsNullOrWhiteSpace(azureAppConfigEndpoint))
{
    builder.Configuration.AddAzureAppConfiguration(config =>
    {
        config.Connect(new Uri(azureAppConfigEndpoint),
            new ManagedIdentityCredential(managedIdentityClientId))
        .Select(KeyFilter.Any, LabelFilter.Null);
    });
}

var defaultCredentialOptions = new DefaultAzureCredentialOptions()
{
    ManagedIdentityClientId = managedIdentityClientId
};

builder.Services.Configure<Settings>(builder.Configuration.GetSection("Biotrackr"));

// Cosmos DB
var cosmosClientOptions = new CosmosClientOptions
{
    SerializerOptions = new CosmosSerializationOptions
    {
        PropertyNamingPolicy = CosmosPropertyNamingPolicy.CamelCase
    }
};
var cosmosClient = new CosmosClient(
    builder.Configuration.GetValue<string>("cosmosdbendpoint"),
    new DefaultAzureCredential(defaultCredentialOptions),
    cosmosClientOptions);
builder.Services.AddSingleton(cosmosClient);

// Services
builder.Services.AddScoped<IChatHistoryRepository, ChatHistoryRepository>();
builder.Services.AddMemoryCache();

// HttpClient for calling Biotrackr APIs via APIM
builder.Services.AddHttpClient("BiotrackrApi", client =>
{
    client.BaseAddress = new Uri(builder.Configuration.GetValue<string>("Biotrackr:ApiBaseUrl")!);
}).AddStandardResilienceHandler();

// OpenTelemetry
builder.Services.AddOpenTelemetry()
    .WithTracing(tracing => tracing
        .AddAspNetCoreInstrumentation()
        .AddHttpClientInstrumentation()
        .AddOtlpExporter())
    .WithMetrics(metrics => metrics
        .AddAspNetCoreInstrumentation()
        .AddHttpClientInstrumentation()
        .AddOtlpExporter());

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
var persistenceMiddleware = new ConversationPersistenceMiddleware(chatHistoryRepository, persistenceLogger);

AIAgent persistentAgent = chatAgent
    .AsBuilder()
        .Use(runFunc: null, runStreamingFunc: persistenceMiddleware.HandleAsync)
    .Build();

app.MapOpenApi();

// AG-UI endpoint — SSE streaming, session management, protocol events
app.MapAGUI("/", persistentAgent);

// Conversation history + health check endpoints
app.RegisterChatEndpoints();
app.RegisterHealthCheckEndpoints();

app.Run();
