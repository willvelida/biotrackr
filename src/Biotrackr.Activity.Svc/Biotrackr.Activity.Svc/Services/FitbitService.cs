using Azure.Security.KeyVault.Secrets;
using Biotrackr.Activity.Svc.Models.FitbitEntities;
using Biotrackr.Activity.Svc.Services.Interfaces;
using System.Net.Http.Headers;
using System.Text.Json;

namespace Biotrackr.Activity.Svc.Services
{
    public class FitbitService : IFitbitService
    {
        private readonly SecretClient _secretClient;
        private readonly HttpClient _httpClient;
        private readonly ILogger<FitbitService> _logger;

        public FitbitService(SecretClient secretClient, HttpClient httpClient, ILogger<FitbitService> logger)
        {
            _secretClient = secretClient;
            _httpClient = httpClient;
            _logger = logger;
        }

        public async Task<ActivityResponse> GetActivityResponse(string date)
        {
            try
            {
                KeyVaultSecret fitbitAccessToken = await _secretClient.GetSecretAsync("AccessToken");
                _httpClient.DefaultRequestHeaders.Clear();
                Uri getDailyActivityLogUri = new Uri($"https://api.fitbit.com/1/user/-/activities/date/{date}.json");
                var request = new HttpRequestMessage(HttpMethod.Get, getDailyActivityLogUri);
                request.Content = new StringContent("");
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", fitbitAccessToken.Value);

                var response = await _httpClient.SendAsync(request);
                response.EnsureSuccessStatusCode();

                var responseString = await response.Content.ReadAsStringAsync();
                var activityResponse = JsonSerializer.Deserialize<ActivityResponse>(responseString);

                return activityResponse;
            }
            catch (Exception ex)
            {
                _logger.LogError($"Exception thrown in {nameof(GetActivityResponse)}: {ex.Message}");
                throw;
            }
        }
    }
}
