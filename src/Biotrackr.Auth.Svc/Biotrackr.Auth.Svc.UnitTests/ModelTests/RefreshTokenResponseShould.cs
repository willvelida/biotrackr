using Biotrackr.Auth.Svc.Models;
using FluentAssertions;
using System.Text.Json;

namespace Biotrackr.Auth.Svc.UnitTests.ModelTests
{
    public class RefreshTokenResponseShould
    {
        [Fact]
        public void DeserializeCorrectlyFromFitbitApiResponse()
        {
            // ARRANGE
            var jsonResponse = """
            {
                "access_token": "test_access_token",
                "expires_in": 3600,
                "refresh_token": "test_refresh_token",
                "scope": "activity",
                "token_type": "Bearer",
                "user_id": "123456"
            }
            """;

            // ACT
            var result = JsonSerializer.Deserialize<RefreshTokenResponse>(jsonResponse);

            // ASSERT
            result.Should().NotBeNull();
            result.AccessToken.Should().Be("test_access_token");
            result.ExpiresIn.Should().Be(3600);
            result.RefreshToken.Should().Be("test_refresh_token");
            result.Scope.Should().Be("activity");
            result.TokenType.Should().Be("Bearer");
            result.UserType.Should().Be("123456");
        }
    }
}
