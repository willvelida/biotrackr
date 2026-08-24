using Biotrackr.Auth.Svc.Models;
using FluentAssertions;
using System.Text.Json;

namespace Biotrackr.Auth.Svc.UnitTests.ModelTests
{
    public class WithingsTokenResponseShould
    {
        [Fact]
        public void Deserialize_ShouldMapAllProperties_WhenWithingsApiResponseIsValid()
        {
            // Arrange
            var jsonResponse = """
            {
                "status": 0,
                "body": {
                    "userid": "363",
                    "access_token": "test_access_token",
                    "refresh_token": "test_refresh_token",
                    "expires_in": 10800,
                    "scope": "user.info,user.metrics",
                    "token_type": "Bearer"
                }
            }
            """;

            // Act
            var result = JsonSerializer.Deserialize<WithingsTokenResponse>(jsonResponse);

            // Assert
            result.Should().NotBeNull();
            result!.Status.Should().Be(0);
            result.Body.Should().NotBeNull();
            result.Body!.AccessToken.Should().Be("test_access_token");
            result.Body.RefreshToken.Should().Be("test_refresh_token");
            result.Body.ExpiresIn.Should().Be(10800);
            result.Body.Scope.Should().Be("user.info,user.metrics");
            result.Body.TokenType.Should().Be("Bearer");
        }

        [Fact]
        public void Deserialize_ShouldMapStatusAndLeaveBodyNull_WhenWithingsApiReturnsErrorResponse()
        {
            // Arrange
            var jsonResponse = """
            {
                "status": 601
            }
            """;

            // Act
            var result = JsonSerializer.Deserialize<WithingsTokenResponse>(jsonResponse);

            // Assert
            result.Should().NotBeNull();
            result!.Status.Should().Be(601);
            result.Body.Should().BeNull();
        }

        [Fact]
        public void Deserialize_ShouldPopulateBody_WhenUserIdIsNumericOrString()
        {
            // Arrange
            // Withings returns userid as either string or number
            var jsonResponseNumeric = """
            {
                "status": 0,
                "body": {
                    "userid": 363,
                    "access_token": "token",
                    "refresh_token": "refresh",
                    "expires_in": 10800,
                    "scope": "user.metrics",
                    "token_type": "Bearer"
                }
            }
            """;

            var jsonResponseString = """
            {
                "status": 0,
                "body": {
                    "userid": "363",
                    "access_token": "token",
                    "refresh_token": "refresh",
                    "expires_in": 10800,
                    "scope": "user.metrics",
                    "token_type": "Bearer"
                }
            }
            """;

            // Act
            var resultNumeric = JsonSerializer.Deserialize<WithingsTokenResponse>(jsonResponseNumeric);
            var resultString = JsonSerializer.Deserialize<WithingsTokenResponse>(jsonResponseString);

            // Assert
            resultNumeric.Should().NotBeNull();
            resultNumeric!.Body.Should().NotBeNull();
            resultString.Should().NotBeNull();
            resultString!.Body.Should().NotBeNull();
        }
    }
}
