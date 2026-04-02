using Azure;
using Azure.Security.KeyVault.Secrets;
using Biotrackr.Auth.Svc.IntegrationTests.Collections;
using Biotrackr.Auth.Svc.IntegrationTests.Fixtures;
using Biotrackr.Auth.Svc.IntegrationTests.Helpers;
using Biotrackr.Auth.Svc.Models;
using Biotrackr.Auth.Svc.Services.Interfaces;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Moq.Protected;
using System.Net;
using System.Text.Json;
using Xunit;

namespace Biotrackr.Auth.Svc.IntegrationTests.E2E;

[Collection(nameof(WithingsIntegrationTestCollection))]
public class WithingsRefreshTokenServiceTests
{
    private readonly WithingsIntegrationTestFixture _fixture;

    public WithingsRefreshTokenServiceTests(WithingsIntegrationTestFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task RefreshesWithingsTokensEndToEndWithMockedDependencies()
    {
        // Arrange
        var expectedResponse = TestDataGenerator.CreateWithingsTokenResponse();
        var refreshToken = TestDataGenerator.CreateWithingsRefreshToken();
        var clientId = TestDataGenerator.CreateWithingsClientId();
        var clientSecret = TestDataGenerator.CreateWithingsClientSecret();

        _fixture.MockSecretClient
            .Setup(x => x.GetSecretAsync("WithingsRefreshToken", null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Response.FromValue(new KeyVaultSecret("WithingsRefreshToken", refreshToken), Mock.Of<Response>()));

        _fixture.MockSecretClient
            .Setup(x => x.GetSecretAsync("WithingsClientId", null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Response.FromValue(new KeyVaultSecret("WithingsClientId", clientId), Mock.Of<Response>()));

        _fixture.MockSecretClient
            .Setup(x => x.GetSecretAsync("WithingsClientSecret", null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Response.FromValue(new KeyVaultSecret("WithingsClientSecret", clientSecret), Mock.Of<Response>()));

        _fixture.MockHttpMessageHandler.Protected()
            .Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent(JsonSerializer.Serialize(expectedResponse))
            });

        var service = _fixture.ServiceProvider.GetRequiredService<IWithingsRefreshTokenService>();

        // Act
        var result = await service.RefreshTokens();

        // Assert
        result.Should().NotBeNull();
        result.Status.Should().Be(0);
        result.Body.Should().NotBeNull();
        result.Body!.AccessToken.Should().Be(expectedResponse.Body!.AccessToken);
        result.Body.RefreshToken.Should().Be(expectedResponse.Body.RefreshToken);
    }

    [Fact]
    public async Task SavesWithingsTokensEndToEndWithMockedSecretClient()
    {
        // Arrange
        var tokens = TestDataGenerator.CreateWithingsTokenResponse();

        _fixture.MockSecretClient
            .Setup(x => x.SetSecretAsync("WithingsAccessToken", tokens.Body!.AccessToken, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Response.FromValue(new KeyVaultSecret("WithingsAccessToken", tokens.Body.AccessToken), Mock.Of<Response>()));

        _fixture.MockSecretClient
            .Setup(x => x.SetSecretAsync("WithingsRefreshToken", tokens.Body.RefreshToken, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Response.FromValue(new KeyVaultSecret("WithingsRefreshToken", tokens.Body.RefreshToken), Mock.Of<Response>()));

        var service = _fixture.ServiceProvider.GetRequiredService<IWithingsRefreshTokenService>();

        // Act
        await service.SaveTokens(tokens);

        // Assert
        _fixture.MockSecretClient.Verify(
            x => x.SetSecretAsync("WithingsAccessToken", tokens.Body!.AccessToken, It.IsAny<CancellationToken>()),
            Times.Once);

        _fixture.MockSecretClient.Verify(
            x => x.SetSecretAsync("WithingsRefreshToken", tokens.Body.RefreshToken, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task ThrowsExceptionWhenWithingsSecretNotFound()
    {
        // Arrange
        _fixture.MockSecretClient
            .Setup(x => x.GetSecretAsync("WithingsRefreshToken", null, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new RequestFailedException(404, "Secret not found"));

        var service = _fixture.ServiceProvider.GetRequiredService<IWithingsRefreshTokenService>();

        // Act & Assert
        await Assert.ThrowsAsync<RequestFailedException>(async () => await service.RefreshTokens());
    }

    [Fact]
    public async Task ThrowsExceptionWhenWithingsAPIReturnsError()
    {
        // Arrange
        var refreshToken = TestDataGenerator.CreateWithingsRefreshToken();
        var clientId = TestDataGenerator.CreateWithingsClientId();
        var clientSecret = TestDataGenerator.CreateWithingsClientSecret();

        _fixture.MockSecretClient
            .Setup(x => x.GetSecretAsync("WithingsRefreshToken", null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Response.FromValue(new KeyVaultSecret("WithingsRefreshToken", refreshToken), Mock.Of<Response>()));

        _fixture.MockSecretClient
            .Setup(x => x.GetSecretAsync("WithingsClientId", null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Response.FromValue(new KeyVaultSecret("WithingsClientId", clientId), Mock.Of<Response>()));

        _fixture.MockSecretClient
            .Setup(x => x.GetSecretAsync("WithingsClientSecret", null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Response.FromValue(new KeyVaultSecret("WithingsClientSecret", clientSecret), Mock.Of<Response>()));

        _fixture.MockHttpMessageHandler.Protected()
            .Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.Unauthorized,
                Content = new StringContent("Unauthorized")
            });

        var service = _fixture.ServiceProvider.GetRequiredService<IWithingsRefreshTokenService>();

        // Act & Assert
        await Assert.ThrowsAsync<HttpRequestException>(async () => await service.RefreshTokens());
    }

    [Fact]
    public async Task ThrowsExceptionWhenWithingsAPIReturnsNonZeroStatus()
    {
        // Arrange
        var refreshToken = TestDataGenerator.CreateWithingsRefreshToken();
        var clientId = TestDataGenerator.CreateWithingsClientId();
        var clientSecret = TestDataGenerator.CreateWithingsClientSecret();
        var errorResponse = new WithingsTokenResponse { Status = 601, Body = null };

        _fixture.MockSecretClient
            .Setup(x => x.GetSecretAsync("WithingsRefreshToken", null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Response.FromValue(new KeyVaultSecret("WithingsRefreshToken", refreshToken), Mock.Of<Response>()));

        _fixture.MockSecretClient
            .Setup(x => x.GetSecretAsync("WithingsClientId", null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Response.FromValue(new KeyVaultSecret("WithingsClientId", clientId), Mock.Of<Response>()));

        _fixture.MockSecretClient
            .Setup(x => x.GetSecretAsync("WithingsClientSecret", null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Response.FromValue(new KeyVaultSecret("WithingsClientSecret", clientSecret), Mock.Of<Response>()));

        _fixture.MockHttpMessageHandler.Protected()
            .Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent(JsonSerializer.Serialize(errorResponse))
            });

        var service = _fixture.ServiceProvider.GetRequiredService<IWithingsRefreshTokenService>();

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(async () => await service.RefreshTokens());
    }
}
