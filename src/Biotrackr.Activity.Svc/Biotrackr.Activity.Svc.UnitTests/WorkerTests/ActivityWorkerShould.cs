using Biotrackr.Activity.Svc.Services.Interfaces;
using Biotrackr.Activity.Svc.Workers;
using Microsoft.Extensions.Hosting;
using System.Diagnostics;
using System.Globalization;
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

            _sut = new ActivityWorker(_fitbitServiceMock.Object, _activityServiceMock.Object, _loggerMock.Object, _appLifetimeMock.Object, TimeProvider.System);
        }

        /// <summary>
        /// A TimeProvider fixed at a chosen instant in a chosen time zone, so date
        /// arithmetic in the worker becomes deterministic and testable.
        /// </summary>
        private sealed class FixedTimeProvider : TimeProvider
        {
            private readonly DateTimeOffset _utcNow;
            private readonly TimeZoneInfo _localTimeZone;

            public FixedTimeProvider(DateTimeOffset utcNow, TimeSpan localOffset)
            {
                _utcNow = utcNow;
                var id = $"Fixed offset {localOffset}";
                _localTimeZone = TimeZoneInfo.CreateCustomTimeZone(id, localOffset, id, id);
            }

            public override DateTimeOffset GetUtcNow() => _utcNow;

            public override TimeZoneInfo LocalTimeZone => _localTimeZone;
        }

        /// <summary>
        /// Helper method to properly invoke the protected ExecuteAsync method
        /// </summary>
        private async Task<int> InvokeExecuteAsync(CancellationToken cancellationToken = default, ActivityWorker worker = null)
        {
            var executeMethod = typeof(ActivityWorker).GetMethod("ExecuteAsync", BindingFlags.NonPublic | BindingFlags.Instance);
            var task = (Task<int>)executeMethod.Invoke(worker ?? _sut, new object[] { cancellationToken });
            return await task;
        }

        #region Success Path Tests

        [Fact]
        public async Task ExecuteAsync_ShouldReturn0_WhenSuccessful()
        {
            // Arrange
            var expectedDate = DateTime.Now.AddDays(-1).ToString("yyyy-MM-dd");
            var activityResponse = new ActivityResponse();

            _fitbitServiceMock.Setup(x => x.GetActivityResponse(expectedDate))
                .ReturnsAsync(activityResponse);
            _activityServiceMock.Setup(x => x.MapAndSaveDocument(expectedDate, activityResponse))
                .Returns(Task.CompletedTask);

            // Act
            var result = await InvokeExecuteAsync();

            // Assert
            result.Should().Be(0);
            _fitbitServiceMock.Verify(x => x.GetActivityResponse(expectedDate), Times.Once);
            _activityServiceMock.Verify(x => x.MapAndSaveDocument(expectedDate, activityResponse), Times.Once);
            _appLifetimeMock.Verify(x => x.StopApplication(), Times.Once);
        }

        [Fact]
        public async Task ExecuteAsync_ShouldLogInformationMessages_WhenSuccessful()
        {
            // Arrange
            var expectedDate = DateTime.Now.AddDays(-1).ToString("yyyy-MM-dd");
            var activityResponse = new ActivityResponse();

            _fitbitServiceMock.Setup(x => x.GetActivityResponse(expectedDate))
                .ReturnsAsync(activityResponse);
            _activityServiceMock.Setup(x => x.MapAndSaveDocument(expectedDate, activityResponse))
                .Returns(Task.CompletedTask);

            // Act
            await InvokeExecuteAsync();

            // Assert
            _loggerMock.VerifyLog(logger => logger.LogInformation(It.Is<string>(s => s.StartsWith($"{nameof(ActivityWorker)} executed at:"))), Times.Once);
            _loggerMock.VerifyLog(logger => logger.LogInformation($"Getting activity response for date: {expectedDate}"), Times.Once);
            _loggerMock.VerifyLog(logger => logger.LogInformation($"Mapping and saving document for date: {expectedDate}"), Times.Once);
        }

        [Fact]
        public async Task ExecuteAsync_ShouldRequestYesterdaysDate_WhenInvoked()
        {
            // Arrange
            var expectedDate = DateTime.Now.AddDays(-1).ToString("yyyy-MM-dd");
            var activityResponse = new ActivityResponse();

            _fitbitServiceMock.Setup(x => x.GetActivityResponse(expectedDate))
                .ReturnsAsync(activityResponse);
            _activityServiceMock.Setup(x => x.MapAndSaveDocument(expectedDate, activityResponse))
                .Returns(Task.CompletedTask);

            // Act
            await InvokeExecuteAsync();

            // Assert
            _fitbitServiceMock.Verify(x => x.GetActivityResponse(expectedDate), Times.Once);
            _activityServiceMock.Verify(x => x.MapAndSaveDocument(expectedDate, activityResponse), Times.Once);
        }

        [Fact]
        public async Task ExecuteAsync_ShouldCallServicesInCorrectOrder_WhenSuccessful()
        {
            // Arrange
            var expectedDate = DateTime.Now.AddDays(-1).ToString("yyyy-MM-dd");
            var activityResponse = new ActivityResponse();
            var callOrder = new List<string>();

            _fitbitServiceMock.Setup(x => x.GetActivityResponse(expectedDate))
                .Returns(async () =>
                {
                    callOrder.Add("FitbitService");
                    await Task.CompletedTask;
                    return activityResponse;
                });

            _activityServiceMock.Setup(x => x.MapAndSaveDocument(expectedDate, activityResponse))
                .Returns(async () =>
                {
                    callOrder.Add("ActivityService");
                    await Task.CompletedTask;
                });

            // Act
            await InvokeExecuteAsync();

            // Assert
            callOrder.Should().Equal("FitbitService", "ActivityService");
        }

        #endregion

        #region Exception Handling Tests

        [Fact]
        public async Task ExecuteAsync_ShouldReturn1_WhenFitbitServiceThrowsException()
        {
            // Arrange
            var expectedDate = DateTime.Now.AddDays(-1).ToString("yyyy-MM-dd");
            var expectedException = new Exception("Test exception");
            _fitbitServiceMock.Setup(x => x.GetActivityResponse(expectedDate))
                .ThrowsAsync(expectedException);

            // Act
            var result = await InvokeExecuteAsync();

            // Assert
            result.Should().Be(1);
            _loggerMock.VerifyLog(logger => logger.LogError($"Exception thrown in {nameof(ActivityWorker)}: Test exception"), Times.Once);
            _appLifetimeMock.Verify(x => x.StopApplication(), Times.Once);
            _activityServiceMock.Verify(x => x.MapAndSaveDocument(It.IsAny<string>(), It.IsAny<ActivityResponse>()), Times.Never);
        }

        [Fact]
        public async Task ExecuteAsync_ShouldReturn1_WhenActivityServiceThrowsException()
        {
            // Arrange
            var expectedDate = DateTime.Now.AddDays(-1).ToString("yyyy-MM-dd");
            var activityResponse = new ActivityResponse();
            var expectedException = new Exception("Test exception");

            _fitbitServiceMock.Setup(x => x.GetActivityResponse(expectedDate))
                .ReturnsAsync(activityResponse);
            _activityServiceMock.Setup(x => x.MapAndSaveDocument(expectedDate, activityResponse))
                .ThrowsAsync(expectedException);

            // Act
            var result = await InvokeExecuteAsync();

            // Assert
            result.Should().Be(1);
            _loggerMock.VerifyLog(logger => logger.LogError($"Exception thrown in {nameof(ActivityWorker)}: Test exception"), Times.Once);
            _appLifetimeMock.Verify(x => x.StopApplication(), Times.Once);
            _fitbitServiceMock.Verify(x => x.GetActivityResponse(expectedDate), Times.Once);
        }

        [Fact]
        public async Task ExecuteAsync_ShouldNotCallActivityService_WhenFitbitServiceFails()
        {
            // Arrange
            var expectedDate = DateTime.Now.AddDays(-1).ToString("yyyy-MM-dd");
            _fitbitServiceMock.Setup(x => x.GetActivityResponse(expectedDate))
                .ThrowsAsync(new Exception("Fitbit failed"));

            // Act
            await InvokeExecuteAsync();

            // Assert
            _activityServiceMock.Verify(x => x.MapAndSaveDocument(It.IsAny<string>(), It.IsAny<ActivityResponse>()), Times.Never);
        }

        [Theory]
        [InlineData(typeof(HttpRequestException), "Network error")]
        [InlineData(typeof(TimeoutException), "Request timeout")]
        [InlineData(typeof(ArgumentException), "Invalid argument")]
        [InlineData(typeof(InvalidOperationException), "Invalid operation")]
        public async Task ExecuteAsync_ShouldReturn1AndLogError_WhenExceptionTypeIsThrown(Type exceptionType, string message)
        {
            // Arrange
            var expectedDate = DateTime.Now.AddDays(-1).ToString("yyyy-MM-dd");
            var exception = (Exception)Activator.CreateInstance(exceptionType, message);

            _fitbitServiceMock.Setup(x => x.GetActivityResponse(expectedDate))
                .ThrowsAsync(exception);

            // Act
            var result = await InvokeExecuteAsync();

            // Assert
            result.Should().Be(1);
            _loggerMock.VerifyLog(logger => logger.LogError($"Exception thrown in {nameof(ActivityWorker)}: {message}"), Times.Once);
            _appLifetimeMock.Verify(x => x.StopApplication(), Times.Once);
        }

        #endregion

        #region Edge Cases and Null Handling Tests

        [Fact]
        public async Task ExecuteAsync_ShouldReturn0_WhenActivityResponseIsNull()
        {
            // Arrange
            var expectedDate = DateTime.Now.AddDays(-1).ToString("yyyy-MM-dd");
            _fitbitServiceMock.Setup(x => x.GetActivityResponse(expectedDate))
                .ReturnsAsync((ActivityResponse)null);
            _activityServiceMock.Setup(x => x.MapAndSaveDocument(expectedDate, null))
                .Returns(Task.CompletedTask);

            // Act
            var result = await InvokeExecuteAsync();

            // Assert
            result.Should().Be(0);
            _activityServiceMock.Verify(x => x.MapAndSaveDocument(expectedDate, null), Times.Once);
        }

        [Theory]
        [InlineData("2024-02-29T13:00:00Z", 13, "2024-02-29")]  // Local date is already Mar 1st while UTC is still Feb 29th
        [InlineData("2024-01-01T02:00:00Z", -8, "2023-12-30")]  // Local day is still behind UTC, in the previous year
        [InlineData("2024-03-01T12:00:00Z", 0, "2024-02-29")]   // Leap year - Mar 1st to Feb 29th
        [InlineData("2024-12-31T12:00:00Z", 0, "2024-12-30")]   // New Year's Eve - Dec 31st to Dec 30th
        public async Task ExecuteAsync_ShouldRequestPreviousLocalDate_WhenClockIsAtADateBoundary(string utcNow, int localOffsetHours, string expectedPreviousDate)
        {
            // Arrange
            var timeProvider = new FixedTimeProvider(
                DateTimeOffset.Parse(utcNow, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal),
                TimeSpan.FromHours(localOffsetHours));
            var worker = new ActivityWorker(_fitbitServiceMock.Object, _activityServiceMock.Object, _loggerMock.Object, _appLifetimeMock.Object, timeProvider);
            var activityResponse = new ActivityResponse();

            _fitbitServiceMock.Setup(x => x.GetActivityResponse(expectedPreviousDate))
                .ReturnsAsync(activityResponse);
            _activityServiceMock.Setup(x => x.MapAndSaveDocument(expectedPreviousDate, activityResponse))
                .Returns(Task.CompletedTask);

            // Act
            var result = await InvokeExecuteAsync(worker: worker);

            // Assert
            result.Should().Be(0);
            _fitbitServiceMock.Verify(x => x.GetActivityResponse(expectedPreviousDate), Times.Once,
                $"AGENT FIX: ActivityWorker must request the day before the current LOCAL date ({expectedPreviousDate}) "
                + $"for a clock at {utcNow} in a UTC{localOffsetHours:+00;-00} zone. "
                + "Using DateTime.UtcNow instead of local time breaks this.");
            _activityServiceMock.Verify(x => x.MapAndSaveDocument(expectedPreviousDate, activityResponse), Times.Once);
        }

        #endregion

        #region Cancellation Token Tests

        [Fact]
        public async Task ExecuteAsync_ShouldStopApplication_WhenCancellationIsRequested()
        {
            // Arrange
            var cts = new CancellationTokenSource();
            var expectedDate = DateTime.Now.AddDays(-1).ToString("yyyy-MM-dd");

            _fitbitServiceMock.Setup(x => x.GetActivityResponse(expectedDate))
                .Returns(async () =>
                {
                    await Task.Delay(50); // Small delay
                    return new ActivityResponse();
                });

            _activityServiceMock.Setup(x => x.MapAndSaveDocument(It.IsAny<string>(), It.IsAny<ActivityResponse>()))
                .Returns(Task.CompletedTask);

            // Act
            cts.Cancel(); // Cancel before starting
            var result = await InvokeExecuteAsync(cts.Token);

            // Assert
            _appLifetimeMock.Verify(x => x.StopApplication(), Times.Once);
        }      

        #endregion

        #region Performance Tests

        [Fact]
        public async Task ExecuteAsync_ShouldCompleteWithinReasonableTime_WhenSuccessful()
        {
            // Arrange
            var expectedDate = DateTime.Now.AddDays(-1).ToString("yyyy-MM-dd");
            var activityResponse = new ActivityResponse();

            _fitbitServiceMock.Setup(x => x.GetActivityResponse(expectedDate))
                .ReturnsAsync(activityResponse);
            _activityServiceMock.Setup(x => x.MapAndSaveDocument(expectedDate, activityResponse))
                .Returns(Task.CompletedTask);

            // Act
            var stopwatch = Stopwatch.StartNew();
            var result = await InvokeExecuteAsync();
            stopwatch.Stop();

            // Assert
            result.Should().Be(0);
            stopwatch.ElapsedMilliseconds.Should().BeLessThan(5000); // 5 seconds max for unit test
        }

        [Fact]
        public async Task ExecuteAsync_ShouldReturn0_WhenFitbitServiceIsSlow()
        {
            // Arrange
            var expectedDate = DateTime.Now.AddDays(-1).ToString("yyyy-MM-dd");
            var activityResponse = new ActivityResponse();

            _fitbitServiceMock.Setup(x => x.GetActivityResponse(expectedDate))
                .Returns(async () =>
                {
                    await Task.Delay(1000); // Simulate 1 second delay
                    return activityResponse;
                });
            _activityServiceMock.Setup(x => x.MapAndSaveDocument(expectedDate, activityResponse))
                .Returns(Task.CompletedTask);

            // Act
            var result = await InvokeExecuteAsync();

            // Assert
            result.Should().Be(0);
            _fitbitServiceMock.Verify(x => x.GetActivityResponse(expectedDate), Times.Once);
            _activityServiceMock.Verify(x => x.MapAndSaveDocument(expectedDate, activityResponse), Times.Once);
        }

        #endregion

        #region Application Lifetime Tests

        [Fact]
        public async Task ExecuteAsync_ShouldStopApplication_WhenExceptionIsThrown()
        {
            // Arrange
            var expectedDate = DateTime.Now.AddDays(-1).ToString("yyyy-MM-dd");
            _fitbitServiceMock.Setup(x => x.GetActivityResponse(expectedDate))
                .ThrowsAsync(new Exception("Test exception"));

            // Act
            await InvokeExecuteAsync();

            // Assert
            _appLifetimeMock.Verify(x => x.StopApplication(), Times.Once);
        }

        [Fact]
        public async Task ExecuteAsync_ShouldStopApplication_WhenSuccessful()
        {
            // Arrange
            var expectedDate = DateTime.Now.AddDays(-1).ToString("yyyy-MM-dd");
            var activityResponse = new ActivityResponse();

            _fitbitServiceMock.Setup(x => x.GetActivityResponse(expectedDate))
                .ReturnsAsync(activityResponse);
            _activityServiceMock.Setup(x => x.MapAndSaveDocument(expectedDate, activityResponse))
                .Returns(Task.CompletedTask);

            // Act
            await InvokeExecuteAsync();

            // Assert
            _appLifetimeMock.Verify(x => x.StopApplication(), Times.Once);
        }

        #endregion

        #region Mock Verification Tests

        [Fact]
        public async Task ExecuteAsync_ShouldPassCorrectParametersToServices_WhenSuccessful()
        {
            // Arrange
            var expectedDate = DateTime.Now.AddDays(-1).ToString("yyyy-MM-dd");
            var activityResponse = new ActivityResponse();

            _fitbitServiceMock.Setup(x => x.GetActivityResponse(expectedDate))
                .ReturnsAsync(activityResponse);
            _activityServiceMock.Setup(x => x.MapAndSaveDocument(expectedDate, activityResponse))
                .Returns(Task.CompletedTask);

            // Act
            await InvokeExecuteAsync();

            // Assert
            _fitbitServiceMock.Verify(x => x.GetActivityResponse(expectedDate), Times.Once);
            _activityServiceMock.Verify(x => x.MapAndSaveDocument(expectedDate, activityResponse), Times.Once);

            // Verify no other calls were made
            _fitbitServiceMock.VerifyNoOtherCalls();
            _activityServiceMock.VerifyNoOtherCalls();
        }

        [Fact]
        public async Task ExecuteAsync_ShouldNotMakeAdditionalServiceCalls_WhenSuccessful()
        {
            // Arrange
            var expectedDate = DateTime.Now.AddDays(-1).ToString("yyyy-MM-dd");
            var activityResponse = new ActivityResponse();

            _fitbitServiceMock.Setup(x => x.GetActivityResponse(expectedDate))
                .ReturnsAsync(activityResponse);
            _activityServiceMock.Setup(x => x.MapAndSaveDocument(expectedDate, activityResponse))
                .Returns(Task.CompletedTask);

            // Act
            await InvokeExecuteAsync();

            // Assert
            _fitbitServiceMock.Verify(x => x.GetActivityResponse(It.IsAny<string>()), Times.Once);
            _activityServiceMock.Verify(x => x.MapAndSaveDocument(It.IsAny<string>(), It.IsAny<ActivityResponse>()), Times.Once);
        }

        #endregion
    }
}