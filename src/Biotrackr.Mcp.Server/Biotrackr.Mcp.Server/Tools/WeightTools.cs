using Biotrackr.Mcp.Server.Models;
using Biotrackr.Mcp.Server.Models.Weight;
using Microsoft.Extensions.Logging;
using ModelContextProtocol.Server;
using System.ComponentModel;
using System.Text.Json;

namespace Biotrackr.Mcp.Server.Tools
{
    [McpServerToolType]
    public class WeightTools : BaseTool
    {
        public WeightTools(HttpClient httpClient, ILogger<WeightTools> logger) : base(httpClient, logger)
        {
        }

        [McpServerTool, Description("Gets Weight Records between two specified dates. Dates must be in yyyy-MM-dd format. Supports pagination.")]
        public async Task<string> GetWeightByDateRange(
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

            var endpoint = BuildPaginatedEndpoint($"/weight/range/{startDate}/{endDate}", pageNumber, pageSize);
            return await GetAsync<PaginatedResponse<WeightItem>>(endpoint, "GetWeightByDateRange");
        }

        [McpServerTool, Description("Gets a Weight Record for a specified date. Date must be in yyyy-MM-dd format.")]
        public async Task<string> GetWeightByDate(
            [Description("Date in yyyy-MM-dd format")] string date)
        {
            if (!IsValidDate(date))
                return JsonSerializer.Serialize(new { error = "Invalid date format. Use yyyy-MM-dd." });

            var endpoint = $"/weight/{date}";
            return await GetAsync<WeightItem>(endpoint, "GetWeightByDate");
        }

        [McpServerTool, Description("Retrieves paginated Weight Records.")]
        public async Task<string> GetWeightRecords(
            [Description("Page number (default: 1)")] int pageNumber = 1,
            [Description("Page size between 1-100 (default: 20)")] int pageSize = 20)
        {
            var endpoint = BuildPaginatedEndpoint("/weight", pageNumber, pageSize);
            return await GetAsync<PaginatedResponse<WeightItem>>(endpoint, "GetWeightRecords");
        }
    }
}
