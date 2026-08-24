using AutoFixture;
using Azure;
using Azure.Security.KeyVault.Secrets;
using Biotrackr.Auth.Svc.Models;
using Biotrackr.Auth.Svc.Services;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using Moq.Protected;
using System.Net;
using System.Text.Json;

namespace Biotrackr.Auth.Svc.UnitTests.ServiceTests
{
    public class RefreshTokenServiceShould
    {
        private readonly Mock<SecretClient> _mockSecretClient;
        private readonly Mock<HttpMessageHandler> _mockHttpMessageHandler;
        private readonly Mock<ILogger<RefreshTokenService>> _mockLogger;
        private readonly RefreshTokenService _sut;

        private const string RefreshTokenSecretName = "RefreshToken";
        private const string FitbitCredentialsSecretName = "FitbitCredentials";
        private const string AccessTokenSecretName = "AccessToken";

        public RefreshTokenServiceShould()
        {
            _mockSecretClient = new Mock<SecretClient>();
            _mockHttpMessageHandler = new Mock<HttpMessageHandler>();
            _mockLogger = new Mock<ILogger<RefreshTokenService>>();

            var httpClient = new HttpClient(_mockHttpMessageHandler.Object);
            _sut = new RefreshTokenService(_mockSecretClient.Object, httpClient, _mockLogger.Object);
        }

        [Fact]
        public async Task RefreshTokens_ShouldReturnTokens_WhenSecretsAndHttpResponseAreValid()
        {
            // Arrange
            var fixture = new Fixture();
            var mockRefreshTokenResponse = fixture.Create<RefreshTokenResponse>();
            var mockFitbitRefreshToken = "testFitbitRefreshToken";
            var mockFitbitCredential = "testFitbitCredential";

            SetupSecretClientMocks(mockFitbitRefreshToken, mockFitbitCredential);
            SetupHttpMessageHandlerMock(mockRefreshTokenResponse);

            // Act
            var result = await _sut.RefreshTokens();

            // Assert
            result.AccessToken.Should().Be(mockRefreshTokenResponse.AccessToken);
            result.RefreshToken.Should().Be(mockRefreshTokenResponse.RefreshToken);
            result.ExpiresIn.Should().Be(mockRefreshTokenResponse.ExpiresIn);
            result.Scope.Should().Be(mockRefreshTokenResponse.Scope);
            result.TokenType.Should().Be(mockRefreshTokenResponse.TokenType);
            result.UserType.Should().Be(mockRefreshTokenResponse.UserType);
        }

        [Fact]
        public async Task RefreshTokens_ShouldSendPostRequestToFitbitTokenEndpoint_WhenCalled()
        {
            // Arrange
            var fixture = new Fixture();
            var mockRefreshTokenResponse = fixture.Create<RefreshTokenResponse>();
            var mockFitbitRefreshToken = "testFitbitRefreshToken";
            var mockFitbitCredential = "testFitbitCredential";
            HttpRequestMessage? capturedRequest = null;

            SetupSecretClientMocks(mockFitbitRefreshToken, mockFitbitCredential);

            _mockHttpMessageHandler.Protected()
                .Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>())
                .Callback<HttpRequestMessage, CancellationToken>((request, _) => capturedRequest = request)
                .ReturnsAsync(new HttpResponseMessage
                {
                    StatusCode = HttpStatusCode.OK,
                    Content = new StringContent(JsonSerializer.Serialize(mockRefreshTokenResponse))
                });

            // Act
            await _sut.RefreshTokens();

