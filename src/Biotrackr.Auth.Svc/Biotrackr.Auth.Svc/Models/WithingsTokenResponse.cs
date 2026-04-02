using System.Text.Json.Serialization;

namespace Biotrackr.Auth.Svc.Models
{
    public class WithingsTokenResponse
    {
        [JsonPropertyName("status")]
        public int Status { get; set; }

        [JsonPropertyName("body")]
        public WithingsTokenBody? Body { get; set; }
    }

    public class WithingsTokenBody
    {
        [JsonPropertyName("userid")]
        public object? UserId { get; set; }

        [JsonPropertyName("access_token")]
        public string AccessToken { get; set; } = string.Empty;

        [JsonPropertyName("refresh_token")]
        public string RefreshToken { get; set; } = string.Empty;

        [JsonPropertyName("expires_in")]
        public int ExpiresIn { get; set; }

        [JsonPropertyName("scope")]
        public string Scope { get; set; } = string.Empty;

        [JsonPropertyName("token_type")]
        public string TokenType { get; set; } = string.Empty;
    }
}
