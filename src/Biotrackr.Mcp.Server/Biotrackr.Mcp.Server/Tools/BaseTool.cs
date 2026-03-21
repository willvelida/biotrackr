using Microsoft.Extensions.Logging;
using System.Diagnostics;
using System.Diagnostics.Metrics;
using System.Globalization;
using System.Net.Http.Json;
using System.Text.Json;

namespace Biotrackr.Mcp.Server.Tools
{
    public abstract class BaseTool
    {
        internal const string ServiceName = "Biotrackr.Mcp.Server";

        private static readonly ActivitySource _activitySource = new(ServiceName);
        private static readonly Meter _meter = new(ServiceName);
        private static readonly Counter<long> _invocationCounter = _meter.CreateCounter<long>("mcp.tool.invocations", description: "Number of MCP tool invocations");
        private static readonly Counter<long> _errorCounter = _meter.CreateCounter<long>("mcp.tool.errors", description: "Number of MCP tool errors");
        private static readonly Histogram<double> _latencyHistogram = _meter.CreateHistogram<double>("mcp.tool.duration", unit: "ms", description: "MCP tool invocation duration in milliseconds");

        protected readonly HttpClient _httpClient;
        protected readonly ILogger _logger;

        private static readonly JsonSerializerOptions JsonOptions = new()
        {
            PropertyNameCaseInsensitive = true
        };

        protected BaseTool(
            HttpClient httpClient,
            ILogger logger)
        {
            _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        protected async Task<string> GetAsync<T>(
            string endpoint,
            string operationName) where T : class, new()
        {
            using var activity = _activitySource.StartActivity(operationName);
            activity?.SetTag("mcp.tool.operation", operationName);
            activity?.SetTag("mcp.tool.endpoint", endpoint);

            var stopwatch = Stopwatch.StartNew();
            var success = false;

            _logger.LogInformation("Fetching data for {OperationName} - Endpoint: {Endpoint}", operationName, endpoint);
            _invocationCounter.Add(1, new KeyValuePair<string, object?>("operation", operationName));

            try
            {
                var response = await _httpClient.GetAsync(endpoint);

                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogError("API call failed for {OperationName} - Status Code: {StatusCode}", operationName, response.StatusCode);
                    _errorCounter.Add(1, new KeyValuePair<string, object?>("operation", operationName), new KeyValuePair<string, object?>("reason", $"http_{(int)response.StatusCode}"));
                    activity?.SetTag("mcp.tool.error", true);
                    activity?.SetTag("mcp.tool.status_code", (int)response.StatusCode);
                    return JsonSerializer.Serialize(new { error = $"API call failed with status code {response.StatusCode}" });
                }

                var content = await response.Content.ReadAsStringAsync();

                var result = JsonSerializer.Deserialize<T>(content, JsonOptions);

                if (result == null)
                {
                    _logger.LogError("Failed to deserialize API response for {OperationName}", operationName);
                    _errorCounter.Add(1, new KeyValuePair<string, object?>("operation", operationName), new KeyValuePair<string, object?>("reason", "deserialization_null"));
                    activity?.SetTag("mcp.tool.error", true);
                    return JsonSerializer.Serialize(new { error = "Failed to deserialize API response" });
                }

                success = true;
                _logger.LogInformation("API response for {OperationName} deserialized successfully. Duration: {DurationMs}ms", operationName, stopwatch.ElapsedMilliseconds);
                return JsonSerializer.Serialize(result, JsonOptions);
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError(ex, "Network error for {OperationName}", operationName);
                _errorCounter.Add(1, new KeyValuePair<string, object?>("operation", operationName), new KeyValuePair<string, object?>("reason", "network"));
                activity?.SetTag("mcp.tool.error", true);
                return JsonSerializer.Serialize(new { error = $"Network error: {ex.Message}" });
            }
            catch (TaskCanceledException ex)
            {
                _logger.LogError(ex, "Timeout for {OperationName}", operationName);
                _errorCounter.Add(1, new KeyValuePair<string, object?>("operation", operationName), new KeyValuePair<string, object?>("reason", "timeout"));
                activity?.SetTag("mcp.tool.error", true);
                return JsonSerializer.Serialize(new { error = $"Request timed out: {ex.Message}" });
            }
            catch (JsonException ex)
            {
                _logger.LogError(ex, "JSON parsing error for {OperationName}", operationName);
                _errorCounter.Add(1, new KeyValuePair<string, object?>("operation", operationName), new KeyValuePair<string, object?>("reason", "json_parse"));
                activity?.SetTag("mcp.tool.error", true);
                return JsonSerializer.Serialize(new { error = $"JSON parsing error: {ex.Message}" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error for {OperationName}", operationName);
                _errorCounter.Add(1, new KeyValuePair<string, object?>("operation", operationName), new KeyValuePair<string, object?>("reason", "unexpected"));
                activity?.SetTag("mcp.tool.error", true);
                return JsonSerializer.Serialize(new { error = $"Unexpected error: {ex.Message}" });
            }
            finally
            {
                stopwatch.Stop();
                _latencyHistogram.Record(stopwatch.Elapsed.TotalMilliseconds, new KeyValuePair<string, object?>("operation", operationName), new KeyValuePair<string, object?>("success", success));
                activity?.SetTag("mcp.tool.duration_ms", stopwatch.Elapsed.TotalMilliseconds);
                activity?.SetTag("mcp.tool.success", success);
                _logger.LogDebug("Tool {OperationName} completed in {DurationMs}ms - Success: {Success}", operationName, stopwatch.Elapsed.TotalMilliseconds, success);
            }
        }

        protected static bool IsValidDate(string date)
        {
            return DateTime.TryParseExact(date, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out _);
        }

        protected static bool IsValidDateRange(string startDate, string endDate)
        {
            if (!DateTime.TryParseExact(startDate, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var start))
                return false;
            if (!DateTime.TryParseExact(endDate, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var end))
                return false;
            return start <= end;
        }

        protected static string BuildPaginatedEndpoint(string basePath, int pageNumber, int pageSize)
        {
            pageNumber = Math.Max(1, pageNumber);
            pageSize = Math.Clamp(pageSize, 1, 50);
            return $"{basePath}?pageNumber={pageNumber}&pageSize={pageSize}";
        }
    }
}
