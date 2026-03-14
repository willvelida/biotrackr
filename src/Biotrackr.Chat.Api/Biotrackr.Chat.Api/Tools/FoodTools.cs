using System.ComponentModel;
using System.Text.Json;
using Biotrackr.Chat.Api.Models;
using Biotrackr.Chat.Api.Models.Food;
using Microsoft.Extensions.Caching.Memory;

namespace Biotrackr.Chat.Api.Tools
{
    public class FoodTools(IHttpClientFactory httpClientFactory, IMemoryCache cache)
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

        [Description("Get food data (calories, macronutrients, meals) for a specific date. Date format: YYYY-MM-DD.")]
        public async Task<string> GetFoodByDate(
            [Description("The date to get food data for, in YYYY-MM-DD format")] string date)
        {
            if (!DateOnly.TryParse(date, out _))
                return """{"error": "Invalid date format. Use YYYY-MM-DD."}""";

            var cacheKey = $"food:{date}";
            if (cache.TryGetValue(cacheKey, out string? cached))
                return cached!;

            var client = httpClientFactory.CreateClient("BiotrackrApi");
            var response = await client.GetAsync($"/food/{date}");

            if (!response.IsSuccessStatusCode)
                return $"{{\"error\": \"Food data not found for {date}.\"}}";

            var result = await response.Content.ReadAsStringAsync();
            var sanitized = SanitizeResponse<FoodItem>(result, "food");

            var ttl = DateOnly.Parse(date) == DateOnly.FromDateTime(DateTime.UtcNow)
                ? TimeSpan.FromMinutes(5)
                : TimeSpan.FromHours(1);
            cache.Set(cacheKey, sanitized, ttl);

            return sanitized;
        }

        [Description("Get food data for a date range. Maximum 365 days. Date format: YYYY-MM-DD.")]
        public async Task<string> GetFoodByDateRange(
            [Description("The start date, in YYYY-MM-DD format")] string startDate,
            [Description("The end date, in YYYY-MM-DD format")] string endDate)
        {
            if (!DateOnly.TryParse(startDate, out var start) || !DateOnly.TryParse(endDate, out var end))
                return """{"error": "Invalid date format. Use YYYY-MM-DD."}""";

            if ((end.ToDateTime(TimeOnly.MinValue) - start.ToDateTime(TimeOnly.MinValue)).Days > 365)
                return """{"error": "Date range cannot exceed 365 days."}""";

            var cacheKey = $"food-range:{startDate}:{endDate}";
            if (cache.TryGetValue(cacheKey, out string? cached))
                return cached!;

            var client = httpClientFactory.CreateClient("BiotrackrApi");
            var response = await client.GetAsync($"/food/range/{startDate}/{endDate}");

            if (!response.IsSuccessStatusCode)
                return """{"error": "No food data found for the specified range."}""";

            var result = await response.Content.ReadAsStringAsync();
            var sanitized = SanitizeResponse<PaginatedResponse<FoodItem>>(result, "food range");
            cache.Set(cacheKey, sanitized, TimeSpan.FromMinutes(30));
            return sanitized;
        }

        [Description("Get paginated food records. Returns the most recent records by default.")]
        public async Task<string> GetFoodRecords(
            [Description("Page number (default: 1)")] int pageNumber = 1,
            [Description("Page size (default: 10, max: 50)")] int pageSize = 10)
        {
            pageSize = Math.Min(pageSize, 50);
            var cacheKey = $"food-records:{pageNumber}:{pageSize}";
            if (cache.TryGetValue(cacheKey, out string? cached))
                return cached!;

            var client = httpClientFactory.CreateClient("BiotrackrApi");
            var response = await client.GetAsync($"/food?pageNumber={pageNumber}&pageSize={pageSize}");

            if (!response.IsSuccessStatusCode)
                return """{"error": "Failed to retrieve food records."}""";

            var result = await response.Content.ReadAsStringAsync();
            var sanitized = SanitizeResponse<PaginatedResponse<FoodItem>>(result, "food records");
            cache.Set(cacheKey, sanitized, TimeSpan.FromMinutes(15));
            return sanitized;
        }
    }
}
