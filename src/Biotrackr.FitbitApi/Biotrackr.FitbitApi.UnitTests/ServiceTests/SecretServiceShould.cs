using Azure;
using Azure.Security.KeyVault.Secrets;
using Biotrackr.FitbitApi.Services;
using Biotrackr.FitbitApi.Services.Interfaces;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;

namespace Biotrackr.FitbitApi.UnitTests.ServiceTests
{
    public class SecretServiceShould
    {
        private readonly Mock<SecretClient> _mockSecretClient;
        private readonly Mock<ILogger<SecretService>> _mockLogger;
        private readonly ISecretService _secretService;

        public SecretServiceShould()
        {
            _mockSecretClient = new Mock<SecretClient>();
            _mockLogger = new Mock<ILogger<SecretService>>();
            _secretService = new SecretService(_mockSecretClient.Object, _mockLogger.Object);
        }

        [Fact]
        public async Task GetSecretAsync_ShouldReturnSecretValue_WhenSecretExists()
        {
            // Arrange
            var secretName = "test-secret";
            var secretValue = "secret-value";
            var keyVaultSecret = new KeyVaultSecret(secretName, secretValue);
            _mockSecretClient.Setup(client => client.GetSecretAsync(secretName, null, default))
                             .ReturnsAsync(Response.FromValue(keyVaultSecret, Mock.Of<Response>()));

            // Act
            var result = await _secretService.GetSecretAsync(secretName);

            // Assert
            result.Should().Be(secretValue);
        }

        [Fact]
        public async Task GetSecretAsync_ShouldLogErrorAndThrow_WhenExceptionIsThrown()
        {
            // Arrange
            var secretName = "test-secret";
            var exceptionMessage = "An error occurred";
            _mockSecretClient.Setup(client => client.GetSecretAsync(secretName, null, default))
                             .ThrowsAsync(new RequestFailedException(exceptionMessage));

            // Act
            Func<Task> act = async () => await _secretService.GetSecretAsync(secretName);

            // Assert
            await act.Should().ThrowAsync<Exception>();
            _mockLogger.VerifyLog(logger => logger.LogError($"Exception thrown in GetSecretAsync: {exceptionMessage}"));
        }
    }
}
