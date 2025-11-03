using AutoFixture;
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

[Collection(nameof(IntegrationTestCollection))]
public class RefreshTokenServiceTests
{
    private readonly IntegrationTestFixture _fixture;
    private readonly Fixture _autoFixture;

    public RefreshTokenServiceTests(IntegrationTestFixture fixture)
    {
        _fixture = fixture;
        _autoFixture = new Fixture();
    }

    [Fact]
    public async Task RefreshesTokensEndToEndWithMockedDependencies()
    {
        // Arrange - Configure mocked dependencies
        var expectedResponse = TestDataGenerator.CreateRefreshTokenResponse();
        var refreshToken = TestDataGenerator.CreateRefreshToken();
        var fitbitCredentials = TestDataGenerator.CreateFitbitCredentials();

        // Setup SecretClient mocks
        _fixture.MockSecretClient
            .Setup(x => x.GetSecretAsync("RefreshToken", null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Response.FromValue(new KeyVaultSecret("RefreshToken", refreshToken), Mock.Of<Response>()));
        
        _fixture.MockSecretClient
            .Setup(x => x.GetSecretAsync("FitbitCredentials", null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Response.FromValue(new KeyVaultSecret("FitbitCredentials", fitbitCredentials), Mock.Of<Response>()));

        // Setup HttpMessageHandler mock
        _fixture.MockHttpMessageHandler.Protected()
            .Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent(JsonSerializer.Serialize(expectedResponse))
            });

        var service = _fixture.ServiceProvider.GetRequiredService<IRefreshTokenService>();

        // Act
        var result = await service.RefreshTokens();

        // Assert
        result.Should().NotBeNull();
        result.AccessToken.Should().Be(expectedResponse.AccessToken);
        result.RefreshToken.Should().Be(expectedResponse.RefreshToken);
        result.ExpiresIn.Should().Be(expectedResponse.ExpiresIn);
    }

    [Fact]
    public async Task SavesTokensEndToEndWithMockedSecretClient()
    {
        // Arrange
        var tokens = TestDataGenerator.CreateRefreshTokenResponse();
        
        _fixture.MockSecretClient
            .Setup(x => x.SetSecretAsync("AccessToken", tokens.AccessToken, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Response.FromValue(new KeyVaultSecret("AccessToken", tokens.AccessToken), Mock.Of<Response>()));
        
        _fixture.MockSecretClient
            .Setup(x => x.SetSecretAsync("RefreshToken", tokens.RefreshToken, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Response.FromValue(new KeyVaultSecret("RefreshToken", tokens.RefreshToken), Mock.Of<Response>()));

        var service = _fixture.ServiceProvider.GetRequiredService<IRefreshTokenService>();

        // Act
        await service.SaveTokens(tokens);

        // Assert - Verify secrets were saved
        _fixture.MockSecretClient.Verify(
            x => x.SetSecretAsync("AccessToken", tokens.AccessToken, It.IsAny<CancellationToken>()),
            Times.Once);
        
        _fixture.MockSecretClient.Verify(
            x => x.SetSecretAsync("RefreshToken", tokens.RefreshToken, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task ThrowsExceptionWhenSecretNotFoundInE2EWorkflow()
    {
        // Arrange
        _fixture.MockSecretClient
            .Setup(x => x.GetSecretAsync("RefreshToken", null, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new RequestFailedException(404, "Secret not found"));

        var service = _fixture.ServiceProvider.GetRequiredService<IRefreshTokenService>();

        // Act & Assert
        await Assert.ThrowsAsync<RequestFailedException>(async () => await service.RefreshTokens());
    }

    [Fact]
    public async Task ThrowsExceptionWhenFitbitAPIReturnsErrorInE2EWorkflow()
    {
        // Arrange
        var refreshToken = TestDataGenerator.CreateRefreshToken();
        var fitbitCredentials = TestDataGenerator.CreateFitbitCredentials();

        _fixture.MockSecretClient
            .Setup(x => x.GetSecretAsync("RefreshToken", null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Response.FromValue(new KeyVaultSecret("RefreshToken", refreshToken), Mock.Of<Response>()));
        
        _fixture.MockSecretClient
            .Setup(x => x.GetSecretAsync("FitbitCredentials", null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Response.FromValue(new KeyVaultSecret("FitbitCredentials", fitbitCredentials), Mock.Of<Response>()));

        _fixture.MockHttpMessageHandler.Protected()
            .Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.Unauthorized,
                Content = new StringContent("Invalid refresh token")
            });

        var service = _fixture.ServiceProvider.GetRequiredService<IRefreshTokenService>();

        // Act & Assert
        await Assert.ThrowsAsync<HttpRequestException>(async () => await service.RefreshTokens());
    }

    [Fact]
    public async Task HandlesRateLimitingGracefullyInE2EWorkflow()
    {
        // Arrange
        var refreshToken = TestDataGenerator.CreateRefreshToken();
        var fitbitCredentials = TestDataGenerator.CreateFitbitCredentials();

        _fixture.MockSecretClient
            .Setup(x => x.GetSecretAsync("RefreshToken", null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Response.FromValue(new KeyVaultSecret("RefreshToken", refreshToken), Mock.Of<Response>()));
        
        _fixture.MockSecretClient
            .Setup(x => x.GetSecretAsync("FitbitCredentials", null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Response.FromValue(new KeyVaultSecret("FitbitCredentials", fitbitCredentials), Mock.Of<Response>()));

        _fixture.MockHttpMessageHandler.Protected()
            .Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.TooManyRequests,
                Content = new StringContent("Rate limit exceeded")
            });

        var service = _fixture.ServiceProvider.GetRequiredService<IRefreshTokenService>();

        // Act & Assert
        var exception = await Assert.ThrowsAsync<HttpRequestException>(async () => await service.RefreshTokens());
        exception.StatusCode.Should().Be(HttpStatusCode.TooManyRequests);
    }
}
