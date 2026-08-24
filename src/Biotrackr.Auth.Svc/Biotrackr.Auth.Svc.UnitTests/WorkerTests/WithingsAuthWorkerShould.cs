using AutoFixture;
using Biotrackr.Auth.Svc.Models;
using Biotrackr.Auth.Svc.Services.Interfaces;
using FluentAssertions;
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
        public async Task ExecuteAsync_ShouldRefreshAndSaveTokens_WhenRefreshTokenServiceSucceeds()
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
        public async Task ExecuteAsync_ShouldLogErrorAndStopApplication_WhenRefreshTokensThrows()
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
        public async Task ExecuteAsync_ShouldLogErrorAndStopApplication_WhenSaveTokensThrows()
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
        public async Task ExecuteAsync_ShouldLogInformationMessagesInOrder_WhenWorkflowSucceeds()
        {
            // Arrange
            var mockWithingsTokenResponse = CreateSuccessfulWithingsResponse();
            var completionSource = new TaskCompletionSource<bool>();
            var informationMessages = new List<string>();

            _mockLogger.Setup(l => l.Log(
                    LogLevel.Information,
                    It.IsAny<EventId>(),
                    It.IsAny<It.IsAnyType>(),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()))
                .Callback(new InvocationAction(invocation => informationMessages.Add(invocation.Arguments[2].ToString()!)));

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
            informationMessages.Should().HaveCount(3,
                "AGENT FIX: WithingsAuthWorker.ExecuteAsync must log exactly three information messages "
                + "(refresh attempt, refresh success, save success) on the happy path.");
            informationMessages[0].Should().Contain("Attempting to refresh Withings Tokens",
                "AGENT FIX: the first information log must announce the refresh attempt.");
            informationMessages[1].Should().Contain("Withings Tokens refresh successful",
                "AGENT FIX: the second information log must follow the refresh and precede the save.");
            informationMessages[2].Should().Contain("Withings Tokens saved successfully",
                "AGENT FIX: the third information log must confirm the save completed last.");
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
