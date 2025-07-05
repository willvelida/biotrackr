namespace Biotrackr.Activity.Svc.UnitTests.RepositoryTests
{
    public class CosmosRepositoryShould
    {
        private readonly Mock<CosmosClient> _cosmosClientMock;
        private readonly Mock<ILogger<CosmosRepository>> _loggerMock;
        private readonly Mock<IOptions<Settings>> _optionsMock;
        private readonly Mock<Container> _containerMock;

        private CosmosRepository _cosmosRepository;

        public CosmosRepositoryShould()
        {
            _cosmosClientMock = new Mock<CosmosClient>();
            _loggerMock = new Mock<ILogger<CosmosRepository>>();
            _optionsMock = new Mock<IOptions<Settings>>();
            _containerMock = new Mock<Container>();

            _optionsMock.Setup(x => x.Value).Returns(new Settings
            {
                DatabaseName = "DatabaseName",
                ContainerName = "ContainerName"
            });

            _cosmosClientMock.Setup(x => x.GetContainer(It.IsAny<string>(), It.IsAny<string>()))
                .Returns(_containerMock.Object);

            _cosmosRepository = new CosmosRepository(_cosmosClientMock.Object, _optionsMock.Object, _loggerMock.Object);
        }

        [Fact]
        public async Task CreateActivityDocument_ShouldSucceed()
        {
            // Arrange
            var fixture = new Fixture();
            var activityDocument = fixture.Create<ActivityDocument>();
            var mockedItemResponse = new Mock<ItemResponse<ActivityDocument>>();

            mockedItemResponse.Setup(x => x.StatusCode)
                .Returns(System.Net.HttpStatusCode.OK);

            _containerMock.Setup(x => x.CreateItemAsync(It.IsAny<ActivityDocument>(), It.IsAny<PartitionKey>(), It.IsAny<ItemRequestOptions>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(mockedItemResponse.Object)
                .Callback<ActivityDocument, PartitionKey?, RequestOptions, CancellationToken>(
                    (t, p, r, c) => activityDocument = t);

            // Act
            Func<Task> repositoryAction = async () => await _cosmosRepository.CreateActivityDocument(activityDocument);

            // Assert
            await repositoryAction.Should().NotThrowAsync<Exception>();
            _containerMock.Verify(x => x.CreateItemAsync(activityDocument, new PartitionKey(activityDocument.DocumentType), It.IsAny<ItemRequestOptions>(), It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task CreateActivityDocument_ShouldThrowExceptionWhenFails()
        {
            // Arrange
            var fixture = new Fixture();
            var activityEnvelope = fixture.Create<ActivityDocument>();

            _containerMock.Setup(x => x.CreateItemAsync(It.IsAny<ActivityDocument>(), It.IsAny<PartitionKey>(), It.IsAny<ItemRequestOptions>(), It.IsAny<CancellationToken>()))
                .ThrowsAsync(new Exception("Mock Failure"));

            // Act
            Func<Task> repositoryAction = async () => await _cosmosRepository.CreateActivityDocument(activityEnvelope);

            // Assert
            await repositoryAction.Should().ThrowAsync<Exception>();
            _loggerMock.VerifyLog(logger => logger.LogError($"Exception thrown in CreateActivityDocument: Mock Failure"));
        }

        [Fact]
        public void Constructor_ShouldInitializeContainerWithCorrectParameters()
        {
            // Assert
            _cosmosClientMock.Verify(x => x.GetContainer("DatabaseName", "ContainerName"), Times.Once);
        }

        [Fact]
        public void Constructor_ShouldThrowArgumentNullException_WhenCosmosClientIsNull()
        {
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() =>
                new CosmosRepository(null, _optionsMock.Object, _loggerMock.Object));
        }

        [Fact]
        public void Constructor_ShouldThrowArgumentNullException_WhenOptionsIsNull()
        {
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() =>
                new CosmosRepository(_cosmosClientMock.Object, null, _loggerMock.Object));
        }

        [Fact]
        public void Constructor_ShouldThrowArgumentNullException_WhenLoggerIsNull()
        {
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() =>
                new CosmosRepository(_cosmosClientMock.Object, _optionsMock.Object, null));
        }

        [Theory]
        [InlineData(null, "ContainerName")]
        [InlineData("", "ContainerName")]
        [InlineData("DatabaseName", null)]
        [InlineData("DatabaseName", "")]
        [InlineData(null, null)]
        public void Constructor_ShouldHandleInvalidSettings(string databaseName, string containerName)
        {
            // Arrange
            _optionsMock.Setup(x => x.Value).Returns(new Settings
            {
                DatabaseName = databaseName,
                ContainerName = containerName
            });

            // Act & Assert
            var exception = Assert.Throws<ArgumentNullException>(() =>
                new CosmosRepository(_cosmosClientMock.Object, _optionsMock.Object, _loggerMock.Object));

        }
    }
}
