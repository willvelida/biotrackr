using System.ComponentModel;
using System.Text.Json;
using Biotrackr.Chat.Api.Models;
using Biotrackr.Chat.Api.Models.Weight;
using Microsoft.Extensions.Caching.Memory;

namespace Biotrackr.Chat.Api.Tools
{
    public class WeightTools(IHttpClientFactory httpClientFactory, IMemoryCache cache)
    {
        private static readonly JsonSerializerOptions JsonOptions = new()
        {
            PropertyNameCaseInsensitive = true
        };

        private static string SanitizeResponse<T>(string rawJson, string entityName) where T : class, new()
        {
            var typed = JsonSerializer.Deserialize<T>(rawJson, JsonOptions);
            if (typed is null)
                return JsonSerializer.Serialize(new { error = $"Failed to parse {entityName} data." });

            return JsonSerializer.Serialize(typed, JsonOptions);
        }

        [Description("Get weight data (weight, BMI, body fat percentage) for a specific date. Date format: YYYY-MM-DD.")]
        public async Task<string> GetWeightByDate(
            [Description("The date to get weight data for, in YYYY-MM-DD format")] string date)
        {
            if (!DateOnly.TryParse(date, out _))
                return """{"error": "Invalid date format. Use YYYY-MM-DD."}""";

            var cacheKey = $"weight:{date}";
            if (cache.TryGetValue(cacheKey, out string? cached))
                return cached!;

            var client = httpClientFactory.CreateClient("BiotrackrApi");
            var response = await client.GetAsync($"/weight/{date}");

            if (!response.IsSuccessStatusCode)
                return $"{{\"error\": \"Weight data not found for {date}.\"}}";

            var result = await response.Content.ReadAsStringAsync();
            var sanitized = SanitizeResponse<WeightItem>(result, "weight");

            var ttl = DateOnly.Parse(date) == DateOnly.FromDateTime(DateTime.UtcNow)
                ? TimeSpan.FromMinutes(5)
                : TimeSpan.FromHours(1);
            cache.Set(cacheKey, sanitized, ttl);

            return sanitized;
        }

        [Description("Get weight data for a date range. Maximum 365 days. Date format: YYYY-MM-DD.")]
        public async Task<string> GetWeightByDateRange(
            [Description("The start date, in YYYY-MM-DD format")] string startDate,
            [Description("The end date, in YYYY-MM-DD format")] string endDate)
        {
            if (!DateOnly.TryParse(startDate, out var start) || !DateOnly.TryParse(endDate, out var end))
                return """{"error": "Invalid date format. Use YYYY-MM-DD."}""";

            if ((end.ToDateTime(TimeOnly.MinValue) - start.ToDateTime(TimeOnly.MinValue)).Days > 365)
                return """{"error": "Date range cannot exceed 365 days."}""";

            var cacheKey = $"weight-range:{startDate}:{endDate}";
            if (cache.TryGetValue(cacheKey, out string? cached))
                return cached!;

            var client = httpClientFactory.CreateClient("BiotrackrApi");
            var response = await client.GetAsync($"/weight/range/{startDate}/{endDate}");

            if (!response.IsSuccessStatusCode)
                return """{"error": "No weight data found for the specified range."}""";

            var result = await response.Content.ReadAsStringAsync();
            var sanitized = SanitizeResponse<PaginatedResponse<WeightItem>>(result, "weight range");
            cache.Set(cacheKey, sanitized, TimeSpan.FromMinutes(30));
            return sanitized;
        }

        [Description("Get paginated weight records. Returns the most recent records by default.")]
        public async Task<string> GetWeightRecords(
            [Description("Page number (default: 1)")] int pageNumber = 1,
            [Description("Page size (default: 10, max: 50)")] int pageSize = 10)
        {
            pageSize = Math.Min(pageSize, 50);
            var cacheKey = $"weight-records:{pageNumber}:{pageSize}";
            if (cache.TryGetValue(cacheKey, out string? cached))
                return cached!;

            var client = httpClientFactory.CreateClient("BiotrackrApi");
            var response = await client.GetAsync($"/weight?pageNumber={pageNumber}&pageSize={pageSize}");

            if (!response.IsSuccessStatusCode)
                return """{"error": "Failed to retrieve weight records."}""";

            var result = await response.Content.ReadAsStringAsync();
            var sanitized = SanitizeResponse<PaginatedResponse<WeightItem>>(result, "weight records");
            cache.Set(cacheKey, sanitized, TimeSpan.FromMinutes(15));
            return sanitized;
        }
    }
}
