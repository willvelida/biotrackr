using Azure.Security.KeyVault.Secrets;
using Biotrackr.Auth.Svc.Models;
using Biotrackr.Auth.Svc.Services.Interfaces;
using System.Net.Http.Headers;
using System.Text.Json;

namespace Biotrackr.Auth.Svc.Services
{
    public class RefreshTokenService : IRefreshTokenService
    {
        private readonly SecretClient _secretClient;
        private readonly HttpClient _httpClient;
        private readonly ILogger<RefreshTokenService> _logger;

        private const string RefreshTokenSecretName = "RefreshToken";
        private const string FitbitCredentialsSecretName = "FitbitCredentials";
        private const string FitbitTokenUrl = "https://api.fitbit.com/oauth2/token";

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
                var fitbitRefreshToken = await GetSecretAsync(RefreshTokenSecretName);
                var fitbitClientCredentials = await GetSecretAsync(FitbitCredentialsSecretName);

                var tokens = await RequestNewTokensAsync(fitbitRefreshToken, fitbitClientCredentials);
                return tokens;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Exception thrown in {nameof(RefreshTokens)}");
                throw;
            }
        }

        public async Task SaveTokens(RefreshTokenResponse tokens)
        {
            try
            {
                _logger.LogInformation("Attempting to save tokens to secret store");
                await _secretClient.SetSecretAsync(RefreshTokenSecretName, tokens.RefreshToken);
                await _secretClient.SetSecretAsync("AccessToken", tokens.AccessToken);
                _logger.LogInformation("Tokens saved to secret store");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Exception thrown in {nameof(SaveTokens)}");
                throw;
            }
        }

        private async Task<string> GetSecretAsync(string secretName)
        {
            var secret = await _secretClient.GetSecretAsync(secretName);
            if (secret is null)
            {
                throw new NullReferenceException($"{secretName} not found in secret store");
            }
            return secret.Value.Value;
        }

        private async Task<RefreshTokenResponse> RequestNewTokensAsync(string refreshToken, string clientCredentials)
        {
            _httpClient.DefaultRequestHeaders.Clear();
            var uri = new UriBuilder(FitbitTokenUrl)
            {
                Query = $"grant_type=refresh_token&refresh_token={refreshToken}"
            };

            var request = new HttpRequestMessage(HttpMethod.Post, uri.Uri)
            {
                Content = new StringContent("")
            };
            request.Content.Headers.ContentType = new MediaTypeHeaderValue("application/x-www-form-urlencoded");
            request.Headers.Authorization = new AuthenticationHeaderValue("Basic", clientCredentials);

            var response = await _httpClient.SendAsync(request);
            response.EnsureSuccessStatusCode();
            _logger.LogInformation("Fitbit API called successfully. Parsing response");

            var content = await response.Content.ReadAsStringAsync();
            var tokens = JsonSerializer.Deserialize<RefreshTokenResponse>(content);

            return tokens;
        }
    }
}
