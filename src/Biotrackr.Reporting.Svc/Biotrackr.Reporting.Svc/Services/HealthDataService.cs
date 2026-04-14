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

        var activity = await FetchDomainDataAsync(mcpToolCaller, "GetActivityByDateRange", startDate, endDate, cancellationToken);
        var food = await FetchDomainDataAsync(mcpToolCaller, "GetFoodByDateRange", startDate, endDate, cancellationToken);
        var sleep = await FetchDomainDataAsync(mcpToolCaller, "GetSleepByDateRange", startDate, endDate, cancellationToken);
        var vitals = await FetchDomainDataAsync(mcpToolCaller, "GetVitalsByDateRange", startDate, endDate, cancellationToken);

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
                break;
            }

            using var doc = JsonDocument.Parse(responseText);
            var root = doc.RootElement;

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
        return JsonSerializer.Serialize(aggregated);
    }
}
