using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Serialization;

namespace Biotrackr.Auth.Svc.Models
{
    [ExcludeFromCodeCoverage]
    public class RefreshTokenResponse
    {
        [JsonPropertyName("access_token")]
        public string AccessToken { get; set; }

        [JsonPropertyName("expires_in")]
        public int ExpiresIn { get; set; }

        [JsonPropertyName("refresh_token")]
        public string RefreshToken { get; set; }

        [JsonPropertyName("scope")]
        public string Scope { get; set; }

        [JsonPropertyName("token_type")]
        public string TokenType { get; set; }

        [JsonPropertyName("user_id")]
        public string UserType { get; set; }
    }
}
