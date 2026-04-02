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
    public class WithingsRefreshTokenServiceShould
    {
        private readonly Mock<SecretClient> _mockSecretClient;
        private readonly Mock<HttpMessageHandler> _mockHttpMessageHandler;
        private readonly Mock<ILogger<WithingsRefreshTokenService>> _mockLogger;
        private readonly WithingsRefreshTokenService _sut;

        private const string WithingsRefreshTokenSecretName = "WithingsRefreshToken";
        private const string WithingsClientIdSecretName = "WithingsClientId";
        private const string WithingsClientSecretSecretName = "WithingsClientSecret";
        private const string WithingsAccessTokenSecretName = "WithingsAccessToken";

        public WithingsRefreshTokenServiceShould()
        {
            _mockSecretClient = new Mock<SecretClient>();
            _mockHttpMessageHandler = new Mock<HttpMessageHandler>();
            _mockLogger = new Mock<ILogger<WithingsRefreshTokenService>>();

            var httpClient = new HttpClient(_mockHttpMessageHandler.Object);
            _sut = new WithingsRefreshTokenService(_mockSecretClient.Object, httpClient, _mockLogger.Object);
        }

        [Fact]
        public async Task RefreshTokensSuccessfullyWhenValidSecretsAndHttpResponseProvided()
        {
            // ARRANGE
            var mockWithingsResponse = CreateSuccessfulWithingsResponse();
            SetupSecretClientMocks("testRefreshToken", "testClientId", "testClientSecret");
            SetupHttpMessageHandlerMock(mockWithingsResponse);

            // ACT
            var result = await _sut.RefreshTokens();

            // ASSERT
            result.Status.Should().Be(0);
            result.Body.Should().NotBeNull();
            result.Body!.AccessToken.Should().Be(mockWithingsResponse.Body!.AccessToken);
            result.Body.RefreshToken.Should().Be(mockWithingsResponse.Body.RefreshToken);
            result.Body.ExpiresIn.Should().Be(mockWithingsResponse.Body.ExpiresIn);
            result.Body.Scope.Should().Be(mockWithingsResponse.Body.Scope);
            result.Body.TokenType.Should().Be(mockWithingsResponse.Body.TokenType);
        }

        [Fact]
        public async Task MakeCorrectHttpRequestWhenRefreshTokensIsCalled()
        {
            // ARRANGE
            var mockWithingsResponse = CreateSuccessfulWithingsResponse();
            var mockRefreshToken = "testRefreshToken";
            var mockClientId = "testClientId";
            var mockClientSecret = "testClientSecret";
            HttpRequestMessage? capturedRequest = null;
            string? capturedBody = null;

            SetupSecretClientMocks(mockRefreshToken, mockClientId, mockClientSecret);

            _mockHttpMessageHandler.Protected()
                .Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>())
                .Callback<HttpRequestMessage, CancellationToken>(async (request, _) =>
                {
                    capturedRequest = request;
                    capturedBody = await request.Content!.ReadAsStringAsync();
                })
                .ReturnsAsync(new HttpResponseMessage
                {
                    StatusCode = HttpStatusCode.OK,
                    Content = new StringContent(JsonSerializer.Serialize(mockWithingsResponse))
                });

            // ACT
            await _sut.RefreshTokens();

            // ASSERT
            capturedRequest.Should().NotBeNull();
            capturedRequest!.Method.Should().Be(HttpMethod.Post);
            capturedRequest.RequestUri.Should().NotBeNull();
            capturedRequest.RequestUri!.AbsoluteUri.Should().Be("https://wbsapi.withings.net/v2/oauth2");
            capturedBody.Should().NotBeNull();
            capturedBody.Should().Contain("action=requesttoken");
            capturedBody.Should().Contain($"client_id={mockClientId}");
            capturedBody.Should().Contain($"client_secret={mockClientSecret}");
            capturedBody.Should().Contain("grant_type=refresh_token");
            capturedBody.Should().Contain($"refresh_token={mockRefreshToken}");
        }

        [Fact]
        public async Task SaveBothTokensWhenSaveTokensIsCalled()
        {
            // ARRANGE
            var mockWithingsResponse = CreateSuccessfulWithingsResponse();

            _mockSecretClient.Setup(x => x.SetSecretAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new Mock<Response<KeyVaultSecret>>().Object);

            // ACT
            await _sut.SaveTokens(mockWithingsResponse);

            // ASSERT
            _mockSecretClient.Verify(x => x.SetSecretAsync(WithingsAccessTokenSecretName, mockWithingsResponse.Body!.AccessToken, It.IsAny<CancellationToken>()), Times.Once);
            _mockSecretClient.Verify(x => x.SetSecretAsync(WithingsRefreshTokenSecretName, mockWithingsResponse.Body.RefreshToken, It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task LogInformationMessagesWhenSaveTokensIsSuccessful()
        {
            // ARRANGE
            var mockWithingsResponse = CreateSuccessfulWithingsResponse();

            _mockSecretClient.Setup(x => x.SetSecretAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new Mock<Response<KeyVaultSecret>>().Object);

            // ACT
            await _sut.SaveTokens(mockWithingsResponse);

            // ASSERT
            _mockLogger.VerifyLog(l => l.LogInformation("Attempting to save Withings tokens to secret store"), Times.Once);
            _mockLogger.VerifyLog(l => l.LogInformation("Withings tokens saved to secret store"), Times.Once);
        }

        [Fact]
        public async Task LogInformationWhenHttpRequestIsSuccessful()
        {
            // ARRANGE
            var mockWithingsResponse = CreateSuccessfulWithingsResponse();
            SetupSecretClientMocks("testRefreshToken", "testClientId", "testClientSecret");
            SetupHttpMessageHandlerMock(mockWithingsResponse);

            // ACT
            await _sut.RefreshTokens();

            // ASSERT
            _mockLogger.VerifyLog(l => l.LogInformation("Withings API called successfully. Parsing response"), Times.Once);
        }

        [Fact]
        public async Task ThrowExceptionAndLogErrorWhenRefreshTokenSecretNotFound()
        {
            // ARRANGE
            _mockSecretClient.Setup(x => x.GetSecretAsync(WithingsRefreshTokenSecretName, null, It.IsAny<CancellationToken>()))
                .ReturnsAsync(Response.FromValue<KeyVaultSecret>(null!, new Mock<Response>().Object));

            // ACT
            Func<Task> refreshTokenAction = async () => await _sut.RefreshTokens();

            // ASSERT
            await refreshTokenAction.Should().ThrowAsync<NullReferenceException>();
            _mockLogger.VerifyLog(l => l.LogError(It.IsAny<Exception>(), $"Exception thrown in {nameof(WithingsRefreshTokenService.RefreshTokens)}"), Times.Once);
        }

        [Fact]
        public async Task ThrowExceptionAndLogErrorWhenClientIdSecretNotFound()
        {
            // ARRANGE
            _mockSecretClient.Setup(x => x.GetSecretAsync(WithingsRefreshTokenSecretName, null, It.IsAny<CancellationToken>()))
                .ReturnsAsync(Response.FromValue(new KeyVaultSecret(WithingsRefreshTokenSecretName, "testToken"), new Mock<Response>().Object));
            _mockSecretClient.Setup(x => x.GetSecretAsync(WithingsClientIdSecretName, null, It.IsAny<CancellationToken>()))
                .ReturnsAsync(Response.FromValue<KeyVaultSecret>(null!, new Mock<Response>().Object));

            // ACT
            Func<Task> refreshTokenAction = async () => await _sut.RefreshTokens();

            // ASSERT
            await refreshTokenAction.Should().ThrowAsync<NullReferenceException>();
            _mockLogger.VerifyLog(l => l.LogError(It.IsAny<Exception>(), $"Exception thrown in {nameof(WithingsRefreshTokenService.RefreshTokens)}"), Times.Once);
        }

        [Fact]
        public async Task ThrowExceptionAndLogErrorWhenClientSecretSecretNotFound()
        {
            // ARRANGE
            _mockSecretClient.Setup(x => x.GetSecretAsync(WithingsRefreshTokenSecretName, null, It.IsAny<CancellationToken>()))
                .ReturnsAsync(Response.FromValue(new KeyVaultSecret(WithingsRefreshTokenSecretName, "testToken"), new Mock<Response>().Object));
            _mockSecretClient.Setup(x => x.GetSecretAsync(WithingsClientIdSecretName, null, It.IsAny<CancellationToken>()))
                .ReturnsAsync(Response.FromValue(new KeyVaultSecret(WithingsClientIdSecretName, "testClientId"), new Mock<Response>().Object));
            _mockSecretClient.Setup(x => x.GetSecretAsync(WithingsClientSecretSecretName, null, It.IsAny<CancellationToken>()))
                .ReturnsAsync(Response.FromValue<KeyVaultSecret>(null!, new Mock<Response>().Object));

            // ACT
            Func<Task> refreshTokenAction = async () => await _sut.RefreshTokens();

            // ASSERT
            await refreshTokenAction.Should().ThrowAsync<NullReferenceException>();
            _mockLogger.VerifyLog(l => l.LogError(It.IsAny<Exception>(), $"Exception thrown in {nameof(WithingsRefreshTokenService.RefreshTokens)}"), Times.Once);
        }

        [Fact]
        public async Task ThrowHttpRequestExceptionAndLogErrorWhenHttpRequestFails()
        {
            // ARRANGE
            SetupSecretClientMocks("testRefreshToken", "testClientId", "testClientSecret");

            _mockHttpMessageHandler.Protected()
                .Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>())
                .ReturnsAsync(new HttpResponseMessage
                {
                    StatusCode = HttpStatusCode.Unauthorized,
                    Content = new StringContent("Unauthorized")
                });

            // ACT
            Func<Task> refreshTokenAction = async () => await _sut.RefreshTokens();

            // ASSERT
            await refreshTokenAction.Should().ThrowAsync<HttpRequestException>();
            _mockLogger.VerifyLog(l => l.LogError(It.IsAny<Exception>(), $"Exception thrown in {nameof(WithingsRefreshTokenService.RefreshTokens)}"), Times.Once);
        }

        [Fact]
        public async Task ThrowExceptionAndLogErrorWhenSaveTokensFails()
        {
            // ARRANGE
            var mockWithingsResponse = CreateSuccessfulWithingsResponse();
            var testException = new Exception("Key Vault error");

            _mockSecretClient.Setup(x => x.SetSecretAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ThrowsAsync(testException);

            // ACT
            Func<Task> saveTokenAction = async () => await _sut.SaveTokens(mockWithingsResponse);

            // ASSERT
            await saveTokenAction.Should().ThrowAsync<Exception>().WithMessage("Key Vault error");
            _mockLogger.VerifyLog(l => l.LogError(testException, $"Exception thrown in {nameof(WithingsRefreshTokenService.SaveTokens)}"), Times.Once);
        }

        [Fact]
        public async Task ThrowInvalidOperationExceptionWhenWithingsStatusIsNonZero()
        {
            // ARRANGE
            var errorResponse = new WithingsTokenResponse { Status = 601, Body = null };
            SetupSecretClientMocks("testRefreshToken", "testClientId", "testClientSecret");

            _mockHttpMessageHandler.Protected()
                .Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>())
                .ReturnsAsync(new HttpResponseMessage
                {
                    StatusCode = HttpStatusCode.OK,
                    Content = new StringContent(JsonSerializer.Serialize(errorResponse))
                });

            // ACT
            Func<Task> refreshTokenAction = async () => await _sut.RefreshTokens();

            // ASSERT
            await refreshTokenAction.Should().ThrowAsync<InvalidOperationException>()
                .WithMessage("*Status: 601*");
            _mockLogger.VerifyLog(l => l.LogError(It.IsAny<Exception>(), $"Exception thrown in {nameof(WithingsRefreshTokenService.RefreshTokens)}"), Times.Once);
        }

        [Fact]
        public async Task ThrowJsonExceptionWhenInvalidJsonResponseReceived()
        {
            // ARRANGE
            SetupSecretClientMocks("testRefreshToken", "testClientId", "testClientSecret");

            _mockHttpMessageHandler.Protected()
                .Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>())
                .ReturnsAsync(new HttpResponseMessage
                {
                    StatusCode = HttpStatusCode.OK,
                    Content = new StringContent("{ invalid json }")
                });

            // ACT
            Func<Task> refreshTokenAction = async () => await _sut.RefreshTokens();

            // ASSERT
            await refreshTokenAction.Should().ThrowAsync<JsonException>();
            _mockLogger.VerifyLog(l => l.LogError(It.IsAny<Exception>(), $"Exception thrown in {nameof(WithingsRefreshTokenService.RefreshTokens)}"), Times.Once);
        }

        [Fact]
        public async Task ThrowTaskCanceledExceptionWhenNetworkTimeout()
        {
            // ARRANGE
            SetupSecretClientMocks("testRefreshToken", "testClientId", "testClientSecret");

            _mockHttpMessageHandler.Protected()
                .Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>())
                .ThrowsAsync(new TaskCanceledException("The request was canceled due to the configured HttpClient.Timeout"));

            // ACT
            Func<Task> refreshTokenAction = async () => await _sut.RefreshTokens();

            // ASSERT
            await refreshTokenAction.Should().ThrowAsync<TaskCanceledException>();
            _mockLogger.VerifyLog(l => l.LogError(It.IsAny<Exception>(), $"Exception thrown in {nameof(WithingsRefreshTokenService.RefreshTokens)}"), Times.Once);
        }

        private void SetupSecretClientMocks(string mockRefreshToken, string mockClientId, string mockClientSecret)
        {
            _mockSecretClient.Setup(x => x.GetSecretAsync(WithingsRefreshTokenSecretName, null, It.IsAny<CancellationToken>()))
                .ReturnsAsync(Response.FromValue(new KeyVaultSecret(WithingsRefreshTokenSecretName, mockRefreshToken), new Mock<Response>().Object));
            _mockSecretClient.Setup(x => x.GetSecretAsync(WithingsClientIdSecretName, null, It.IsAny<CancellationToken>()))
                .ReturnsAsync(Response.FromValue(new KeyVaultSecret(WithingsClientIdSecretName, mockClientId), new Mock<Response>().Object));
            _mockSecretClient.Setup(x => x.GetSecretAsync(WithingsClientSecretSecretName, null, It.IsAny<CancellationToken>()))
                .ReturnsAsync(Response.FromValue(new KeyVaultSecret(WithingsClientSecretSecretName, mockClientSecret), new Mock<Response>().Object));
        }

        private void SetupHttpMessageHandlerMock(WithingsTokenResponse mockResponse)
        {
            _mockHttpMessageHandler.Protected()
                .Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>())
                .ReturnsAsync(new HttpResponseMessage
                {
                    StatusCode = HttpStatusCode.OK,
                    Content = new StringContent(JsonSerializer.Serialize(mockResponse))
                });
        }

        private static WithingsTokenResponse CreateSuccessfulWithingsResponse()
        {
            return new WithingsTokenResponse
            {
                Status = 0,
                Body = new WithingsTokenBody
                {
                    AccessToken = "test_withings_access_token",
                    RefreshToken = "test_withings_refresh_token",
                    ExpiresIn = 10800,
                    Scope = "user.metrics",
                    TokenType = "Bearer"
                }
            };
        }
    }
}
