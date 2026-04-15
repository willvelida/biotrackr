using System.Text.Json;
using Biotrackr.Reporting.Svc.Models;
using Biotrackr.Reporting.Svc.Services.Interfaces;

namespace Biotrackr.Reporting.Svc.Services;

public class HealthDataService : IHealthDataService
{
    private readonly IMcpClientFactory _mcpClientFactory;
    private readonly ILogger<HealthDataService> _logger;

    public HealthDataService(
        IMcpClientFactory mcpClientFactory,
        ILogger<HealthDataService> logger)
    {
        _mcpClientFactory = mcpClientFactory;
        _logger = logger;
    }

    public async Task<HealthDataSnapshot> FetchHealthDataAsync(string startDate, string endDate, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Fetching health data from MCP Server for {StartDate} to {EndDate}", startDate, endDate);

        await using var mcpToolCaller = await _mcpClientFactory.CreateClientAsync(cancellationToken);

        var activity = await FetchDomainDataAsync(mcpToolCaller, "get_activity_by_date_range", startDate, endDate, cancellationToken);
        var food = await FetchDomainDataAsync(mcpToolCaller, "get_food_by_date_range", startDate, endDate, cancellationToken);
        var sleep = await FetchDomainDataAsync(mcpToolCaller, "get_sleep_by_date_range", startDate, endDate, cancellationToken);
        var vitals = await FetchDomainDataAsync(mcpToolCaller, "get_vitals_by_date_range", startDate, endDate, cancellationToken);

        _logger.LogInformation("Health data fetched successfully for all 4 domains");

        return new HealthDataSnapshot
        {
            Activity = activity,
            Food = food,
            Sleep = sleep,
            Vitals = vitals
        };
    }

    private async Task<string> FetchDomainDataAsync(IMcpToolCaller mcpToolCaller, string toolName, string startDate, string endDate, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Calling MCP tool {ToolName} for {StartDate} to {EndDate}", toolName, startDate, endDate);

        const int maxRetries = 1;
        for (var attempt = 0; attempt <= maxRetries; attempt++)
        {
            var result = await FetchDomainDataCoreAsync(mcpToolCaller, toolName, startDate, endDate, cancellationToken);
            if (result.success || attempt == maxRetries)
            {
                return result.data;
            }

            _logger.LogWarning("Retrying MCP tool {ToolName} after failure (attempt {Attempt})", toolName, attempt + 1);
            await Task.Delay(TimeSpan.FromSeconds(5), cancellationToken);
        }

        // Unreachable, but satisfies compiler
        return SerializeEmpty();
    }

    private async Task<(bool success, string data)> FetchDomainDataCoreAsync(IMcpToolCaller mcpToolCaller, string toolName, string startDate, string endDate, CancellationToken cancellationToken)
    {
        var allItems = new List<JsonElement>();
        var pageNumber = 1;
        const int pageSize = 50;
        var hasNextPage = true;

        while (hasNextPage)
        {
            var responseText = await mcpToolCaller.CallToolAsync(
                toolName,
                new Dictionary<string, object?>
                {
                    ["startDate"] = startDate,
                    ["endDate"] = endDate,
                    ["pageNumber"] = pageNumber,
                    ["pageSize"] = pageSize
                },
                cancellationToken);

            if (string.IsNullOrEmpty(responseText))
            {
                _logger.LogWarning("MCP tool {ToolName} returned null/empty response on page {Page}", toolName, pageNumber);
                break;
            }

            using var doc = JsonDocument.Parse(responseText);
            var root = doc.RootElement;

            // Detect error responses from the MCP Server (e.g., upstream API returned 401/404/500)
            if (root.TryGetProperty("error", out var error))
            {
                _logger.LogError("MCP tool {ToolName} returned error on page {Page}: {Error}", toolName, pageNumber, error.GetString());
                return (false, SerializeEmpty());
            }

            if (root.TryGetProperty("items", out var items) && items.ValueKind == JsonValueKind.Array)
            {
                foreach (var item in items.EnumerateArray())
                {
                    allItems.Add(item.Clone());
                }
            }

            hasNextPage = root.TryGetProperty("hasNextPage", out var nextPage) && nextPage.GetBoolean();
            pageNumber++;
        }

        _logger.LogInformation("MCP tool {ToolName} returned {Count} items across {Pages} pages", toolName, allItems.Count, pageNumber - 1);

        var aggregated = new { items = allItems, totalCount = allItems.Count };
        return (allItems.Count > 0, JsonSerializer.Serialize(aggregated));
    }

    private static string SerializeEmpty()
    {
        return JsonSerializer.Serialize(new { items = Array.Empty<object>(), totalCount = 0 });
    }
}
