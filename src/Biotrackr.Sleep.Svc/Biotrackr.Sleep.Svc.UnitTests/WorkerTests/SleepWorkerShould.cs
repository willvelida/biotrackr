using AutoFixture;
using Biotrackr.Sleep.Svc.Models.FitbitEntities;
using Biotrackr.Sleep.Svc.Services.Interfaces;
using Biotrackr.Sleep.Svc.Worker;
using FluentAssertions;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Moq;

namespace Biotrackr.Sleep.Svc.UnitTests.WorkerTests
{
    public class SleepWorkerShould
    {
        private readonly Mock<IFitbitService> _mockFitbitService;
        private readonly Mock<ISleepService> _mockSleepService;
        private readonly Mock<ILogger<SleepWorker>> _mockLogger;
        private readonly Mock<IHostApplicationLifetime> _mockAppLifetime;
        private readonly Fixture _fixture;

        public SleepWorkerShould()
        {
            _mockFitbitService = new Mock<IFitbitService>();
            _mockSleepService = new Mock<ISleepService>();
            _mockLogger = new Mock<ILogger<SleepWorker>>();
            _mockAppLifetime = new Mock<IHostApplicationLifetime>();
            _fixture = new Fixture();
        }

        [Fact]
        public void Constructor_ShouldInitialize_WithValidDependencies()
        {
            // Act
            var worker = new SleepWorker(
                _mockFitbitService.Object,
                _mockSleepService.Object,
                _mockLogger.Object,
                _mockAppLifetime.Object);

            // Assert
            worker.Should().NotBeNull();
        }

        [Fact]
        public async Task ExecuteAsync_ShouldReturn0_WhenSuccessful()
        {
            // Arrange
            var expectedDate = DateTime.Now.AddDays(-1).ToString("yyyy-MM-dd");
            var sleepResponse = _fixture.Create<SleepResponse>();
            
            _mockFitbitService
                .Setup(x => x.GetSleepResponse(expectedDate))
                .ReturnsAsync(sleepResponse);

            _mockSleepService
                .Setup(x => x.MapAndSaveDocument(expectedDate, sleepResponse))
                .Returns(Task.CompletedTask);

            var worker = new SleepWorker(
                _mockFitbitService.Object,
                _mockSleepService.Object,
                _mockLogger.Object,
                _mockAppLifetime.Object);

            var cancellationTokenSource = new CancellationTokenSource();

            // Act
            await worker.StartAsync(cancellationTokenSource.Token);
            await Task.Delay(100); // Allow background task to execute

            // Assert
            _mockFitbitService.Verify(
                x => x.GetSleepResponse(It.Is<string>(d => d == expectedDate)),
                Times.Once);

            _mockSleepService.Verify(
                x => x.MapAndSaveDocument(expectedDate, sleepResponse),
                Times.Once);

            _mockAppLifetime.Verify(
                x => x.StopApplication(),
                Times.Once);
        }

        [Fact]
        public async Task ExecuteAsync_ShouldReturn1_WhenExceptionThrown()
        {
            // Arrange
            var expectedDate = DateTime.Now.AddDays(-1).ToString("yyyy-MM-dd");
            var exceptionMessage = "Test exception";

            _mockFitbitService
                .Setup(x => x.GetSleepResponse(expectedDate))
                .ThrowsAsync(new Exception(exceptionMessage));

            var worker = new SleepWorker(
                _mockFitbitService.Object,
                _mockSleepService.Object,
                _mockLogger.Object,
                _mockAppLifetime.Object);

            var cancellationTokenSource = new CancellationTokenSource();

            // Act
            await worker.StartAsync(cancellationTokenSource.Token);
            await Task.Delay(100); // Allow background task to execute and handle exception

            // Assert
            _mockLogger.Verify(
                x => x.Log(
                    LogLevel.Error,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains(exceptionMessage)),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
                Times.Once);

            _mockAppLifetime.Verify(
                x => x.StopApplication(),
                Times.Once);
        }

        [Fact]
        public async Task ExecuteAsync_ShouldHandleCancellation_Gracefully()
        {
            // Arrange
            var cancellationTokenSource = new CancellationTokenSource();
            cancellationTokenSource.Cancel(); // Cancel immediately

            var worker = new SleepWorker(
                _mockFitbitService.Object,
                _mockSleepService.Object,
                _mockLogger.Object,
                _mockAppLifetime.Object);

            // Act
            await worker.StartAsync(cancellationTokenSource.Token);
            await Task.Delay(100); // Give time for cleanup

            // Assert - Should complete without throwing
            _mockAppLifetime.Verify(
                x => x.StopApplication(),
                Times.Once);
        }
    }
}
