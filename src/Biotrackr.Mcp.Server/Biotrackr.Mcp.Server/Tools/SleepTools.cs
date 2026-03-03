using Biotrackr.Mcp.Server.Models;
using Biotrackr.Mcp.Server.Models.Sleep;
using Microsoft.Extensions.Logging;
using ModelContextProtocol.Server;
using System.ComponentModel;
using System.Text.Json;

namespace Biotrackr.Mcp.Server.Tools
{
    [McpServerToolType]
    public class SleepTools : BaseTool
    {
        public SleepTools(HttpClient httpClient, ILogger<SleepTools> logger) : base(httpClient, logger)
        {
        }

        [McpServerTool, Description("Gets Sleep Records between two specified dates. Dates must be in yyyy-MM-dd format. Supports pagination.")]
        public async Task<string> GetSleepByDateRange(
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

            var endpoint = BuildPaginatedEndpoint($"/sleep/range/{startDate}/{endDate}", pageNumber, pageSize);
            return await GetAsync<PaginatedResponse<SleepItem>>(endpoint, "GetSleepByDateRange");
        }

        [McpServerTool, Description("Gets a Sleep Record for a specified date. Date must be in yyyy-MM-dd format.")]
        public async Task<string> GetSleepByDate(
            [Description("Date in yyyy-MM-dd format")] string date)
        {
            if (!IsValidDate(date))
                return JsonSerializer.Serialize(new { error = "Invalid date format. Use yyyy-MM-dd." });

            var endpoint = $"/sleep/{date}";
            return await GetAsync<SleepItem>(endpoint, "GetSleepByDate");
        }

        [McpServerTool, Description("Retrieves paginated Sleep Records.")]
        public async Task<string> GetSleepRecords(
            [Description("Page number (default: 1)")] int pageNumber = 1,
            [Description("Page size between 1-100 (default: 20)")] int pageSize = 20)
        {
            var endpoint = BuildPaginatedEndpoint("/sleep", pageNumber, pageSize);
            return await GetAsync<PaginatedResponse<SleepItem>>(endpoint, "GetSleepRecords");
        }
    }
}
