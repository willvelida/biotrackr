using Microsoft.Extensions.AI;
using Microsoft.Extensions.Caching.Memory;

namespace Biotrackr.Chat.Api.Tools
{
    /// <summary>
    /// Wraps an MCP-provided AITool with response caching.
    /// Cache keys are derived from tool name + arguments.
    /// TTLs match the original per-tool caching strategy.
    /// </summary>
    public static class CachingMcpToolWrapper
    {
        /// <summary>
        /// Wraps an AITool with caching behavior that intercepts invocations,
        /// checks cache, and stores results with tool-specific TTLs.
        /// </summary>
        public static AITool Wrap(AITool innerTool, IMemoryCache cache, ILogger logger)
        {
            ArgumentNullException.ThrowIfNull(innerTool);
            ArgumentNullException.ThrowIfNull(cache);
            ArgumentNullException.ThrowIfNull(logger);

            var innerFunction = (AIFunction)innerTool;

            return AIFunctionFactory.Create(
                method: async (AIFunctionArguments args, CancellationToken cancellationToken) =>
                {
                    var cacheKey = DeriveCacheKey(innerTool.Name, args);

                    if (cache.TryGetValue(cacheKey, out string? cachedResult))
                    {
                        logger.LogDebug("Cache hit for {ToolName} with key {CacheKey}", innerTool.Name, cacheKey);
                        return cachedResult!;
                    }

                    logger.LogDebug("Cache miss for {ToolName} with key {CacheKey}", innerTool.Name, cacheKey);

                    var result = await innerFunction.InvokeAsync(args, cancellationToken);
                    var resultString = result?.ToString() ?? string.Empty;

                    var ttl = DetermineTtl(innerTool.Name, args);
                    cache.Set(cacheKey, resultString, ttl);

                    logger.LogDebug("Cached {ToolName} result with TTL {TtlMinutes}m", innerTool.Name, ttl.TotalMinutes);
                    return resultString;
                },
                name: innerTool.Name,
                description: innerTool.Description);
        }

        /// <summary>
        /// Derives a deterministic cache key from tool name and arguments.
        /// </summary>
        public static string DeriveCacheKey(string toolName, AIFunctionArguments args)
        {
            if (toolName.Contains("ByDate") && !toolName.Contains("ByDateRange"))
            {
                var date = GetArgValue(args, "date");
                return $"{toolName}:{date}";
            }

            if (toolName.Contains("ByDateRange"))
            {
                var startDate = GetArgValue(args, "startDate");
                var endDate = GetArgValue(args, "endDate");
                var pageNumber = GetArgValue(args, "pageNumber", "1");
                var pageSize = GetArgValue(args, "pageSize", "20");
                return $"{toolName}:{startDate}:{endDate}:{pageNumber}:{pageSize}";
            }

            if (toolName.Contains("Records"))
            {
                var pageNumber = GetArgValue(args, "pageNumber", "1");
                var pageSize = GetArgValue(args, "pageSize", "20");
                return $"{toolName}:{pageNumber}:{pageSize}";
            }

            // Fallback: tool name only
            return toolName;
        }

        /// <summary>
        /// Determines the cache TTL based on tool name pattern and arguments.
        /// </summary>
        public static TimeSpan DetermineTtl(string toolName, AIFunctionArguments args)
        {
            if (toolName.Contains("ByDate") && !toolName.Contains("ByDateRange"))
            {
                var date = GetArgValue(args, "date");
                if (DateOnly.TryParse(date, out var parsedDate) && parsedDate == DateOnly.FromDateTime(DateTime.UtcNow))
                {
                    return TimeSpan.FromMinutes(5);
                }

                return TimeSpan.FromHours(1);
            }

            if (toolName.Contains("ByDateRange"))
            {
                return TimeSpan.FromMinutes(30);
            }

            if (toolName.Contains("Records"))
            {
                return TimeSpan.FromMinutes(15);
            }

            // Fallback
            return TimeSpan.FromMinutes(5);
        }

        private static string GetArgValue(AIFunctionArguments args, string key, string defaultValue = "")
        {
            if (args.TryGetValue(key, out var value) && value is not null)
            {
                return value.ToString() ?? defaultValue;
            }

            return defaultValue;
        }
    }
}
