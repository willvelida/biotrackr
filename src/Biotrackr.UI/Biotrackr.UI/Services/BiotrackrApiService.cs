using System.Net.Http.Json;
using System.Text.Json;
using Biotrackr.UI.Models;
using Biotrackr.UI.Models.Activity;
using Biotrackr.UI.Models.Food;
using Biotrackr.UI.Models.Sleep;
using Biotrackr.UI.Models.Vitals;
using Microsoft.Extensions.Logging;

namespace Biotrackr.UI.Services
{
    public class BiotrackrApiService : IBiotrackrApiService
    {
        private readonly HttpClient _httpClient;
        private readonly ILogger<BiotrackrApiService> _logger;

        private static readonly JsonSerializerOptions JsonOptions = new()
        {
            PropertyNameCaseInsensitive = true
        };

        public BiotrackrApiService(HttpClient httpClient, ILogger<BiotrackrApiService> logger)
        {
            _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        // Activity endpoints
        public async Task<PaginatedResponse<ActivityItem>> GetActivitiesAsync(int pageNumber = 1, int pageSize = 20)
        {
            var endpoint = BuildPaginatedEndpoint("/activity", pageNumber, pageSize);
            return await GetAsync<PaginatedResponse<ActivityItem>>(endpoint) ?? new PaginatedResponse<ActivityItem>();
        }

        public async Task<ActivityItem?> GetActivityByDateAsync(string date)
        {
            return await GetAsync<ActivityItem>($"/activity/{date}");
        }

        public async Task<PaginatedResponse<ActivityItem>> GetActivitiesByDateRangeAsync(string startDate, string endDate, int pageNumber = 1, int pageSize = 20)
        {
            var endpoint = BuildPaginatedEndpoint($"/activity/range/{startDate}/{endDate}", pageNumber, pageSize);
            return await GetAsync<PaginatedResponse<ActivityItem>>(endpoint) ?? new PaginatedResponse<ActivityItem>();
        }

        // Food endpoints
        public async Task<PaginatedResponse<FoodItem>> GetFoodLogsAsync(int pageNumber = 1, int pageSize = 20)
        {
            var endpoint = BuildPaginatedEndpoint("/food", pageNumber, pageSize);
            return await GetAsync<PaginatedResponse<FoodItem>>(endpoint) ?? new PaginatedResponse<FoodItem>();
        }

        public async Task<FoodItem?> GetFoodLogByDateAsync(string date)
        {
            return await GetAsync<FoodItem>($"/food/{date}");
        }

        public async Task<PaginatedResponse<FoodItem>> GetFoodLogsByDateRangeAsync(string startDate, string endDate, int pageNumber = 1, int pageSize = 20)
        {
            var endpoint = BuildPaginatedEndpoint($"/food/range/{startDate}/{endDate}", pageNumber, pageSize);
            return await GetAsync<PaginatedResponse<FoodItem>>(endpoint) ?? new PaginatedResponse<FoodItem>();
        }

        // Sleep endpoints
        public async Task<PaginatedResponse<SleepItem>> GetSleepRecordsAsync(int pageNumber = 1, int pageSize = 20)
        {
            var endpoint = BuildPaginatedEndpoint("/sleep", pageNumber, pageSize);
            return await GetAsync<PaginatedResponse<SleepItem>>(endpoint) ?? new PaginatedResponse<SleepItem>();
        }

        public async Task<SleepItem?> GetSleepByDateAsync(string date)
        {
            return await GetAsync<SleepItem>($"/sleep/{date}");
        }

        public async Task<PaginatedResponse<SleepItem>> GetSleepByDateRangeAsync(string startDate, string endDate, int pageNumber = 1, int pageSize = 20)
        {
            var endpoint = BuildPaginatedEndpoint($"/sleep/range/{startDate}/{endDate}", pageNumber, pageSize);
            return await GetAsync<PaginatedResponse<SleepItem>>(endpoint) ?? new PaginatedResponse<SleepItem>();
        }

        // Vitals endpoints
        public async Task<PaginatedResponse<VitalsItem>> GetVitalsRecordsAsync(int pageNumber = 1, int pageSize = 20)
        {
            var endpoint = BuildPaginatedEndpoint("/vitals", pageNumber, pageSize);
            return await GetAsync<PaginatedResponse<VitalsItem>>(endpoint) ?? new PaginatedResponse<VitalsItem>();
        }

        public async Task<VitalsItem?> GetVitalsByDateAsync(string date)
        {
            return await GetAsync<VitalsItem>($"/vitals/{date}");
        }

        public async Task<PaginatedResponse<VitalsItem>> GetVitalsByDateRangeAsync(string startDate, string endDate, int pageNumber = 1, int pageSize = 20)
        {
            var endpoint = BuildPaginatedEndpoint($"/vitals/range/{startDate}/{endDate}", pageNumber, pageSize);
            return await GetAsync<PaginatedResponse<VitalsItem>>(endpoint) ?? new PaginatedResponse<VitalsItem>();
        }

        private async Task<T?> GetAsync<T>(string endpoint) where T : class
        {
            try
            {
                _logger.LogInformation("Fetching data from {Endpoint}", endpoint);
                var response = await _httpClient.GetAsync(endpoint);

                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogWarning("API call to {Endpoint} returned {StatusCode}", endpoint, response.StatusCode);
                    return null;
                }

                var content = await response.Content.ReadAsStringAsync();
                return JsonSerializer.Deserialize<T>(content, JsonOptions);
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError(ex, "Network error calling {Endpoint}", endpoint);
                return null;
            }
            catch (TaskCanceledException ex)
            {
                _logger.LogError(ex, "Request to {Endpoint} timed out", endpoint);
                return null;
            }
            catch (JsonException ex)
            {
                _logger.LogError(ex, "Failed to deserialize response from {Endpoint}", endpoint);
                return null;
            }
        }

        private static string BuildPaginatedEndpoint(string basePath, int pageNumber, int pageSize)
        {
            pageNumber = Math.Max(1, pageNumber);
            pageSize = Math.Clamp(pageSize, 1, 100);
            return $"{basePath}?pageNumber={pageNumber}&pageSize={pageSize}";
        }
    }
}
