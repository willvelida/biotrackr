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

        public RefreshTokenServiceShould()
        {
            _mockSecretClient = new Mock<SecretClient>();
            _mockHttpMessageHandler = new Mock<HttpMessageHandler>();
            _mockLogger = new Mock<ILogger<RefreshTokenService>>();

            var httpClient = new HttpClient(_mockHttpMessageHandler.Object);
            _sut = new RefreshTokenService(_mockSecretClient.Object, httpClient, _mockLogger.Object);
        }

        [Fact]
        public async Task RefreshTokensWhenRefreshTokensIsCalled()
        {
            // ARRANGE
            var fixture = new Fixture();
            var mockRefreshTokenResponse = fixture.Create<RefreshTokenResponse>();
            var mockFitbitRefreshToken = "testFitbitRefreshToken";
            var mockFitbitCredential = "testFitbitCredential";

            SetupSecretClientMocks(mockFitbitRefreshToken, mockFitbitCredential);
            SetupHttpMessageHandlerMock(mockRefreshTokenResponse);

            // ACT
            var result = await _sut.RefreshTokens();

            // ASSERT
            result.AccessToken.Should().Be(mockRefreshTokenResponse.AccessToken);
            result.RefreshToken.Should().Be(mockRefreshTokenResponse.RefreshToken);
        }

        [Fact]
        public async Task SaveRefreshTokensWhenSaveTokensIsCalled()
        {
            // ARRANGE
            var fixture = new Fixture();
            var mockTokenResponse = fixture.Create<RefreshTokenResponse>();

            _mockSecretClient.Setup(x => x.SetSecretAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new Mock<Response<KeyVaultSecret>>().Object);

            // ACT
            Func<Task> saveTokenAction = async () => await _sut.SaveTokens(mockTokenResponse);

            // ASSERT
            await saveTokenAction.Should().NotThrowAsync<Exception>();
            _mockSecretClient.Verify(x => x.SetSecretAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Exactly(2));
        }

        [Fact]
        public async Task CatchAndThrowExceptionWhenGetSecretFailsInRefreshTokens()
        {
            // ARRANGE
            _mockSecretClient.Setup(x => x.GetSecretAsync(It.IsAny<string>(), null, It.IsAny<CancellationToken>())).ThrowsAsync(new Exception());

            // ACT
            Func<Task> refreshTokenAction = async () => await _sut.RefreshTokens();

            // ASSERT
            await refreshTokenAction.Should().ThrowAsync<Exception>();
        }

        [Fact]
        public async Task CatchAndThrowExceptionWhenSaveSecretFailsInSaveTokens()
        {
            // ARRANGE
            var fixture = new Fixture();
            var mockTokenResponse = fixture.Create<RefreshTokenResponse>();
            _mockSecretClient.Setup(x => x.SetSecretAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ThrowsAsync(new Exception());

            // ACT
            Func<Task> refreshTokenAction = async () => await _sut.SaveTokens(mockTokenResponse);

            // ASSERT
            await refreshTokenAction.Should().ThrowAsync<Exception>();
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
