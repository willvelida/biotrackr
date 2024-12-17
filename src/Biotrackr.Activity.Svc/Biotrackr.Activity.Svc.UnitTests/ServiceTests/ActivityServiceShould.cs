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
    }
}
