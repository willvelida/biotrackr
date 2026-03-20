using Biotrackr.Chat.Api.Tools;
using FluentAssertions;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Moq;

namespace Biotrackr.Chat.Api.UnitTests.Tools
{
    public class CachingMcpToolWrapperShould
    {
        [Theory]
        [InlineData("GetActivityByDate", "2026-03-20", "GetActivityByDate:2026-03-20")]
        [InlineData("GetSleepByDate", "2025-01-01", "GetSleepByDate:2025-01-01")]
        [InlineData("GetWeightByDate", "2026-12-31", "GetWeightByDate:2026-12-31")]
        [InlineData("GetFoodByDate", "2026-06-15", "GetFoodByDate:2026-06-15")]
        public void DeriveCacheKeyForByDateTools(string toolName, string date, string expectedKey)
        {
            var args = new AIFunctionArguments { ["date"] = date };

            var result = CachingMcpToolWrapper.DeriveCacheKey(toolName, args);

            result.Should().Be(expectedKey);
        }

        [Theory]
        [InlineData("GetActivityByDateRange", "2026-01-01", "2026-01-31", "1", "20", "GetActivityByDateRange:2026-01-01:2026-01-31:1:20")]
        [InlineData("GetSleepByDateRange", "2026-03-01", "2026-03-15", "2", "10", "GetSleepByDateRange:2026-03-01:2026-03-15:2:10")]
        public void DeriveCacheKeyForByDateRangeTools(string toolName, string startDate, string endDate, string pageNumber, string pageSize, string expectedKey)
        {
            var args = new AIFunctionArguments
            {
                ["startDate"] = startDate,
                ["endDate"] = endDate,
                ["pageNumber"] = pageNumber,
                ["pageSize"] = pageSize
            };

            var result = CachingMcpToolWrapper.DeriveCacheKey(toolName, args);

            result.Should().Be(expectedKey);
        }

        [Theory]
        [InlineData("GetActivityByDateRange", "2026-01-01", "2026-01-31", "GetActivityByDateRange:2026-01-01:2026-01-31:1:20")]
        public void DeriveCacheKeyForByDateRangeToolsWithDefaultPagination(string toolName, string startDate, string endDate, string expectedKey)
        {
            var args = new AIFunctionArguments
            {
                ["startDate"] = startDate,
                ["endDate"] = endDate
            };

            var result = CachingMcpToolWrapper.DeriveCacheKey(toolName, args);

            result.Should().Be(expectedKey);
        }

        [Theory]
        [InlineData("GetActivityRecords", "1", "10", "GetActivityRecords:1:10")]
        [InlineData("GetSleepRecords", "3", "50", "GetSleepRecords:3:50")]
        public void DeriveCacheKeyForRecordsTools(string toolName, string pageNumber, string pageSize, string expectedKey)
        {
            var args = new AIFunctionArguments
            {
                ["pageNumber"] = pageNumber,
                ["pageSize"] = pageSize
            };

            var result = CachingMcpToolWrapper.DeriveCacheKey(toolName, args);

            result.Should().Be(expectedKey);
        }

        [Fact]
        public void DetermineTtlForTodayByDateToolReturnsFiveMinutes()
        {
            var today = DateOnly.FromDateTime(DateTime.UtcNow).ToString("yyyy-MM-dd");
            var args = new AIFunctionArguments { ["date"] = today };

            var ttl = CachingMcpToolWrapper.DetermineTtl("GetActivityByDate", args);

            ttl.Should().Be(TimeSpan.FromMinutes(5));
        }

        [Fact]
        public void DetermineTtlForHistoricalByDateToolReturnsOneHour()
        {
            var args = new AIFunctionArguments { ["date"] = "2020-01-01" };

            var ttl = CachingMcpToolWrapper.DetermineTtl("GetActivityByDate", args);

            ttl.Should().Be(TimeSpan.FromHours(1));
        }

        [Theory]
        [InlineData("GetActivityByDateRange")]
        [InlineData("GetSleepByDateRange")]
        [InlineData("GetWeightByDateRange")]
        [InlineData("GetFoodByDateRange")]
        public void DetermineTtlForDateRangeToolReturnsThirtyMinutes(string toolName)
        {
            var args = new AIFunctionArguments
            {
                ["startDate"] = "2026-01-01",
                ["endDate"] = "2026-01-31"
            };

            var ttl = CachingMcpToolWrapper.DetermineTtl(toolName, args);

            ttl.Should().Be(TimeSpan.FromMinutes(30));
        }

        [Theory]
        [InlineData("GetActivityRecords")]
        [InlineData("GetSleepRecords")]
        [InlineData("GetWeightRecords")]
        [InlineData("GetFoodRecords")]
        public void DetermineTtlForRecordsToolReturnsFifteenMinutes(string toolName)
        {
            var args = new AIFunctionArguments
            {
                ["pageNumber"] = "1",
                ["pageSize"] = "10"
            };

            var ttl = CachingMcpToolWrapper.DetermineTtl(toolName, args);

            ttl.Should().Be(TimeSpan.FromMinutes(15));
        }

        [Fact]
        public void WrapReturnsCachedResultOnCacheHit()
        {
            var cache = new MemoryCache(new MemoryCacheOptions());
            var logger = new Mock<ILogger>().Object;
            var innerTool = CreateMockAITool("GetActivityByDate", "test result");

            cache.Set("GetActivityByDate:2026-03-20", "cached result", TimeSpan.FromMinutes(5));

            var wrappedTool = CachingMcpToolWrapper.Wrap(innerTool, cache, logger);

            wrappedTool.Name.Should().Be("GetActivityByDate");
        }

        [Fact]
        public void WrapPreservesToolNameAndDescription()
        {
            var cache = new MemoryCache(new MemoryCacheOptions());
            var logger = new Mock<ILogger>().Object;
            var innerTool = CreateMockAITool("GetSleepByDate", "test", "Gets sleep data for a date");

            var wrappedTool = CachingMcpToolWrapper.Wrap(innerTool, cache, logger);

            wrappedTool.Name.Should().Be("GetSleepByDate");
            wrappedTool.Description.Should().Be("Gets sleep data for a date");
        }

        [Fact]
        public void WrapThrowsOnNullInnerTool()
        {
            var cache = new MemoryCache(new MemoryCacheOptions());
            var logger = new Mock<ILogger>().Object;

            var act = () => CachingMcpToolWrapper.Wrap(null!, cache, logger);

            act.Should().Throw<ArgumentNullException>().WithParameterName("innerTool");
        }

        [Fact]
        public void WrapThrowsOnNullCache()
        {
            var innerTool = CreateMockAITool("test", "result");
            var logger = new Mock<ILogger>().Object;

            var act = () => CachingMcpToolWrapper.Wrap(innerTool, null!, logger);

            act.Should().Throw<ArgumentNullException>().WithParameterName("cache");
        }

        [Fact]
        public void WrapThrowsOnNullLogger()
        {
            var innerTool = CreateMockAITool("test", "result");
            var cache = new MemoryCache(new MemoryCacheOptions());

            var act = () => CachingMcpToolWrapper.Wrap(innerTool, cache, null!);

            act.Should().Throw<ArgumentNullException>().WithParameterName("logger");
        }

        private static AITool CreateMockAITool(string name, string returnValue, string? description = null)
        {
            return AIFunctionFactory.Create(
                method: () => returnValue,
                name: name,
                description: description);
        }
    }
}
