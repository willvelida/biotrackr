using Azure.Security.KeyVault.Secrets;
using Biotrackr.Auth.Models;
using Biotrackr.Auth.Services.Interfaces;
using Microsoft.Extensions.Logging;
using System.Net.Http.Headers;
using System.Text.Json;

namespace Biotrackr.Auth.Services
{
    public class RefreshTokenService : IRefreshTokenService
    {
        private readonly SecretClient _secretClient;
        private readonly HttpClient _httpClient;
        private readonly ILogger<RefreshTokenService> _logger;

        public RefreshTokenService(SecretClient secretClient, HttpClient httpClient, ILogger<RefreshTokenService> logger)
        {
            _secretClient = secretClient;
            _httpClient = httpClient;
            _logger = logger;
        }

        public async Task<RefreshTokenResponse> RefreshTokens()
        {
            try
            {
                var fitbitRefreshTokenSecret = await _secretClient.GetSecretAsync("RefreshToken");
                if (fitbitRefreshTokenSecret is null)
                    throw new NullReferenceException("Fitbit refresh token not found in secret store");

                var fitbitClientCredentials = await _secretClient.GetSecretAsync("FitbitCredentials");
                if (fitbitClientCredentials is null)
                    throw new NullReferenceException("Fitbit credentials not found in secret store");

                _httpClient.DefaultRequestHeaders.Clear();
                UriBuilder uri = new UriBuilder("https://api.fitbit.com/oauth2/token");
                uri.Query = $"grant_type=refresh_token&refresh_token={fitbitRefreshTokenSecret.Value}";
                var request = new HttpRequestMessage(HttpMethod.Post, uri.Uri);
                request.Content = new StringContent("");
                request.Content.Headers.ContentType = new MediaTypeHeaderValue("application/x-www-form-urlencoded");
                request.Headers.Authorization = new AuthenticationHeaderValue("Basic", fitbitClientCredentials.Value.ToString());

                var response = await _httpClient.SendAsync(request);
                response.EnsureSuccessStatusCode();
                _logger.LogInformation("Fitbit API called successfully. Parsing response");

                var content = await response.Content.ReadAsStringAsync();
                var tokens = JsonSerializer.Deserialize<RefreshTokenResponse>(content);

                return tokens;
            }
            catch (Exception ex)
            {
                _logger.LogInformation($"Exception thrown in {nameof(RefreshTokens)}: {ex.Message}");
                throw;
            }
        }

        public async Task SaveTokens(RefreshTokenResponse tokens)
        {
            try
            {
                _logger.LogInformation("Attempting to save tokens to secret store");
                await _secretClient.SetSecretAsync("RefreshToken", tokens.RefreshToken);
                await _secretClient.SetSecretAsync("AccessToken", tokens.AccessToken);
                _logger.LogInformation("Tokens saved to secret store");
            }
            catch (Exception ex)
            {
                _logger.LogError($"Exception thrown in {nameof(SaveTokens)}: {ex.Message}");
                throw;
            }
        }
    }
}
