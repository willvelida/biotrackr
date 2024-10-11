using AutoFixture;
using Azure;
using Azure.Security.KeyVault.Secrets;
using Biotrackr.Auth.Models;
using Biotrackr.Auth.Services;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using Moq.Protected;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace Biotrackr.Auth.UnitTests.ServiceTests
{
    public class RefreshTokenServiceShould
    {
        private Mock<SecretClient> _mockSecretClient;
        private Mock<HttpMessageHandler> _mockHttpMessageHandler;
        private Mock<HttpClient> _mockHttpClient;
        private Mock<ILogger<RefreshTokenService>> _mockLogger;

        private RefreshTokenService _sut;
        public RefreshTokenServiceShould()
        {
            _mockSecretClient = new Mock<SecretClient>();
            _mockHttpMessageHandler = new Mock<HttpMessageHandler>();
            _mockHttpClient = new Mock<HttpClient>(_mockHttpMessageHandler.Object);
            _mockLogger = new Mock<ILogger<RefreshTokenService>>();

            _sut = new RefreshTokenService(
                _mockSecretClient.Object,
                _mockHttpClient.Object,
                _mockLogger.Object);
        }

        [Fact]
        public async Task RefreshTokensWhenRefreshTokensIsCalled()
        {
            // ARRANGE
            var fixture = new Fixture();
            var mockRefreshTokenResponse = fixture.Create<RefreshTokenResponse>();
            var mockFitbitRefreshToken = "testFitbitRefreshToken";
            var mockFitbitCredential = "testFitbitCredential";

            _mockSecretClient.Setup(x => x.GetSecretAsync("RefreshToken", null, It.IsAny<CancellationToken>()))
                .ReturnsAsync(Response.FromValue(new KeyVaultSecret("RefreshToken", mockFitbitRefreshToken), new Mock<Response>().Object));
            _mockSecretClient.Setup(x => x.GetSecretAsync("FitbitCredentials", null, It.IsAny<CancellationToken>()))
                .ReturnsAsync(Response.FromValue(new KeyVaultSecret("FitbitCredentials", mockFitbitCredential), new Mock<Response>().Object));

            _mockHttpMessageHandler.Protected()
                .Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<System.Threading.CancellationToken>())
                .ReturnsAsync(new HttpResponseMessage
                {
                    StatusCode = System.Net.HttpStatusCode.OK,
                    Content = new StringContent(JsonSerializer.Serialize(mockRefreshTokenResponse))
                });

            // ACT
            var result = await _sut.RefreshTokens();

            // ASSERT
            Assert.Equal(mockRefreshTokenResponse.AccessToken, result.AccessToken);
            Assert.Equal(mockRefreshTokenResponse.RefreshToken, result.RefreshToken);
        }

        [Fact]
        public async Task SaveRefreshTokensWhenSaveTokensIsCalled()
        {
            // ARRANGE
            var fixture = new Fixture();
            var mockTokenResponse = fixture.Create<RefreshTokenResponse>();

            _mockSecretClient.Setup(x => x.SetSecretAsync("RefreshToken", mockTokenResponse.RefreshToken, It.IsAny<CancellationToken>()))
                .ReturnsAsync(new Mock<Response<KeyVaultSecret>>().Object);

            _mockSecretClient.Setup(x => x.SetSecretAsync("AccessToken", mockTokenResponse.AccessToken, It.IsAny<CancellationToken>()))
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
    }
}
