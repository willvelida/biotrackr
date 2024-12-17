using Biotrackr.Activity.Svc.Services.Interfaces;
using Biotrackr.Activity.Svc.Workers;
using Microsoft.Extensions.Hosting;
using System.Reflection;

namespace Biotrackr.Activity.Svc.UnitTests.WorkerTests
{
    public class ActivityWorkerShould
    {
        private readonly Mock<IFitbitService> _fitbitServiceMock;
        private readonly Mock<IActivityService> _activityServiceMock;
        private readonly Mock<ILogger<ActivityWorker>> _loggerMock;
        private readonly Mock<IHostApplicationLifetime> _appLifetimeMock;

        private ActivityWorker _sut;

        public ActivityWorkerShould()
        {
            _fitbitServiceMock = new Mock<IFitbitService>();
            _activityServiceMock = new Mock<IActivityService>();
            _loggerMock = new Mock<ILogger<ActivityWorker>>();
            _appLifetimeMock = new Mock<IHostApplicationLifetime>();

            _sut = new ActivityWorker(_fitbitServiceMock.Object, _activityServiceMock.Object, _loggerMock.Object, _appLifetimeMock.Object);
        }

        [Fact]
        public void ExecuteAsync_ShouldLogError_WhenFitbitServiceThrowsException()
        {
            // Arrange
            var date = DateTime.Now.AddDays(-1).ToString("yyyy-MM-dd");
            _fitbitServiceMock.Setup(x => x.GetActivityResponse(date)).ThrowsAsync(new Exception("Test exception"));
            var executeMethod = typeof(ActivityWorker).GetMethod("ExecuteAsync", BindingFlags.NonPublic | BindingFlags.Instance);

            // Act
            var result = executeMethod.Invoke(_sut, new object[] { CancellationToken.None });

            // Assert
            _loggerMock.VerifyLog(logger => logger.LogError($"Exception thrown in {nameof(ActivityWorker)}: Test exception"));
            _appLifetimeMock.Verify(x => x.StopApplication(), Times.Once);
        }

        [Fact]
        public void ExecuteAsync_ShouldLogError_WhenActivityServiceThrowsException()
        {
            // Arrange
            var date = DateTime.Now.AddDays(-1).ToString("yyyy-MM-dd");
            _fitbitServiceMock.Setup(x => x.GetActivityResponse(date)).ReturnsAsync(new ActivityResponse());
            _activityServiceMock.Setup(x => x.MapAndSaveDocument(date, It.IsAny<ActivityResponse>())).ThrowsAsync(new Exception("Test exception"));
            var executeMethod = typeof(ActivityWorker).GetMethod("ExecuteAsync", BindingFlags.NonPublic | BindingFlags.Instance);

            // Act
            var result = executeMethod.Invoke(_sut, new object[] { CancellationToken.None });

            // Assert
            _loggerMock.VerifyLog(logger => logger.LogError($"Exception thrown in {nameof(ActivityWorker)}: Test exception"));
            _appLifetimeMock.Verify(x => x.StopApplication(), Times.Once);
        }

        [Fact]
        public async Task ExecuteAsync_ShouldReturn1_WhenSuccessful()
        {
            // Arrange
            var date = DateTime.Now.AddDays(-1).ToString("yyyy-MM-dd");
            _fitbitServiceMock.Setup(x => x.GetActivityResponse(date)).ReturnsAsync(new ActivityResponse());
            _activityServiceMock.Setup(x => x.MapAndSaveDocument(date, It.IsAny<ActivityResponse>()));
            var executeMethod = typeof(ActivityWorker).GetMethod("ExecuteAsync", BindingFlags.NonPublic | BindingFlags.Instance);

            // Act
            Func<Task> activityWorkerAction = async () => executeMethod.Invoke(_sut, new object[] { CancellationToken.None });

            // Assert
            await activityWorkerAction.Should().NotThrowAsync();
            _appLifetimeMock.Verify(x => x.StopApplication(), Times.Once);
        }
    }
}
