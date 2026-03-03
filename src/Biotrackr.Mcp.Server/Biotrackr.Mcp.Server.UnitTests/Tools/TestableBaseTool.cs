using Biotrackr.Mcp.Server.Tools;
using Microsoft.Extensions.Logging;

namespace Biotrackr.Mcp.Server.UnitTests.Tools
{
    /// <summary>
    /// Concrete subclass of BaseTool for testing protected/static members.
    /// </summary>
    public class TestableBaseTool : BaseTool
    {
        public TestableBaseTool(HttpClient httpClient, ILogger logger) : base(httpClient, logger)
        {
        }

        public new Task<string> GetAsync<T>(string endpoint, string operationName) where T : class, new()
            => base.GetAsync<T>(endpoint, operationName);

        public static new bool IsValidDate(string date)
            => BaseTool.IsValidDate(date);

        public static new bool IsValidDateRange(string startDate, string endDate)
            => BaseTool.IsValidDateRange(startDate, endDate);

        public static new string BuildPaginatedEndpoint(string basePath, int pageNumber, int pageSize)
            => BaseTool.BuildPaginatedEndpoint(basePath, pageNumber, pageSize);
    }
}
