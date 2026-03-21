using System.Security.Cryptography;
using System.Text;

namespace Biotrackr.Mcp.Server.Middleware;

/// <summary>
/// Validates an X-Api-Key header on all requests except health check endpoints.
/// When no API key is configured (local dev), all requests are allowed.
/// </summary>
public class ApiKeyAuthMiddleware
{
    private const string ApiKeyHeaderName = "X-Api-Key";
    private readonly RequestDelegate _next;
    private readonly string? _expectedApiKey;
    private readonly ILogger<ApiKeyAuthMiddleware> _logger;

    public ApiKeyAuthMiddleware(
        RequestDelegate next,
        IConfiguration configuration,
        ILogger<ApiKeyAuthMiddleware> logger)
    {
        _next = next;
        _expectedApiKey = configuration.GetValue<string>("mcpserverapikey");
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        // Skip auth for health check endpoints (Container App probes)
        if (context.Request.Path.StartsWithSegments("/api/healthz"))
        {
            await _next(context);
            return;
        }

        // If no API key is configured, allow all requests (local dev / fallback)
        if (string.IsNullOrWhiteSpace(_expectedApiKey))
        {
            await _next(context);
            return;
        }

        if (!context.Request.Headers.TryGetValue(ApiKeyHeaderName, out var providedKey) ||
            !CryptographicOperations.FixedTimeEquals(
                Encoding.UTF8.GetBytes(providedKey.ToString()),
                Encoding.UTF8.GetBytes(_expectedApiKey)))
        {
            _logger.LogWarning("Rejected request to {Path} — missing or invalid API key",
                context.Request.Path);
            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            await context.Response.WriteAsJsonAsync(new { error = "Unauthorized" });
            return;
        }

        await _next(context);
    }
}
