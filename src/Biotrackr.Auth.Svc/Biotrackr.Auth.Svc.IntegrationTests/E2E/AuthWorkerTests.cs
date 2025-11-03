using Azure;
using Azure.Security.KeyVault.Secrets;
using Biotrackr.Auth.Svc.IntegrationTests.Collections;
using Biotrackr.Auth.Svc.IntegrationTests.Fixtures;
using Biotrackr.Auth.Svc.IntegrationTests.Helpers;
using Biotrackr.Auth.Svc.Models;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Moq;
using Moq.Protected;
using System.Net;
using System.Text.Json;
using Xunit;

namespace Biotrackr.Auth.Svc.IntegrationTests.E2E;

[Collection(nameof(IntegrationTestCollection))]
public class AuthWorkerTests
{
    private readonly IntegrationTestFixture _fixture;

    public AuthWorkerTests(IntegrationTestFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task ExecutesCompleteWorkflowEndToEndWithMockedDependencies()
    {
        // Arrange
        var expectedResponse = TestDataGenerator.CreateRefreshTokenResponse();
        var refreshToken = TestDataGenerator.CreateRefreshToken();
        var fitbitCredentials = TestDataGenerator.CreateFitbitCredentials();

        // Setup SecretClient for GetSecret calls
        _fixture.MockSecretClient
            .Setup(x => x.GetSecretAsync("RefreshToken", null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Response.FromValue(new KeyVaultSecret("RefreshToken", refreshToken), Mock.Of<Response>()));
        
        _fixture.MockSecretClient
            .Setup(x => x.GetSecretAsync("FitbitCredentials", null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Response.FromValue(new KeyVaultSecret("FitbitCredentials", fitbitCredentials), Mock.Of<Response>()));

        // Setup HttpMessageHandler for Fitbit API call
        _fixture.MockHttpMessageHandler.Protected()
            .Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent(JsonSerializer.Serialize(expectedResponse))
            });

        // Setup SecretClient for SetSecret calls
        _fixture.MockSecretClient
            .Setup(x => x.SetSecretAsync("AccessToken", It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((string name, string value, CancellationToken ct) =>
                Response.FromValue(new KeyVaultSecret(name, value), Mock.Of<Response>()));
        
        _fixture.MockSecretClient
            .Setup(x => x.SetSecretAsync("RefreshToken", It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((string name, string value, CancellationToken ct) =>
                Response.FromValue(new KeyVaultSecret(name, value), Mock.Of<Response>()));

        var worker = _fixture.ServiceProvider.GetRequiredService<IHostedService>();

        // Act - Start worker and let it run briefly
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(3));
        await worker.StartAsync(CancellationToken.None);
        
        try
        {
            // Give worker time to execute one iteration
            await Task.Delay(1500, cts.Token);
        }
        catch (OperationCanceledException)
        {
            // Expected
        }
        finally
        {
            await worker.StopAsync(CancellationToken.None);
        }

        // Assert - Verify the workflow executed (secrets were saved)
        _fixture.MockSecretClient.Verify(
            x => x.SetSecretAsync("AccessToken", expectedResponse.AccessToken, It.IsAny<CancellationToken>()),
            Times.AtLeastOnce());
    }

    [Fact]
    public async Task HandlesServiceErrorsGracefullyInE2EWorkflow()
    {
        // Arrange - Setup SecretClient to throw exception
        _fixture.MockSecretClient
            .Setup(x => x.GetSecretAsync("RefreshToken", null, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new RequestFailedException(500, "Key Vault unavailable"));

        var worker = _fixture.ServiceProvider.GetRequiredService<IHostedService>();

        // Act - Start worker and verify it handles errors gracefully
        await worker.StartAsync(CancellationToken.None);
        
        // Give worker time to attempt execution and handle error
        await Task.Delay(1500);
        
        // Stop worker
        var stopAction = async () => await worker.StopAsync(CancellationToken.None);

        // Assert - Worker should stop gracefully without unhandled exceptions
        await stopAction.Should().NotThrowAsync("Worker should handle service errors gracefully");
    }
}