            // Assert
            capturedRequest.Should().NotBeNull();
            capturedRequest!.Method.Should().Be(HttpMethod.Post);
            capturedRequest.RequestUri.Should().NotBeNull();
            capturedRequest.RequestUri!.AbsoluteUri.Should().Contain("https://api.fitbit.com/oauth2/token");
            capturedRequest.RequestUri.Query.Should().Contain($"grant_type=refresh_token&refresh_token={mockFitbitRefreshToken}");
            capturedRequest.Headers.Authorization.Should().NotBeNull();
            capturedRequest.Headers.Authorization!.Scheme.Should().Be("Basic");
            capturedRequest.Headers.Authorization.Parameter.Should().Be(mockFitbitCredential);
            capturedRequest.Content.Should().NotBeNull();
            capturedRequest.Content!.Headers.ContentType!.MediaType.Should().Be("application/x-www-form-urlencoded");
        }

        [Fact]
        public async Task SaveTokens_ShouldSaveRefreshAndAccessTokens_WhenCalled()
        {
            // Arrange
            var fixture = new Fixture();
            var mockTokenResponse = fixture.Create<RefreshTokenResponse>();

            _mockSecretClient.Setup(x => x.SetSecretAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new Mock<Response<KeyVaultSecret>>().Object);

            // Act
            await _sut.SaveTokens(mockTokenResponse);

            // Assert
            _mockSecretClient.Verify(x => x.SetSecretAsync(RefreshTokenSecretName, mockTokenResponse.RefreshToken, It.IsAny<CancellationToken>()), Times.Once);
            _mockSecretClient.Verify(x => x.SetSecretAsync(AccessTokenSecretName, mockTokenResponse.AccessToken, It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task SaveTokens_ShouldLogStartAndCompletionMessages_WhenSaveSucceeds()
        {
            // Arrange
            var fixture = new Fixture();
            var mockTokenResponse = fixture.Create<RefreshTokenResponse>();

            _mockSecretClient.Setup(x => x.SetSecretAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new Mock<Response<KeyVaultSecret>>().Object);

            // Act
            await _sut.SaveTokens(mockTokenResponse);

            // Assert
            _mockLogger.VerifyLog(l => l.LogInformation("Attempting to save tokens to secret store"), Times.Once);
            _mockLogger.VerifyLog(l => l.LogInformation("Tokens saved to secret store"), Times.Once);
        }

        [Fact]
        public async Task RefreshTokens_ShouldLogSuccessMessage_WhenFitbitApiCallSucceeds()
        {
            // Arrange
            var fixture = new Fixture();
            var mockRefreshTokenResponse = fixture.Create<RefreshTokenResponse>();
            var mockFitbitRefreshToken = "testFitbitRefreshToken";
            var mockFitbitCredential = "testFitbitCredential";

            SetupSecretClientMocks(mockFitbitRefreshToken, mockFitbitCredential);
            SetupHttpMessageHandlerMock(mockRefreshTokenResponse);

            // Act
            await _sut.RefreshTokens();

            // Assert
            _mockLogger.VerifyLog(l => l.LogInformation("Fitbit API called successfully. Parsing response"), Times.Once);
        }

        [Fact]
        public async Task RefreshTokens_ShouldThrowAndLogError_WhenRefreshTokenSecretIsNotFound()
        {
            // Arrange
            _mockSecretClient.Setup(x => x.GetSecretAsync(RefreshTokenSecretName, null, It.IsAny<CancellationToken>()))
                .ReturnsAsync(Response.FromValue<KeyVaultSecret>(null!, new Mock<Response>().Object));

            // Act
            Func<Task> refreshTokenAction = async () => await _sut.RefreshTokens();

            // Assert
            await refreshTokenAction.Should().ThrowAsync<NullReferenceException>();
            _mockLogger.VerifyLog(l => l.LogError(It.IsAny<Exception>(), $"Exception thrown in {nameof(RefreshTokenService.RefreshTokens)}"), Times.Once);
        }

        [Fact]
        public async Task RefreshTokens_ShouldThrowAndLogError_WhenFitbitCredentialsSecretIsNotFound()
        {
            // Arrange
            var mockFitbitRefreshToken = "testFitbitRefreshToken";

            _mockSecretClient.Setup(x => x.GetSecretAsync(RefreshTokenSecretName, null, It.IsAny<CancellationToken>()))
                .ReturnsAsync(Response.FromValue(new KeyVaultSecret(RefreshTokenSecretName, mockFitbitRefreshToken), new Mock<Response>().Object));
            _mockSecretClient.Setup(x => x.GetSecretAsync(FitbitCredentialsSecretName, null, It.IsAny<CancellationToken>()))
                .ReturnsAsync(Response.FromValue<KeyVaultSecret>(null!, new Mock<Response>().Object));

            // Act
            Func<Task> refreshTokenAction = async () => await _sut.RefreshTokens();

            // Assert
            await refreshTokenAction.Should().ThrowAsync<NullReferenceException>();
            _mockLogger.VerifyLog(l => l.LogError(It.IsAny<Exception>(), $"Exception thrown in {nameof(RefreshTokenService.RefreshTokens)}"), Times.Once);
        }

        [Fact]
        public async Task RefreshTokens_ShouldThrowHttpRequestExceptionAndLogError_WhenFitbitApiReturnsUnauthorized()
        {
            // Arrange
            var mockFitbitRefreshToken = "testFitbitRefreshToken";
            var mockFitbitCredential = "testFitbitCredential";

            SetupSecretClientMocks(mockFitbitRefreshToken, mockFitbitCredential);

            _mockHttpMessageHandler.Protected()
                .Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>())
                .ReturnsAsync(new HttpResponseMessage
                {
                    StatusCode = HttpStatusCode.Unauthorized,
                    Content = new StringContent("Unauthorized")
                });

            // Act
            Func<Task> refreshTokenAction = async () => await _sut.RefreshTokens();

            // Assert
            await refreshTokenAction.Should().ThrowAsync<HttpRequestException>();
            _mockLogger.VerifyLog(l => l.LogError(It.IsAny<Exception>(), $"Exception thrown in {nameof(RefreshTokenService.RefreshTokens)}"), Times.Once);
        }

        [Fact]
        public async Task SaveTokens_ShouldThrowAndLogError_WhenSecretClientThrows()
        {
            // Arrange
            var fixture = new Fixture();
            var mockTokenResponse = fixture.Create<RefreshTokenResponse>();
            var testException = new Exception("Key Vault error");

            _mockSecretClient.Setup(x => x.SetSecretAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ThrowsAsync(testException);

            // Act
            Func<Task> saveTokenAction = async () => await _sut.SaveTokens(mockTokenResponse);

            // Assert
            await saveTokenAction.Should().ThrowAsync<Exception>().WithMessage("Key Vault error");
            _mockLogger.VerifyLog(l => l.LogError(testException, $"Exception thrown in {nameof(RefreshTokenService.SaveTokens)}"), Times.Once);
        }

        [Fact]
        public async Task RefreshTokens_ShouldThrowJsonException_WhenResponseContainsInvalidJson()
        {
            // Arrange
            var mockFitbitRefreshToken = "testFitbitRefreshToken";
            var mockFitbitCredential = "testFitbitCredential";

            SetupSecretClientMocks(mockFitbitRefreshToken, mockFitbitCredential);

            _mockHttpMessageHandler.Protected()
                .Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>())
                .ReturnsAsync(new HttpResponseMessage
                {
                    StatusCode = HttpStatusCode.OK,
                    Content = new StringContent("{ invalid json }")
                });

            // Act
            Func<Task> refreshTokenAction = async () => await _sut.RefreshTokens();

            // Assert
            await refreshTokenAction.Should().ThrowAsync<JsonException>();
            _mockLogger.VerifyLog(l => l.LogError(It.IsAny<Exception>(), $"Exception thrown in {nameof(RefreshTokenService.RefreshTokens)}"), Times.Once);
        }

        [Fact]
        public async Task RefreshTokens_ShouldThrowJsonException_WhenFitbitApiReturnsEmptyBody()
        {
            // Arrange
            var mockFitbitRefreshToken = "testFitbitRefreshToken";
            var mockFitbitCredential = "testFitbitCredential";

            SetupSecretClientMocks(mockFitbitRefreshToken, mockFitbitCredential);

            _mockHttpMessageHandler.Protected()
                .Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>())
                .ReturnsAsync(new HttpResponseMessage
                {
                    StatusCode = HttpStatusCode.OK,
                    Content = new StringContent("")
                });

            // Act
            Func<Task> refreshTokenAction = async () => await _sut.RefreshTokens();

            // Assert
            await refreshTokenAction.Should().ThrowAsync<JsonException>();
        }

        [Fact]
        public async Task RefreshTokens_ShouldThrowHttpRequestException_WhenFitbitApiReturnsTooManyRequests()
        {
            // Arrange
            var mockFitbitRefreshToken = "testFitbitRefreshToken";
            var mockFitbitCredential = "testFitbitCredential";

            SetupSecretClientMocks(mockFitbitRefreshToken, mockFitbitCredential);

            _mockHttpMessageHandler.Protected()
                .Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>())
                .ReturnsAsync(new HttpResponseMessage
                {
                    StatusCode = (HttpStatusCode)429,
                    Content = new StringContent("Too Many Requests")
                });

            // Act
            Func<Task> refreshTokenAction = async () => await _sut.RefreshTokens();

            // Assert
            await refreshTokenAction.Should().ThrowAsync<HttpRequestException>();
            _mockLogger.VerifyLog(l => l.LogError(It.IsAny<Exception>(), $"Exception thrown in {nameof(RefreshTokenService.RefreshTokens)}"), Times.Once);
        }

