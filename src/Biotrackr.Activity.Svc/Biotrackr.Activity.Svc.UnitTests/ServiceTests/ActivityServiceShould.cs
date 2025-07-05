namespace Biotrackr.Activity.Svc.UnitTests.ServiceTests
{
    public class ActivityServiceShould
    {
        private readonly Mock<ICosmosRepository> _mockCosmosRepository;
        private readonly Mock<ILogger<ActivityService>> _mockLogger;
        private readonly ActivityService _activityService;

        public ActivityServiceShould()
        {
            _mockCosmosRepository = new Mock<ICosmosRepository>();
            _mockLogger = new Mock<ILogger<ActivityService>>();
            _activityService = new ActivityService(_mockCosmosRepository.Object, _mockLogger.Object);
        }

        [Fact]
        public async Task MapAndSaveDocument_ShouldMapAndSaveDocument()
        {
            // Arrange
            var date = "2023-10-01";
            var fixture = new Fixture();
            var activityResponse = fixture.Create<ActivityResponse>();

            _mockCosmosRepository.Setup(x => x.CreateActivityDocument(It.IsAny<ActivityDocument>()))
                .Returns(Task.CompletedTask);

            // Act
            Func<Task> activityServiceAction = async () => await _activityService.MapAndSaveDocument(date, activityResponse);

            // Assert
            await activityServiceAction.Should().NotThrowAsync<Exception>();
            _mockCosmosRepository.Verify(x => x.CreateActivityDocument(It.IsAny<ActivityDocument>()), Times.Once);
        }

        [Fact]
        public async Task MapAndSaveDocument_ShouldThrowExceptionWhenFails()
        {
            // Arrange
            var date = "2023-10-01";
            var fixture = new Fixture();
            var activityResponse = fixture.Create<ActivityResponse>();

            _mockCosmosRepository.Setup(x => x.CreateActivityDocument(It.IsAny<ActivityDocument>()))
                .ThrowsAsync(new Exception("Test exception"));

            // Act
            Func<Task> activityServiceAction = async () => await _activityService.MapAndSaveDocument(date, activityResponse);

            // Assert
            await activityServiceAction.Should().ThrowAsync<Exception>();
            _mockLogger.VerifyLog(logger => logger.LogError($"Exception thrown in MapAndSaveDocument: Test exception"));
        }

        [Fact]
        public async Task MapAndSaveDocument_ShouldLogErrorAndRethrow_WhenRepositoryThrowsException()
        {
            // Arrange
            var date = "2023-10-01";
            var activityResponse = new ActivityResponse();
            var expectedMessage = "Database connection failed";
            var expectedException = new Exception(expectedMessage);

            _mockCosmosRepository.Setup(x => x.CreateActivityDocument(It.IsAny<ActivityDocument>()))
                .ThrowsAsync(expectedException);

            // Act
            var exception = await Assert.ThrowsAsync<Exception>(() =>
                _activityService.MapAndSaveDocument(date, activityResponse));

            // Assert
            exception.Should().Be(expectedException);
            _mockLogger.VerifyLog(logger => logger.LogError($"Exception thrown in MapAndSaveDocument: {expectedMessage}"), Times.Once);
        }

        [Theory]
        [InlineData(typeof(TimeoutException), "Request timeout")]
        [InlineData(typeof(ArgumentException), "Invalid argument")]
        [InlineData(typeof(InvalidOperationException), "Invalid operation")]
        public async Task MapAndSaveDocument_ShouldHandleDifferentExceptionTypes(Type exceptionType, string message)
        {
            // Arrange
            var date = "2023-10-01";
            var activityResponse = new ActivityResponse();
            var exception = (Exception)Activator.CreateInstance(exceptionType, message);

            _mockCosmosRepository.Setup(x => x.CreateActivityDocument(It.IsAny<ActivityDocument>()))
                .ThrowsAsync(exception);

            // Act & Assert
            var thrownException = await Assert.ThrowsAsync(exceptionType, () =>
                _activityService.MapAndSaveDocument(date, activityResponse));

            thrownException.Message.Should().Be(message);
            _mockLogger.VerifyLog(logger => logger.LogError($"Exception thrown in MapAndSaveDocument: {message}"), Times.Once);
        }

        [Fact]
        public async Task MapAndSaveDocument_ShouldNotLogError_WhenSuccessful()
        {
            // Arrange
            var date = "2023-10-01";
            var activityResponse = new ActivityResponse();

            _mockCosmosRepository.Setup(x => x.CreateActivityDocument(It.IsAny<ActivityDocument>()))
                .Returns(Task.CompletedTask);

            // Act
            await _activityService.MapAndSaveDocument(date, activityResponse);

            // Assert
            _mockLogger.VerifyLog(logger => logger.LogError(It.IsAny<string>()), Times.Never);
        }
    }
}
