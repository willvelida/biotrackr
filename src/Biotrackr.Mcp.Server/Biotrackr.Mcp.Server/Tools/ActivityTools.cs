using Biotrackr.Mcp.Server.Models;
using Biotrackr.Mcp.Server.Models.Activity;
using Microsoft.Extensions.Logging;
using ModelContextProtocol.Server;
using System.ComponentModel;
using System.Text.Json;

namespace Biotrackr.Mcp.Server.Tools
{
    [McpServerToolType]
    public class ActivityTools : BaseTool
    {
        public ActivityTools(HttpClient httpClient, ILogger<ActivityTools> logger) : base(httpClient, logger)
        {
        }

        [McpServerTool, Description("Gets Activity Records between two specified dates. Dates must be in yyyy-MM-dd format. Supports pagination.")]
        public async Task<string> GetActivityByDateRange(
            [Description("Start date in yyyy-MM-dd format")] string startDate,
            [Description("End date in yyyy-MM-dd format")] string endDate,
            [Description("Page number (default: 1)")] int pageNumber = 1,
            [Description("Page size between 1-100 (default: 20)")] int pageSize = 20)
        {
            if (!IsValidDate(startDate))
                return JsonSerializer.Serialize(new { error = "Invalid startDate format. Use yyyy-MM-dd." });

            if (!IsValidDate(endDate))
                return JsonSerializer.Serialize(new { error = "Invalid endDate format. Use yyyy-MM-dd." });

            if (!IsValidDateRange(startDate, endDate))
                return JsonSerializer.Serialize(new { error = "startDate must be on or before endDate." });

            var endpoint = BuildPaginatedEndpoint($"/activity/range/{startDate}/{endDate}", pageNumber, pageSize);
            return await GetAsync<PaginatedResponse<ActivityItem>>(endpoint, "GetActivityByDateRange");
        }

        [McpServerTool, Description("Gets an Activity Record for a specified date. Date must be in yyyy-MM-dd format.")]
        public async Task<string> GetActivityByDate(
            [Description("Date in yyyy-MM-dd format")] string date)
        {
            if (!IsValidDate(date))
                return JsonSerializer.Serialize(new { error = "Invalid date format. Use yyyy-MM-dd." });

            var endpoint = $"/activity/{date}";
            return await GetAsync<ActivityItem>(endpoint, "GetActivityByDate");
        }

        [McpServerTool, Description("Retrieves paginated Activity Records.")]
        public async Task<string> GetActivityRecords(
            [Description("Page number (default: 1)")] int pageNumber = 1,
            [Description("Page size between 1-100 (default: 20)")] int pageSize = 20)
        {
            var endpoint = BuildPaginatedEndpoint("/activity", pageNumber, pageSize);
            return await GetAsync<PaginatedResponse<ActivityItem>>(endpoint, "GetActivityRecords");
        }
    }
}