        [Fact]
        public async Task RefreshTokens_ShouldThrowTaskCanceledException_WhenHttpRequestTimesOut()
        {
            // Arrange
            var mockFitbitRefreshToken = "testFitbitRefreshToken";
            var mockFitbitCredential = "testFitbitCredential";

            SetupSecretClientMocks(mockFitbitRefreshToken, mockFitbitCredential);

            _mockHttpMessageHandler.Protected()
                .Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>())
                .ThrowsAsync(new TaskCanceledException("The request was canceled due to the configured HttpClient.Timeout"));

            // Act
            Func<Task> refreshTokenAction = async () => await _sut.RefreshTokens();

            // Assert
            await refreshTokenAction.Should().ThrowAsync<TaskCanceledException>();
            _mockLogger.VerifyLog(l => l.LogError(It.IsAny<Exception>(), $"Exception thrown in {nameof(RefreshTokenService.RefreshTokens)}"), Times.Once);
        }

        private void SetupSecretClientMocks(string mockFitbitRefreshToken, string mockFitbitCredential)
        {
            _mockSecretClient.Setup(x => x.GetSecretAsync(RefreshTokenSecretName, null, It.IsAny<CancellationToken>()))
                .ReturnsAsync(Response.FromValue(new KeyVaultSecret(RefreshTokenSecretName, mockFitbitRefreshToken), new Mock<Response>().Object));
            _mockSecretClient.Setup(x => x.GetSecretAsync(FitbitCredentialsSecretName, null, It.IsAny<CancellationToken>()))
                .ReturnsAsync(Response.FromValue(new KeyVaultSecret(FitbitCredentialsSecretName, mockFitbitCredential), new Mock<Response>().Object));
        }

        private void SetupHttpMessageHandlerMock(RefreshTokenResponse mockRefreshTokenResponse)
        {
            _mockHttpMessageHandler.Protected()
                .Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>())
                .ReturnsAsync(new HttpResponseMessage
                {
                    StatusCode = HttpStatusCode.OK,
                    Content = new StringContent(JsonSerializer.Serialize(mockRefreshTokenResponse))
                });
        }
    }
}
