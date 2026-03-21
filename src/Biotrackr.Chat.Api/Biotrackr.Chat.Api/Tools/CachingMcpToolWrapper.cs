using Microsoft.Extensions.AI;
using Microsoft.Extensions.Caching.Memory;

namespace Biotrackr.Chat.Api.Tools
{
    /// <summary>
    /// Wraps an MCP-provided AITool with response caching.
    /// Uses DelegatingAIFunction to preserve the original tool's parameter schema (Name, Description, JsonSchema).
    /// Cache keys are derived from tool name + arguments.
    /// TTLs match the original per-tool caching strategy.
    /// </summary>
    public static class CachingMcpToolWrapper
    {
        /// <summary>
        /// Wraps an AITool with caching behavior that intercepts invocations,
        /// checks cache, and stores results with tool-specific TTLs.
        /// Preserves the original tool's parameter metadata via DelegatingAIFunction.
        /// </summary>
        public static AITool Wrap(AITool innerTool, IMemoryCache cache, ILogger logger)
        {
            ArgumentNullException.ThrowIfNull(innerTool);
            ArgumentNullException.ThrowIfNull(cache);
            ArgumentNullException.ThrowIfNull(logger);

            return new CachingDelegatingFunction((AIFunction)innerTool, cache, logger);
        }

        /// <summary>
        /// Derives a deterministic cache key from tool name and arguments.
        /// </summary>
        public static string DeriveCacheKey(string toolName, AIFunctionArguments args)
        {
            if (toolName.Contains("by_date") && !toolName.Contains("by_date_range"))
            {
                var date = GetArgValue(args, "date");
                return $"{toolName}:{date}";
            }

            if (toolName.Contains("by_date_range"))
            {
                var startDate = GetArgValue(args, "startDate", GetArgValue(args, "start_date"));
                var endDate = GetArgValue(args, "endDate", GetArgValue(args, "end_date"));
                var pageNumber = GetArgValue(args, "pageNumber", GetArgValue(args, "page_number", "1"));
                var pageSize = GetArgValue(args, "pageSize", GetArgValue(args, "page_size", "20"));
                return $"{toolName}:{startDate}:{endDate}:{pageNumber}:{pageSize}";
            }

            if (toolName.Contains("records"))
            {
                var pageNumber = GetArgValue(args, "pageNumber", GetArgValue(args, "page_number", "1"));
                var pageSize = GetArgValue(args, "pageSize", GetArgValue(args, "page_size", "20"));
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
            if (toolName.Contains("by_date") && !toolName.Contains("by_date_range"))
            {
                var date = GetArgValue(args, "date");
                if (DateOnly.TryParse(date, out var parsedDate) && parsedDate == DateOnly.FromDateTime(DateTime.UtcNow))
                {
                    return TimeSpan.FromMinutes(5);
                }

                return TimeSpan.FromHours(1);
            }

            if (toolName.Contains("by_date_range"))
            {
                return TimeSpan.FromMinutes(30);
            }

            if (toolName.Contains("records"))
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

    /// <summary>
    /// A DelegatingAIFunction that adds caching around the inner MCP tool function.
    /// Preserves the original tool's Name, Description, JsonSchema, and parameter metadata.
    /// </summary>
    internal sealed class CachingDelegatingFunction(AIFunction innerFunction, IMemoryCache cache, ILogger logger)
        : DelegatingAIFunction(innerFunction)
    {
        protected override async ValueTask<object?> InvokeCoreAsync(
            AIFunctionArguments arguments,
            CancellationToken cancellationToken)
        {
            var cacheKey = CachingMcpToolWrapper.DeriveCacheKey(Name, arguments);

            if (cache.TryGetValue(cacheKey, out string? cachedResult))
            {
                logger.LogDebug("Cache hit for {ToolName} with key {CacheKey}", Name, cacheKey);
                return cachedResult!;
            }

            logger.LogDebug("Cache miss for {ToolName} with key {CacheKey}", Name, cacheKey);

            var result = await base.InvokeCoreAsync(arguments, cancellationToken);
            var resultString = result?.ToString() ?? string.Empty;

            var ttl = CachingMcpToolWrapper.DetermineTtl(Name, arguments);
            cache.Set(cacheKey, resultString, ttl);

            logger.LogDebug("Cached {ToolName} result with TTL {TtlMinutes}m", Name, ttl.TotalMinutes);
            return resultString;
        }
    }
}
