using System.Threading.RateLimiting;
using Azure.Identity;
using Biotrackr.Mcp.Server.Configuration;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Configuration.AzureAppConfiguration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using OpenTelemetry;
using OpenTelemetry.Metrics;
using OpenTelemetry.Trace;

var builder = WebApplication.CreateBuilder(args);

builder.Configuration.AddEnvironmentVariables();
var managedIdentityClientId = builder.Configuration.GetValue<string>("managedidentityclientid");
var azureAppConfigEndpoint = builder.Configuration.GetValue<string>("azureappconfigendpoint");

if (!string.IsNullOrWhiteSpace(azureAppConfigEndpoint))
{
    builder.Configuration.AddAzureAppConfiguration(config =>
    {
        config.Connect(new Uri(azureAppConfigEndpoint),
            new ManagedIdentityCredential(managedIdentityClientId))
        .Select(keyFilter: KeyFilter.Any, LabelFilter.Null);
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

builder.Services.AddOpenTelemetry()
    .WithTracing(b => b.AddSource("Biotrackr.Mcp.Server")
        .AddAspNetCoreInstrumentation()
        .AddHttpClientInstrumentation(o =>
        {
            o.FilterHttpRequestMessage = _ => true;
            o.EnrichWithHttpRequestMessage = (activity, request) =>
            {
                // Ensure subscription key header is never captured in traces
                activity.SetTag("http.request.header.ocp_apim_subscription_key", "[REDACTED]");
            };
        }))
    .WithMetrics(b => b.AddMeter("Biotrackr.Mcp.Server")
        .AddAspNetCoreInstrumentation()
        .AddHttpClientInstrumentation())
    .WithLogging()
    .UseOtlpExporter();

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
    options.AddFixedWindowLimiter("McpRateLimit", limiter =>
    {
        limiter.PermitLimit = 100;
        limiter.Window = TimeSpan.FromMinutes(1);
        limiter.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
        limiter.QueueLimit = 10;
    });
});

var app = builder.Build();

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
}).RequireRateLimiting("McpRateLimit");

app.MapMcp();

app.Run();

public partial class Program { }
