using AutoFixture;
using Biotrackr.Auth.Svc.Models;
using Biotrackr.Auth.Svc.Services.Interfaces;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Moq;

namespace Biotrackr.Auth.Svc.UnitTests.WorkerTests
{
    public class WithingsAuthWorkerShould
    {
        private readonly Mock<IWithingsRefreshTokenService> _mockWithingsRefreshTokenService;
        private readonly Mock<ILogger<WithingsAuthWorker>> _mockLogger;
        private readonly Mock<IHostApplicationLifetime> _mockAppLifeTime;
        private readonly WithingsAuthWorker _sut;

        public WithingsAuthWorkerShould()
        {
            _mockWithingsRefreshTokenService = new Mock<IWithingsRefreshTokenService>();
            _mockLogger = new Mock<ILogger<WithingsAuthWorker>>();
            _mockAppLifeTime = new Mock<IHostApplicationLifetime>();
            _sut = new WithingsAuthWorker(_mockWithingsRefreshTokenService.Object, _mockLogger.Object, _mockAppLifeTime.Object);
        }

        [Fact]
        public async Task RefreshAndSaveTokensSuccessfullyWhenExecuteAsyncIsCalled()
        {
            // Arrange
            var mockWithingsTokenResponse = CreateSuccessfulWithingsResponse();
            var completionSource = new TaskCompletionSource<bool>();

            _mockWithingsRefreshTokenService.Setup(s => s.RefreshTokens())
                .ReturnsAsync(mockWithingsTokenResponse);

            _mockWithingsRefreshTokenService.Setup(s => s.SaveTokens(mockWithingsTokenResponse))
                .Returns(Task.CompletedTask);

            _mockAppLifeTime.Setup(l => l.StopApplication())
                .Callback(() => completionSource.SetResult(true));

            // Act
            await _sut.StartAsync(CancellationToken.None);

            await completionSource.Task.WaitAsync(TimeSpan.FromSeconds(5));

            await _sut.StopAsync(CancellationToken.None);

            // Assert
            _mockWithingsRefreshTokenService.Verify(s => s.RefreshTokens(), Times.Once);
            _mockWithingsRefreshTokenService.Verify(s => s.SaveTokens(mockWithingsTokenResponse), Times.Once);
            _mockAppLifeTime.Verify(l => l.StopApplication(), Times.Once);
        }

        [Fact]
        public async Task LogErrorAndStopApplicationWhenRefreshTokensThrowsException()
        {
            // Arrange
            var completionSource = new TaskCompletionSource<bool>();
            var testException = new Exception("Test exception");

            _mockWithingsRefreshTokenService.Setup(s => s.RefreshTokens())
                .ThrowsAsync(testException);

            _mockAppLifeTime.Setup(l => l.StopApplication())
                .Callback(() => completionSource.SetResult(true));

            // Act
            await _sut.StartAsync(CancellationToken.None);
            await completionSource.Task.WaitAsync(TimeSpan.FromSeconds(5));
            await _sut.StopAsync(CancellationToken.None);

            // Assert
            _mockLogger.VerifyLog(logger => logger.LogError($"Exception thrown: {testException.Message}"), Times.Once);
            _mockAppLifeTime.Verify(l => l.StopApplication(), Times.Once);
        }

        [Fact]
        public async Task LogErrorAndStopApplicationWhenSaveTokensThrowsException()
        {
            // Arrange
            var mockWithingsTokenResponse = CreateSuccessfulWithingsResponse();
            var completionSource = new TaskCompletionSource<bool>();
            var testException = new Exception("Test exception");

            _mockWithingsRefreshTokenService.Setup(s => s.RefreshTokens())
                .ReturnsAsync(mockWithingsTokenResponse);
            _mockWithingsRefreshTokenService.Setup(s => s.SaveTokens(mockWithingsTokenResponse))
                .ThrowsAsync(testException);

            _mockAppLifeTime.Setup(l => l.StopApplication())
                .Callback(() => completionSource.SetResult(true));

            // Act
            await _sut.StartAsync(CancellationToken.None);
            await completionSource.Task.WaitAsync(TimeSpan.FromSeconds(5));
            await _sut.StopAsync(CancellationToken.None);

            // Assert
            _mockLogger.VerifyLog(l => l.LogError($"Exception thrown: {testException.Message}"), Times.Once);
            _mockAppLifeTime.Verify(l => l.StopApplication(), Times.Once);
        }

        [Fact]
        public async Task LogInformationMessagesInCorrectOrder()
        {
            // Arrange
            var mockWithingsTokenResponse = CreateSuccessfulWithingsResponse();
            var completionSource = new TaskCompletionSource<bool>();

            _mockWithingsRefreshTokenService.Setup(s => s.RefreshTokens())
                .ReturnsAsync(mockWithingsTokenResponse);
            _mockWithingsRefreshTokenService.Setup(s => s.SaveTokens(mockWithingsTokenResponse))
                .Returns(Task.CompletedTask);
            _mockAppLifeTime.Setup(l => l.StopApplication())
                .Callback(() => completionSource.SetResult(true));

            // Act
            await _sut.StartAsync(CancellationToken.None);
            await completionSource.Task.WaitAsync(TimeSpan.FromSeconds(5));
            await _sut.StopAsync(CancellationToken.None);

            // Assert
            _mockLogger.VerifyLog(l => l.LogInformation(It.Is<string>(s => s.Contains("Attempting to refresh Withings Tokens"))), Times.Once);
            _mockLogger.VerifyLog(l => l.LogInformation(It.Is<string>(s => s.Contains("Withings Tokens refresh successful"))), Times.Once);
            _mockLogger.VerifyLog(l => l.LogInformation(It.Is<string>(s => s.Contains("Withings Tokens saved successfully"))), Times.Once);
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
