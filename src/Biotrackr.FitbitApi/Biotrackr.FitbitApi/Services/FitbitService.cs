using Biotrackr.FitbitApi.FitbitEntities;
using Biotrackr.FitbitApi.Services.Interfaces;
using Microsoft.Extensions.Logging;
using System.Net.Http.Headers;
using System.Text.Json;

namespace Biotrackr.FitbitApi.Services
{
    public class FitbitService : IFitbitService
    {
        private readonly ISecretService _secretService;
        private readonly HttpClient _httpClient;
        private readonly ILogger<FitbitService> _logger;

        public FitbitService(ISecretService secretService, HttpClient httpClient, ILogger<FitbitService> logger)
        {
            _secretService = secretService;
            _httpClient = httpClient;
            _logger = logger;
        }

        public async Task<ActivityResponse> GetActivityResponse(string date)
        {
            try
            {
                var accessToken = await _secretService.GetSecretAsync("AccessToken");
               return await RequestActivityRecord(date, accessToken);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Exception thrown in {nameof(GetActivityResponse)}: {ex.Message}");
                throw;
            }
        }

        private async Task<ActivityResponse> RequestActivityRecord(string date, string accessToken)
        {
            _httpClient.DefaultRequestHeaders.Clear();
            Uri getDailyActivityLogUri = new Uri($"https://api.fitbit.com/1/user/-/activities/date/{date}.json");
            var request = new HttpRequestMessage(HttpMethod.Get, getDailyActivityLogUri);
            request.Content = new StringContent("");
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

            var response = await _httpClient.SendAsync(request);
            response.EnsureSuccessStatusCode();
            var responseContent = await response.Content.ReadAsStringAsync();
            var activityResponse = JsonSerializer.Deserialize<ActivityResponse>(responseContent);

            return activityResponse;
        }       
    }
}
