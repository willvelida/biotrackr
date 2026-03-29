using Azure.Identity;
using Azure.Monitor.OpenTelemetry.Exporter;
using Biotrackr.Reporting.Api.Configuration;
using Biotrackr.Reporting.Api.Endpoints;
using Biotrackr.Reporting.Api.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Agents.AI.Hosting;
using Microsoft.Extensions.Configuration.AzureAppConfiguration;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

var resourceAttributes = new Dictionary<string, object>
{
    { "service.name", "Biotrackr.Reporting.Api" },
    { "service.version", "1.0.0" }
};
var resourceBuilder = ResourceBuilder.CreateDefault().AddAttributes(resourceAttributes);

var builder = WebApplication.CreateBuilder(args);

builder.Configuration.AddEnvironmentVariables();
var managedIdentityClientId = builder.Configuration.GetValue<string>("managedidentityclientid");
var azureAppConfigEndpoint = builder.Configuration.GetValue<string>("azureappconfigendpoint");

// Azure App Configuration
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

// Re-add env vars so they override App Configuration values (e.g. AzureAd:ClientId)
builder.Configuration.AddEnvironmentVariables();

builder.Services.Configure<Settings>(builder.Configuration.GetSection("Biotrackr"));

// Authentication — validate incoming JWT Bearer tokens (ASI03 — defense-in-depth)
var azureAdClientId = builder.Configuration.GetValue<string>("AzureAd:ClientId") ?? string.Empty;
var azureAdTenantId = builder.Configuration.GetValue<string>("AzureAd:TenantId") ?? string.Empty;
var azureAdInstance = builder.Configuration.GetValue<string>("AzureAd:Instance") ?? "https://login.microsoftonline.com/";

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.Authority = $"{azureAdInstance.TrimEnd('/')}/{azureAdTenantId}/v2.0";
        options.Audience = azureAdClientId;
        options.TokenValidationParameters = new Microsoft.IdentityModel.Tokens.TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidAudiences = new[] { azureAdClientId, $"api://{azureAdClientId}" },
        };
    });

// Authorization policy: restrict to Chat.Api's agent identity (ASI07 — mutual A2A auth)
var chatApiAgentIdentityId = builder.Configuration.GetValue<string>("Biotrackr:ChatApiAgentIdentityId") ?? string.Empty;
builder.Services.AddAuthorizationBuilder()
    .AddPolicy("ChatApiAgent", policy =>
    {
        policy.RequireAuthenticatedUser();
        if (!string.IsNullOrWhiteSpace(chatApiAgentIdentityId))
        {
            policy.RequireClaim("azp", chatApiAgentIdentityId);
        }
    });

// OpenTelemetry
var appInsightsConnectionString = builder.Configuration.GetValue<string>("applicationinsightsconnectionstring");
builder.Services.AddOpenTelemetry()
    .WithTracing(tracing =>
    {
        tracing.SetResourceBuilder(resourceBuilder)
            .AddAspNetCoreInstrumentation()
            .AddHttpClientInstrumentation();

        if (!string.IsNullOrWhiteSpace(appInsightsConnectionString))
        {
            tracing.AddAzureMonitorTraceExporter(options =>
            {
                options.ConnectionString = appInsightsConnectionString;
            });
        }
    })
    .WithMetrics(metrics =>
    {
        metrics.SetResourceBuilder(resourceBuilder)
            .AddAspNetCoreInstrumentation()
            .AddHttpClientInstrumentation();

        if (!string.IsNullOrWhiteSpace(appInsightsConnectionString))
        {
            metrics.AddAzureMonitorMetricExporter(options =>
            {
                options.ConnectionString = appInsightsConnectionString;
            });
        }
    });

// Health checks
builder.Services.AddHealthChecks();

// Services
builder.Services.AddSingleton<IBlobStorageService, BlobStorageService>();
builder.Services.AddSingleton<ICopilotService, CopilotService>();
builder.Services.AddSingleton<IReportGenerationService, ReportGenerationService>();

var app = builder.Build();

// Authentication & authorization middleware (ASI03)
app.UseAuthentication();
app.UseAuthorization();

// Health check endpoint (no auth — used by Container Apps probes)
app.MapGet("/api/healthz", () => Results.Ok(new { status = "healthy" })).AllowAnonymous();

// Report generation endpoint (202 async pattern)
app.MapGenerateEndpoints();

// Report retrieval endpoints
app.MapReportEndpoints();

app.Run();

[System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
public partial class Program { }
