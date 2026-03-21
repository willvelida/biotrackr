using Azure.Identity;
using Azure.Monitor.OpenTelemetry.Exporter;
using Biotrackr.Mcp.Server.Configuration;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Configuration.AzureAppConfiguration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using Biotrackr.Mcp.Server.Middleware;
using System.Diagnostics.CodeAnalysis;
using System.Threading.RateLimiting;

var resourceAttributes = new Dictionary<string, object>
{
    { "service.name", "Biotrackr.Mcp.Server" },
    { "service.version", "1.0.0" }
};
var resourceBuilder = ResourceBuilder.CreateDefault().AddAttributes(resourceAttributes);

var builder = WebApplication.CreateBuilder(args);

builder.Configuration.AddEnvironmentVariables();
var managedIdentityClientId = builder.Configuration.GetValue<string>("managedidentityclientid");
var azureAppConfigEndpoint = builder.Configuration.GetValue<string>("azureappconfigendpoint");

if (!string.IsNullOrWhiteSpace(azureAppConfigEndpoint))
{
    var credential = new ManagedIdentityCredential(managedIdentityClientId);
    builder.Configuration.AddAzureAppConfiguration(config =>
    {
        config.Connect(new Uri(azureAppConfigEndpoint), credential)
        .Select(keyFilter: KeyFilter.Any, LabelFilter.Null)
        .ConfigureKeyVault(kv =>
        {
            kv.SetCredential(credential);
        });
    });
}

builder.Services.AddOptions<BiotrackrApiSettings>()
    .Configure<IConfiguration>((settings, configuration) =>
    {
        settings.BaseUrl = configuration.GetValue<string>("biotrackrapiendpoint");
        settings.SubscriptionKey = configuration.GetValue<string>("biotrackrapisubscriptionkey");
    });

builder.Services
    .AddMcpServer()
    .WithHttpTransport(o => o.Stateless = true)
    .WithToolsFromAssembly();

var appInsightsConnectionString = builder.Configuration["applicationinsightsconnectionstring"];

builder.Services.AddOpenTelemetry()
    .WithTracing(b => b.SetResourceBuilder(resourceBuilder)
        .AddSource("Biotrackr.Mcp.Server")
        .AddAspNetCoreInstrumentation()
        .AddHttpClientInstrumentation(o =>
        {
            o.FilterHttpRequestMessage = _ => true;
            o.EnrichWithHttpRequestMessage = (activity, request) =>
            {
                // Ensure subscription key header is never captured in traces
                activity.SetTag("http.request.header.ocp_apim_subscription_key", "[REDACTED]");
            };
        })
        .AddAzureMonitorTraceExporter(options =>
        {
            options.ConnectionString = appInsightsConnectionString;
        }))
    .WithMetrics(b => b.SetResourceBuilder(resourceBuilder)
        .AddMeter("Biotrackr.Mcp.Server")
        .AddAspNetCoreInstrumentation()
        .AddHttpClientInstrumentation()
        .AddAzureMonitorMetricExporter(options =>
        {
            options.ConnectionString = appInsightsConnectionString;
        }));

builder.Logging.AddOpenTelemetry(log =>
{
    log.SetResourceBuilder(resourceBuilder);
    log.AddAzureMonitorLogExporter(options =>
    {
        options.ConnectionString = appInsightsConnectionString;
    });
});

builder.Services.AddTransient<ApiKeyDelegatingHandler>();

builder.Services.AddHttpClient("BiotrackrApi", (sp, client) =>
{
    var settings = sp.GetRequiredService<Microsoft.Extensions.Options.IOptions<BiotrackrApiSettings>>().Value;
    client.BaseAddress = new Uri(settings.BaseUrl ?? throw new InvalidOperationException("biotrackrapiendpoint is not configured."));
})
.AddHttpMessageHandler<ApiKeyDelegatingHandler>()
.AddStandardResilienceHandler();

builder.Services.AddSingleton<HttpClient>(provider =>
    provider.GetRequiredService<IHttpClientFactory>().CreateClient("BiotrackrApi"));

builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(context =>
        RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            factory: _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 100,
                Window = TimeSpan.FromMinutes(1),
                QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                QueueLimit = 10
            }));
});

var app = builder.Build();

app.UseMiddleware<ApiKeyAuthMiddleware>();
app.UseRateLimiter();

app.MapGet("/api/healthz", async (HttpClient httpClient) =>
{
    try
    {
        var response = await httpClient.GetAsync("/activity?pageNumber=1&pageSize=1");
        return response.IsSuccessStatusCode
            ? Results.Ok(new { status = "Healthy", downstream = "Reachable" })
            : Results.Ok(new { status = "Degraded", downstream = $"Returned {response.StatusCode}" });
    }
    catch (Exception ex)
    {
        return Results.Ok(new { status = "Degraded", downstream = $"Unreachable: {ex.Message}" });
    }
});

app.MapMcp();

app.Run();

[ExcludeFromCodeCoverage]
public partial class Program { }
