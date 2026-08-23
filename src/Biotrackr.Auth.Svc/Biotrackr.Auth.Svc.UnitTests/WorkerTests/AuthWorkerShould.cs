using AutoFixture;
using Biotrackr.Auth.Svc.Models;
using Biotrackr.Auth.Svc.Services.Interfaces;
using FluentAssertions;
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
        public async Task ExecuteAsync_ShouldRefreshAndSaveTokens_WhenRefreshTokenServiceSucceeds()
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
        public async Task ExecuteAsync_ShouldLogErrorAndStopApplication_WhenRefreshTokensThrows()
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
        public async Task ExecuteAsync_ShouldLogErrorAndStopApplication_WhenSaveTokensThrows()
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
        public async Task ExecuteAsync_ShouldLogInformationMessagesInOrder_WhenWorkflowSucceeds()
        {
            // Arrange
            var fixture = new Fixture();
            var mockRefreshTokenResponse = fixture.Create<RefreshTokenResponse>();
            var completionSource = new TaskCompletionSource<bool>();
            var informationMessages = new List<string>();

            _mockLogger.Setup(l => l.Log(
                    LogLevel.Information,
                    It.IsAny<EventId>(),
                    It.IsAny<It.IsAnyType>(),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()))
                .Callback(new InvocationAction(invocation => informationMessages.Add(invocation.Arguments[2].ToString()!)));

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
            informationMessages.Should().HaveCount(3,
                "AGENT FIX: AuthWorker.ExecuteAsync must log exactly three information messages "
                + "(refresh attempt, refresh success, save success) on the happy path.");
            informationMessages[0].Should().Contain("Attempting to refresh FitBit Tokens",
                "AGENT FIX: the first information log must announce the refresh attempt.");
            informationMessages[1].Should().Contain("FitBit Tokens refresh successful",
                "AGENT FIX: the second information log must follow the refresh and precede the save.");
            informationMessages[2].Should().Contain("FitBit Tokens saved successfully",
                "AGENT FIX: the third information log must confirm the save completed last.");
        }
    }
}
