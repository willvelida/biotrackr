using Azure.Security.KeyVault.Secrets;
using Biotrackr.Food.Svc.Models.FitbitEntities;
using Biotrackr.Food.Svc.Services.Interfaces;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace Biotrackr.Food.Svc.Services
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

        public async Task<FoodResponse> GetFoodResponse(string date)
        {
            try
            {
                KeyVaultSecret fitbitAccessToken = await _secretClient.GetSecretAsync("AccessToken");
                _httpClient.DefaultRequestHeaders.Clear();
                Uri getDailyFoodLogUri = new Uri($"https://api.fitbit.com/1/user/-/foods/log/date/{date}.json");
                var request = new HttpRequestMessage(HttpMethod.Get, getDailyFoodLogUri);
                request.Content = new StringContent("");
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", fitbitAccessToken.Value);

                var response = await _httpClient.SendAsync(request);
                response.EnsureSuccessStatusCode();

                var responseString = await response.Content.ReadAsStringAsync();
                var foodResponse = JsonSerializer.Deserialize<FoodResponse>(responseString);

                return foodResponse;
            }
            catch (Exception ex)
            {
                _logger.LogError($"Exception thrown in {nameof(GetFoodResponse)}: {ex.Message}");
                throw;
            }
        }
    }
}
