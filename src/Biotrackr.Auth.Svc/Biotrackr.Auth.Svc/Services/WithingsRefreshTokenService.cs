using Azure.Security.KeyVault.Secrets;
using Biotrackr.Auth.Svc.Models;
using Biotrackr.Auth.Svc.Services.Interfaces;
using System.Text.Json;

namespace Biotrackr.Auth.Svc.Services
{
    public class WithingsRefreshTokenService : IWithingsRefreshTokenService
    {
        private readonly SecretClient _secretClient;
        private readonly HttpClient _httpClient;
        private readonly ILogger<WithingsRefreshTokenService> _logger;

        private const string WithingsRefreshTokenSecretName = "WithingsRefreshToken";
        private const string WithingsClientIdSecretName = "WithingsClientId";
        private const string WithingsClientSecretSecretName = "WithingsClientSecret";
        private const string WithingsAccessTokenSecretName = "WithingsAccessToken";
        private const string WithingsTokenUrl = "https://wbsapi.withings.net/v2/oauth2";

        public WithingsRefreshTokenService(SecretClient secretClient, HttpClient httpClient, ILogger<WithingsRefreshTokenService> logger)
        {
            _secretClient = secretClient;
            _httpClient = httpClient;
            _logger = logger;
        }

        public async Task<WithingsTokenResponse> RefreshTokens()
        {
            try
            {
                var refreshToken = await GetSecretAsync(WithingsRefreshTokenSecretName);
                var clientId = await GetSecretAsync(WithingsClientIdSecretName);
                var clientSecret = await GetSecretAsync(WithingsClientSecretSecretName);

                var response = await RequestNewTokensAsync(refreshToken, clientId, clientSecret);
                return response;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Exception thrown in {nameof(RefreshTokens)}");
                throw;
            }
        }

        public async Task SaveTokens(WithingsTokenResponse response)
        {
            try
            {
                _logger.LogInformation("Attempting to save Withings tokens to secret store");
                await _secretClient.SetSecretAsync(WithingsAccessTokenSecretName, response.Body!.AccessToken);
                await _secretClient.SetSecretAsync(WithingsRefreshTokenSecretName, response.Body.RefreshToken);
                _logger.LogInformation("Withings tokens saved to secret store");
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

        private async Task<WithingsTokenResponse> RequestNewTokensAsync(string refreshToken, string clientId, string clientSecret)
        {
            var formContent = new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["action"] = "requesttoken",
                ["client_id"] = clientId,
                ["client_secret"] = clientSecret,
                ["grant_type"] = "refresh_token",
                ["refresh_token"] = refreshToken
            });

            var response = await _httpClient.PostAsync(WithingsTokenUrl, formContent);
            response.EnsureSuccessStatusCode();
            _logger.LogInformation("Withings API called successfully. Parsing response");

            var content = await response.Content.ReadAsStringAsync();
            var tokenResponse = JsonSerializer.Deserialize<WithingsTokenResponse>(content);

            if (tokenResponse is null || tokenResponse.Status != 0)
            {
                throw new InvalidOperationException(
                    $"Withings token refresh failed. Status: {tokenResponse?.Status}. Response: {content}");
            }

            return tokenResponse;
        }
    }
}
