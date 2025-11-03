using AutoFixture;
using Biotrackr.Auth.Svc.Models;

namespace Biotrackr.Auth.Svc.IntegrationTests.Helpers
{
    /// <summary>
    /// Helper class for generating test data.
    /// Provides sample/fake values for RefreshTokenResponse and credentials.
    /// No real secrets needed since all dependencies are mocked.
    /// </summary>
    public static class TestDataGenerator
    {
        private static readonly Fixture _fixture = new();

        /// <summary>
        /// Generates a sample RefreshTokenResponse with fake values.
        /// </summary>
        public static RefreshTokenResponse CreateRefreshTokenResponse()
        {
            return _fixture.Create<RefreshTokenResponse>();
        }

        /// <summary>
        /// Generates a sample refresh token string.
        /// </summary>
        public static string CreateRefreshToken()
        {
            return $"test_refresh_token_{Guid.NewGuid():N}";
        }

        /// <summary>
        /// Generates a sample Fitbit credentials string (Base64 encoded client_id:client_secret).
        /// </summary>
        public static string CreateFitbitCredentials()
        {
            var clientId = $"test_client_id_{Guid.NewGuid():N}";
            var clientSecret = $"test_client_secret_{Guid.NewGuid():N}";
            var credentials = $"{clientId}:{clientSecret}";
            return Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(credentials));
        }

        /// <summary>
        /// Generates a sample access token string.
        /// </summary>
        public static string CreateAccessToken()
        {
            return $"test_access_token_{Guid.NewGuid():N}";
        }
    }
}
