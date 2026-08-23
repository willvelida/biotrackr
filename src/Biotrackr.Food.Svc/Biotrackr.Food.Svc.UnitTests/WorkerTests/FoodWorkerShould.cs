using Biotrackr.Food.Svc.Models.FitbitEntities;
using Biotrackr.Food.Svc.Services.Interfaces;
using Biotrackr.Food.Svc.Workers;
using Microsoft.Extensions.Hosting;
using System.Diagnostics;
using System.Globalization;
using System.Reflection;

namespace Biotrackr.Food.Svc.UnitTests.WorkerTests
{
    public class FoodWorkerShould
    {
        private readonly Mock<IFitbitService> _fitbitServiceMock;
        private readonly Mock<IFoodService> _foodServiceMock;
        private readonly Mock<ILogger<FoodWorker>> _loggerMock;
        private readonly Mock<IHostApplicationLifetime> _appLifetimeMock;

        private FoodWorker _sut;

        public FoodWorkerShould()
        {
            _fitbitServiceMock = new Mock<IFitbitService>();
            _foodServiceMock = new Mock<IFoodService>();
            _loggerMock = new Mock<ILogger<FoodWorker>>();
            _appLifetimeMock = new Mock<IHostApplicationLifetime>();

            _sut = new FoodWorker(_fitbitServiceMock.Object, _foodServiceMock.Object, _loggerMock.Object, _appLifetimeMock.Object, TimeProvider.System);
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
        private async Task<int> InvokeExecuteAsync(CancellationToken cancellationToken = default, FoodWorker? worker = null)
        {
            var executeMethod = typeof(FoodWorker).GetMethod("ExecuteAsync", BindingFlags.NonPublic | BindingFlags.Instance);
            var task = (Task<int>)executeMethod!.Invoke(worker ?? _sut, new object[] { cancellationToken })!;
            return await task;
        }

        #region Constructor Tests

        [Fact]
        public void Constructor_ShouldCreateInstance_WhenParametersAreValid()
        {
            // Arrange
            var fitbitService = _fitbitServiceMock.Object;
            var foodService = _foodServiceMock.Object;
            var logger = _loggerMock.Object;
            var appLifetime = _appLifetimeMock.Object;
            var timeProvider = TimeProvider.System;

            // Act
            var worker = new FoodWorker(fitbitService, foodService, logger, appLifetime, timeProvider);

            // Assert
            worker.Should().NotBeNull();
        }

        [Fact]
        public void Constructor_ShouldThrowArgumentNullException_WhenFitbitServiceIsNull()
        {
            // Arrange
            var foodService = _foodServiceMock.Object;
            var logger = _loggerMock.Object;
            var appLifetime = _appLifetimeMock.Object;
            var timeProvider = TimeProvider.System;

            // Act
            var exception = Assert.Throws<ArgumentNullException>(() =>
                new FoodWorker(null!, foodService, logger, appLifetime, timeProvider));

            // Assert
            exception.ParamName.Should().Be("fitbitService");
        }

        [Fact]
        public void Constructor_ShouldThrowArgumentNullException_WhenFoodServiceIsNull()
        {
            // Arrange
            var fitbitService = _fitbitServiceMock.Object;
            var logger = _loggerMock.Object;
            var appLifetime = _appLifetimeMock.Object;
            var timeProvider = TimeProvider.System;

            // Act
            var exception = Assert.Throws<ArgumentNullException>(() =>
                new FoodWorker(fitbitService, null!, logger, appLifetime, timeProvider));

            // Assert
            exception.ParamName.Should().Be("foodService");
        }

        [Fact]
        public void Constructor_ShouldThrowArgumentNullException_WhenLoggerIsNull()
        {
            // Arrange
            var fitbitService = _fitbitServiceMock.Object;
            var foodService = _foodServiceMock.Object;
            var appLifetime = _appLifetimeMock.Object;
            var timeProvider = TimeProvider.System;

            // Act
            var exception = Assert.Throws<ArgumentNullException>(() =>
                new FoodWorker(fitbitService, foodService, null!, appLifetime, timeProvider));

            // Assert
            exception.ParamName.Should().Be("logger");
        }

        [Fact]
        public void Constructor_ShouldThrowArgumentNullException_WhenAppLifetimeIsNull()
        {
            // Arrange
            var fitbitService = _fitbitServiceMock.Object;
            var foodService = _foodServiceMock.Object;
            var logger = _loggerMock.Object;
            var timeProvider = TimeProvider.System;

            // Act
            var exception = Assert.Throws<ArgumentNullException>(() =>
                new FoodWorker(fitbitService, foodService, logger, null!, timeProvider));

            // Assert
            exception.ParamName.Should().Be("appLifetime");
        }

        [Fact]
        public void Constructor_ShouldThrowArgumentNullException_WhenTimeProviderIsNull()
        {
            // Arrange
            var fitbitService = _fitbitServiceMock.Object;
            var foodService = _foodServiceMock.Object;
            var logger = _loggerMock.Object;
            var appLifetime = _appLifetimeMock.Object;

            // Act
            var exception = Assert.Throws<ArgumentNullException>(() =>
                new FoodWorker(fitbitService, foodService, logger, appLifetime, null!));

            // Assert
            exception.ParamName.Should().Be("timeProvider");
        }

        #endregion

        #region Success Path Tests

        [Fact]
        public async Task ExecuteAsync_ShouldReturn0_WhenSuccessful()
        {
            // Arrange
            var expectedDate = DateTime.Now.AddDays(-1).ToString("yyyy-MM-dd");
            var foodResponse = new FoodResponse();

            _fitbitServiceMock.Setup(x => x.GetFoodResponse(expectedDate))
                .ReturnsAsync(foodResponse);
            _foodServiceMock.Setup(x => x.MapAndSaveDocument(expectedDate, foodResponse))
                .Returns(Task.CompletedTask);

            // Act
            var result = await InvokeExecuteAsync();

            // Assert
            result.Should().Be(0);
            _fitbitServiceMock.Verify(x => x.GetFoodResponse(expectedDate), Times.Once);
            _foodServiceMock.Verify(x => x.MapAndSaveDocument(expectedDate, foodResponse), Times.Once);
            _appLifetimeMock.Verify(x => x.StopApplication(), Times.Once);
        }

        [Fact]
        public async Task ExecuteAsync_ShouldLogInformationMessages_WhenSuccessful()
        {
            // Arrange
            var expectedDate = DateTime.Now.AddDays(-1).ToString("yyyy-MM-dd");
            var foodResponse = new FoodResponse();

            _fitbitServiceMock.Setup(x => x.GetFoodResponse(expectedDate))
                .ReturnsAsync(foodResponse);
            _foodServiceMock.Setup(x => x.MapAndSaveDocument(expectedDate, foodResponse))
                .Returns(Task.CompletedTask);

            // Act
            await InvokeExecuteAsync();

            // Assert
            _loggerMock.VerifyLog(logger => logger.LogInformation(It.Is<string>(s => s.StartsWith($"{nameof(FoodWorker)} executed at:"))), Times.Once);
            _loggerMock.VerifyLog(logger => logger.LogInformation($"Fetching food data for date: {expectedDate}"), Times.Once);
            _loggerMock.VerifyLog(logger => logger.LogInformation($"Mapping and saving food document for date: {expectedDate}"), Times.Once);
        }

        [Fact]
        public async Task ExecuteAsync_ShouldRequestYesterdaysDate_WhenInvoked()
        {
            // Arrange
            var expectedDate = DateTime.Now.AddDays(-1).ToString("yyyy-MM-dd");
            var foodResponse = new FoodResponse();

            _fitbitServiceMock.Setup(x => x.GetFoodResponse(expectedDate))
                .ReturnsAsync(foodResponse);
            _foodServiceMock.Setup(x => x.MapAndSaveDocument(expectedDate, foodResponse))
                .Returns(Task.CompletedTask);

            // Act
            await InvokeExecuteAsync();

            // Assert
            _fitbitServiceMock.Verify(x => x.GetFoodResponse(expectedDate), Times.Once);
            _foodServiceMock.Verify(x => x.MapAndSaveDocument(expectedDate, foodResponse), Times.Once);
        }

        [Fact]
        public async Task ExecuteAsync_ShouldCallServicesInCorrectOrder_WhenSuccessful()
        {
            // Arrange
            var expectedDate = DateTime.Now.AddDays(-1).ToString("yyyy-MM-dd");
            var foodResponse = new FoodResponse();
            var callOrder = new List<string>();

            _fitbitServiceMock.Setup(x => x.GetFoodResponse(expectedDate))
                .Returns(async () =>
                {
                    callOrder.Add("FitbitService");
                    await Task.Delay(10);
                    return foodResponse;
                });

            _foodServiceMock.Setup(x => x.MapAndSaveDocument(expectedDate, foodResponse))
                .Returns(async () =>
                {
                    callOrder.Add("FoodService");
                    await Task.Delay(10);
                });

            // Act
            await InvokeExecuteAsync();

            // Assert
            callOrder.Should().Equal("FitbitService", "FoodService");
        }

        #endregion

        #region Exception Handling Tests

        [Fact]
        public async Task ExecuteAsync_ShouldReturn1_WhenFitbitServiceThrowsException()
        {
            // Arrange
            var expectedDate = DateTime.Now.AddDays(-1).ToString("yyyy-MM-dd");
            var expectedException = new Exception("Test exception");
            _fitbitServiceMock.Setup(x => x.GetFoodResponse(expectedDate))
                .ThrowsAsync(expectedException);

            // Act
            var result = await InvokeExecuteAsync();

            // Assert
            result.Should().Be(1);
            _loggerMock.VerifyLog(logger => logger.LogError($"Exception thrown in {nameof(FoodWorker)}: Test exception"), Times.Once);
            _appLifetimeMock.Verify(x => x.StopApplication(), Times.Once);
            _foodServiceMock.Verify(x => x.MapAndSaveDocument(It.IsAny<string>(), It.IsAny<FoodResponse>()), Times.Never);
        }

        [Fact]
        public async Task ExecuteAsync_ShouldReturn1_WhenFoodServiceThrowsException()
        {
            // Arrange
            var expectedDate = DateTime.Now.AddDays(-1).ToString("yyyy-MM-dd");
            var foodResponse = new FoodResponse();
            var expectedException = new Exception("Test exception");

            _fitbitServiceMock.Setup(x => x.GetFoodResponse(expectedDate))
                .ReturnsAsync(foodResponse);
            _foodServiceMock.Setup(x => x.MapAndSaveDocument(expectedDate, foodResponse))
                .ThrowsAsync(expectedException);

            // Act
            var result = await InvokeExecuteAsync();

            // Assert
            result.Should().Be(1);
            _loggerMock.VerifyLog(logger => logger.LogError($"Exception thrown in {nameof(FoodWorker)}: Test exception"), Times.Once);
            _appLifetimeMock.Verify(x => x.StopApplication(), Times.Once);
            _fitbitServiceMock.Verify(x => x.GetFoodResponse(expectedDate), Times.Once);
        }

        [Fact]
        public async Task ExecuteAsync_ShouldNotCallFoodService_WhenFitbitServiceFails()
        {
            // Arrange
            var expectedDate = DateTime.Now.AddDays(-1).ToString("yyyy-MM-dd");
            _fitbitServiceMock.Setup(x => x.GetFoodResponse(expectedDate))
                .ThrowsAsync(new Exception("Fitbit failed"));

            // Act
            await InvokeExecuteAsync();

            // Assert
            _foodServiceMock.Verify(x => x.MapAndSaveDocument(It.IsAny<string>(), It.IsAny<FoodResponse>()), Times.Never);
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
            var exception = (Exception)Activator.CreateInstance(exceptionType, message)!;

            _fitbitServiceMock.Setup(x => x.GetFoodResponse(expectedDate))
                .ThrowsAsync(exception);

            // Act
            var result = await InvokeExecuteAsync();

            // Assert
            result.Should().Be(1);
            _loggerMock.VerifyLog(logger => logger.LogError($"Exception thrown in {nameof(FoodWorker)}: {message}"), Times.Once);
            _appLifetimeMock.Verify(x => x.StopApplication(), Times.Once);
        }

        #endregion

        #region Edge Cases and Null Handling Tests

        [Fact]
        public async Task ExecuteAsync_ShouldReturn0_WhenFoodResponseIsNull()
        {
            // Arrange
            var expectedDate = DateTime.Now.AddDays(-1).ToString("yyyy-MM-dd");
            _fitbitServiceMock.Setup(x => x.GetFoodResponse(expectedDate))
                .ReturnsAsync((FoodResponse?)null);
            _foodServiceMock.Setup(x => x.MapAndSaveDocument(expectedDate, It.IsAny<FoodResponse?>()))
                .Returns(Task.CompletedTask);

            // Act
            var result = await InvokeExecuteAsync();

            // Assert
            result.Should().Be(0);
            _foodServiceMock.Verify(x => x.MapAndSaveDocument(expectedDate, It.IsAny<FoodResponse?>()), Times.Once);
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
            var worker = new FoodWorker(_fitbitServiceMock.Object, _foodServiceMock.Object, _loggerMock.Object, _appLifetimeMock.Object, timeProvider);
            var foodResponse = new FoodResponse();

            _fitbitServiceMock.Setup(x => x.GetFoodResponse(expectedPreviousDate))
                .ReturnsAsync(foodResponse);
            _foodServiceMock.Setup(x => x.MapAndSaveDocument(expectedPreviousDate, foodResponse))
                .Returns(Task.CompletedTask);

            // Act
            var result = await InvokeExecuteAsync(worker: worker);

            // Assert
            result.Should().Be(0);
            _fitbitServiceMock.Verify(x => x.GetFoodResponse(expectedPreviousDate), Times.Once,
                $"AGENT FIX: FoodWorker must request the day before the current LOCAL date ({expectedPreviousDate}) "
                + $"for a clock at {utcNow} in a UTC{localOffsetHours:+00;-00} zone. "
                + "Using DateTime.UtcNow instead of local time breaks this.");
            _foodServiceMock.Verify(x => x.MapAndSaveDocument(expectedPreviousDate, foodResponse), Times.Once);
        }

        #endregion

        #region Cancellation Token Tests

        [Fact]
        public async Task ExecuteAsync_ShouldStopApplication_WhenCancellationIsRequested()
        {
            // Arrange
            var cts = new CancellationTokenSource();
            var expectedDate = DateTime.Now.AddDays(-1).ToString("yyyy-MM-dd");

            _fitbitServiceMock.Setup(x => x.GetFoodResponse(expectedDate))
                .Returns(async () =>
                {
                    cts.Token.ThrowIfCancellationRequested();
                    await Task.Delay(100, cts.Token);
                    return new FoodResponse();
                });

            _foodServiceMock.Setup(x => x.MapAndSaveDocument(It.IsAny<string>(), It.IsAny<FoodResponse>()))
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
            var foodResponse = new FoodResponse();

            _fitbitServiceMock.Setup(x => x.GetFoodResponse(expectedDate))
                .ReturnsAsync(foodResponse);
            _foodServiceMock.Setup(x => x.MapAndSaveDocument(expectedDate, foodResponse))
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
            var foodResponse = new FoodResponse();

            _fitbitServiceMock.Setup(x => x.GetFoodResponse(expectedDate))
                .Returns(async () =>
                {
                    await Task.Delay(100); // Simulate slow service
                    return foodResponse;
                });

            _foodServiceMock.Setup(x => x.MapAndSaveDocument(expectedDate, foodResponse))
                .Returns(Task.CompletedTask);

            // Act
            var result = await InvokeExecuteAsync();

            // Assert
            result.Should().Be(0);
            _fitbitServiceMock.Verify(x => x.GetFoodResponse(expectedDate), Times.Once);
            _foodServiceMock.Verify(x => x.MapAndSaveDocument(expectedDate, foodResponse), Times.Once);
        }

        #endregion

        #region Application Lifetime Tests

        [Fact]
        public async Task ExecuteAsync_ShouldStopApplication_WhenExceptionIsThrown()
        {
            // Arrange
            var expectedDate = DateTime.Now.AddDays(-1).ToString("yyyy-MM-dd");
            _fitbitServiceMock.Setup(x => x.GetFoodResponse(expectedDate))
                .ThrowsAsync(new Exception("Service failure"));

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
            var foodResponse = new FoodResponse();

            _fitbitServiceMock.Setup(x => x.GetFoodResponse(expectedDate))
                .ReturnsAsync(foodResponse);
            _foodServiceMock.Setup(x => x.MapAndSaveDocument(expectedDate, foodResponse))
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
            var foodResponse = new FoodResponse();

            _fitbitServiceMock.Setup(x => x.GetFoodResponse(expectedDate))
                .ReturnsAsync(foodResponse);
            _foodServiceMock.Setup(x => x.MapAndSaveDocument(expectedDate, foodResponse))
                .Returns(Task.CompletedTask);

            // Act
            await InvokeExecuteAsync();

            // Assert
            _fitbitServiceMock.Verify(x => x.GetFoodResponse(expectedDate), Times.Once);
            _foodServiceMock.Verify(x => x.MapAndSaveDocument(expectedDate, foodResponse), Times.Once);
        }

        [Fact]
        public async Task ExecuteAsync_ShouldNotMakeAdditionalServiceCalls_WhenSuccessful()
        {
            // Arrange
            var expectedDate = DateTime.Now.AddDays(-1).ToString("yyyy-MM-dd");
            var foodResponse = new FoodResponse();

            _fitbitServiceMock.Setup(x => x.GetFoodResponse(expectedDate))
                .ReturnsAsync(foodResponse);
            _foodServiceMock.Setup(x => x.MapAndSaveDocument(expectedDate, foodResponse))
                .Returns(Task.CompletedTask);

            // Act
            await InvokeExecuteAsync();

            // Assert
            _fitbitServiceMock.Verify(x => x.GetFoodResponse(It.IsAny<string>()), Times.Once);
            _foodServiceMock.Verify(x => x.MapAndSaveDocument(It.IsAny<string>(), It.IsAny<FoodResponse>()), Times.Once);
            _appLifetimeMock.Verify(x => x.StopApplication(), Times.Once);
        }

        #endregion
    }
}
