using System.Diagnostics.CodeAnalysis;
using Azure.Identity;
using Biotrackr.UI.Components;
using Radzen;
using Biotrackr.UI.Configuration;
using Biotrackr.UI.Services;
using Microsoft.Extensions.Configuration.AzureAppConfiguration;
using OpenTelemetry;
using OpenTelemetry.Metrics;
using OpenTelemetry.Trace;

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

builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

builder.Services.AddRadzenComponents();

builder.Services.AddOpenTelemetry()
    .WithTracing(b => b.AddSource("Biotrackr.UI")
        .AddAspNetCoreInstrumentation()
        .AddHttpClientInstrumentation(o =>
        {
            o.FilterHttpRequestMessage = _ => true;
            o.EnrichWithHttpRequestMessage = (activity, request) =>
            {
                activity.SetTag("http.request.header.ocp_apim_subscription_key", "[REDACTED]");
            };
        }))
    .WithMetrics(b => b.AddMeter("Biotrackr.UI")
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

builder.Services.AddScoped<IBiotrackrApiService>(provider =>
{
    var httpClient = provider.GetRequiredService<IHttpClientFactory>().CreateClient("BiotrackrApi");
    var logger = provider.GetRequiredService<ILogger<BiotrackrApiService>>();
    return new BiotrackrApiService(httpClient, logger);
});

builder.Services.AddHttpClient("ChatApi", (sp, client) =>
{
    var settings = sp.GetRequiredService<Microsoft.Extensions.Options.IOptions<BiotrackrApiSettings>>().Value;
    var baseUrl = settings.BaseUrl ?? throw new InvalidOperationException("biotrackrapiendpoint is not configured.");
    client.BaseAddress = new Uri($"{baseUrl.TrimEnd('/')}/chat/");
})
.AddHttpMessageHandler<ApiKeyDelegatingHandler>()
.AddStandardResilienceHandler();

builder.Services.AddScoped<IChatApiService>(provider =>
{
    var httpClient = provider.GetRequiredService<IHttpClientFactory>().CreateClient("ChatApi");
    var logger = provider.GetRequiredService<ILogger<ChatApiService>>();
    return new ChatApiService(httpClient, logger);
});

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    app.UseHsts();
}
app.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true);
app.UseHttpsRedirection();

// EasyAuth: redirect unauthenticated users to /login
app.Use(async (context, next) =>
{
    var path = context.Request.Path.Value?.ToLowerInvariant() ?? "";

    var isPublicPath = path == "/login"
        || path.StartsWith("/.auth/")
        || path == "/healthz"
        || path.StartsWith("/_framework/")
        || path.StartsWith("/_blazor")
        || path.StartsWith("/_content/")
        || path.StartsWith("/lib/")
        || path.EndsWith(".css")
        || path.EndsWith(".js")
        || path.EndsWith(".png")
        || path.EndsWith(".ico")
        || path.EndsWith(".map");

    if (!isPublicPath && !context.Request.Headers.ContainsKey("X-MS-CLIENT-PRINCIPAL"))
    {
        context.Response.Redirect("/login");
        return;
    }

    await next();
});

app.UseAntiforgery();

app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.MapGet("/healthz", async (IHttpClientFactory httpClientFactory) =>
{
    try
    {
        var httpClient = httpClientFactory.CreateClient("BiotrackrApi");
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

app.Run();

[ExcludeFromCodeCoverage]
public partial class Program { }
