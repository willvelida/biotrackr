using AutoFixture;
using Biotrackr.Auth.Svc.Models;
using Biotrackr.Auth.Svc.Services.Interfaces;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Moq;

namespace Biotrackr.Auth.Svc.UnitTests.WorkerTests
{
    public class AuthWorkerShould
    {
        private readonly Mock<IRefreshTokenService> _mockRefreshTokenService;
        private readonly Mock<ILogger<AuthWorker>> _mockLogger;
        private readonly Mock<IHostApplicationLifetime> _mockAppLifeTime;
        private readonly AuthWorker _sut;

        public AuthWorkerShould()
        {
            _mockRefreshTokenService = new Mock<IRefreshTokenService>();
            _mockLogger = new Mock<ILogger<AuthWorker>>();
            _mockAppLifeTime = new Mock<IHostApplicationLifetime>();
            _sut = new AuthWorker(_mockRefreshTokenService.Object, _mockLogger.Object, _mockAppLifeTime.Object);
        }

        [Fact]
        public async Task RefreshAndSaveTokensSuccessfullyWhenExecuteAsyncIsCalled()
        {
            // Arrange
            var fixture = new Fixture();
            var mockRefreshTokenResponse = fixture.Create<RefreshTokenResponse>();
            var completionSource = new TaskCompletionSource<bool>();

            _mockRefreshTokenService.Setup(s => s.RefreshTokens())
                .ReturnsAsync(mockRefreshTokenResponse);

            _mockRefreshTokenService.Setup(s => s.SaveTokens(mockRefreshTokenResponse))
                .Returns(Task.CompletedTask);

            _mockAppLifeTime.Setup(l => l.StopApplication())
                .Callback(() => completionSource.SetResult(true));

            // Act
            await _sut.StartAsync(CancellationToken.None);

            await completionSource.Task.WaitAsync(TimeSpan.FromSeconds(5)); // Wait for the task to complete

            await _sut.StopAsync(CancellationToken.None);

            // Assert
            _mockRefreshTokenService.Verify(s => s.RefreshTokens(), Times.Once);
            _mockRefreshTokenService.Verify(s => s.SaveTokens(mockRefreshTokenResponse), Times.Once);
            _mockAppLifeTime.Verify(l => l.StopApplication(), Times.Once);
        }

        [Fact]
        public async Task LogErrorAndStopApplicationWhenRefreshTokensThrowsException()
        {
            // Arrange
            var completionSource = new TaskCompletionSource<bool>();
            var testException = new Exception("Test exception");

            _mockRefreshTokenService.Setup(s => s.RefreshTokens())
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
            var fixture = new Fixture();
            var mockRefreshTokenResponse = fixture.Create<RefreshTokenResponse>();
            var completionSource = new TaskCompletionSource<bool>();
            var testException = new Exception("Test exception");

            _mockRefreshTokenService.Setup(s => s.RefreshTokens())
                .ReturnsAsync(mockRefreshTokenResponse);
            _mockRefreshTokenService.Setup(s => s.SaveTokens(mockRefreshTokenResponse))
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
            var fixture = new Fixture();
            var mockRefreshTokenResponse = fixture.Create<RefreshTokenResponse>();
            var completionSource = new TaskCompletionSource<bool>();

            _mockRefreshTokenService.Setup(s => s.RefreshTokens())
                .ReturnsAsync(mockRefreshTokenResponse);
            _mockRefreshTokenService.Setup(s => s.SaveTokens(mockRefreshTokenResponse))
                .Returns(Task.CompletedTask);
            _mockAppLifeTime.Setup(l => l.StopApplication())
                .Callback(() => completionSource.SetResult(true));

            // Act
            await _sut.StartAsync(CancellationToken.None);
            await completionSource.Task.WaitAsync(TimeSpan.FromSeconds(5));
            await _sut.StopAsync(CancellationToken.None);

            // Assert
            _mockLogger.VerifyLog(l => l.LogInformation(It.Is<string>(s => s.Contains("Attempting to refresh FitBit Tokens"))), Times.Once);
            _mockLogger.VerifyLog(l => l.LogInformation(It.Is<string>(s => s.Contains("FitBit Tokens refresh successful"))), Times.Once);
            _mockLogger.VerifyLog(l => l.LogInformation(It.Is<string>(s => s.Contains("FitBit Tokens saved successfully"))), Times.Once);
        }

        [Fact]
        public async Task HandleCancellationTokenProperly()
        {
            // Arrange
            using var cts = new CancellationTokenSource();
            var completionSource = new TaskCompletionSource<bool>();

            _mockRefreshTokenService.Setup(s => s.RefreshTokens())
                .Returns(async () =>
                {
                    await Task.Delay(100, cts.Token); // This will throw when cancelled
                    return new RefreshTokenResponse();
                });

            _mockAppLifeTime.Setup(l => l.StopApplication())
                .Callback(() => completionSource.SetResult(true));

            // Act
            await _sut.StartAsync(cts.Token);
            cts.Cancel(); // Cancel the operation

            await completionSource.Task.WaitAsync(TimeSpan.FromSeconds(5));
            await _sut.StopAsync(CancellationToken.None);

            // Assert
            _mockAppLifeTime.Verify(l => l.StopApplication(), Times.Once);
        }
    }
}
