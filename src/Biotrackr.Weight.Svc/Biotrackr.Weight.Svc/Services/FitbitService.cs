using Azure.Security.KeyVault.Secrets;
using Biotrackr.Weight.Svc.Models.Entities;
using Biotrackr.Weight.Svc.Services.Interfaces;
using System.Net.Http.Headers;
using System.Text.Json;

namespace Biotrackr.Weight.Svc.Services
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

        public async Task<WeightResponse> GetWeightLogs(string startDate, string endDate)
        {
            try
            {
                KeyVaultSecret fitbitAccessToken = await _secretClient.GetSecretAsync("AccessToken");
                _httpClient.DefaultRequestHeaders.Clear();
                Uri getWeightLogsUri = new Uri($"https://api.fitbit.com/1/user/-/body/log/weight/date/{startDate}/{endDate}.json");
                var request = new HttpRequestMessage(HttpMethod.Get, getWeightLogsUri);
                request.Content = new StringContent("");
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", fitbitAccessToken.Value);

                var response = await _httpClient.SendAsync(request);
                response.EnsureSuccessStatusCode();

                var responseAsString = await response.Content.ReadAsStringAsync();
                var weightResponse = JsonSerializer.Deserialize<WeightResponse>(responseAsString);

                return weightResponse;
            }
            catch (Exception ex)
            {
                _logger.LogError($"Exception thrown in {nameof(GetWeightLogs)}: {ex.Message}");
                throw;
            }
        }
    }
}
