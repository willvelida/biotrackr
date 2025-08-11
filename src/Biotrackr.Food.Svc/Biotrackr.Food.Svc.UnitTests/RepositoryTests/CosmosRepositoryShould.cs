using Biotrackr.Food.Svc.Configuration;
using Biotrackr.Food.Svc.Models;
using Biotrackr.Food.Svc.Models.FitbitEntities;
using Biotrackr.Food.Svc.Repositories;
using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Biotrackr.Food.Svc.UnitTests.RepositoryTests
{
    public class CosmosRepositoryShould
    {
        private readonly Mock<CosmosClient> _mockCosmosClient;
        private readonly Mock<Container> _mockContainer;
        private readonly Mock<ILogger<CosmosRepository>> _mockLogger;
        private readonly Mock<IOptions<Settings>> _mockOptions;
        private readonly Settings _settings;
        private readonly Fixture _fixture;

        public CosmosRepositoryShould()
        {
            _mockCosmosClient = new Mock<CosmosClient>();
            _mockContainer = new Mock<Container>();
            _mockLogger = new Mock<ILogger<CosmosRepository>>();
            _mockOptions = new Mock<IOptions<Settings>>();
            _fixture = new Fixture();

            _settings = new Settings
            {
                DatabaseName = "TestDatabase",
                ContainerName = "TestContainer"
            };

            _mockOptions.Setup(x => x.Value).Returns(_settings);
            _mockCosmosClient.Setup(x => x.GetContainer(_settings.DatabaseName, _settings.ContainerName))
                           .Returns(_mockContainer.Object);
        }

        [Fact]
        public void Constructor_WithValidParameters_ShouldCreateInstance()
        {
            // Act
            var repository = new CosmosRepository(_mockCosmosClient.Object, _mockOptions.Object, _mockLogger.Object);

            // Assert
            repository.Should().NotBeNull();
            _mockCosmosClient.Verify(x => x.GetContainer(_settings.DatabaseName, _settings.ContainerName), Times.Once);
        }

        [Fact]
        public void Constructor_WithNullCosmosClient_ShouldThrowArgumentNullException()
        {
            // Act & Assert
            var exception = Assert.Throws<ArgumentNullException>(() =>
                new CosmosRepository(null!, _mockOptions.Object, _mockLogger.Object));

            exception.ParamName.Should().Be("cosmosClient");
        }

        [Fact]
        public void Constructor_WithNullSettings_ShouldThrowArgumentNullException()
        {
            // Act & Assert
            var exception = Assert.Throws<ArgumentNullException>(() =>
                new CosmosRepository(_mockCosmosClient.Object, null!, _mockLogger.Object));

            exception.ParamName.Should().Be("settings");
        }

        [Fact]
        public void Constructor_WithNullLogger_ShouldThrowArgumentNullException()
        {
            // Act & Assert
            var exception = Assert.Throws<ArgumentNullException>(() =>
                new CosmosRepository(_mockCosmosClient.Object, _mockOptions.Object, null!));

            exception.ParamName.Should().Be("logger");
        }

        [Fact]
        public void Constructor_WithNullDatabaseName_ShouldThrowArgumentNullException()
        {
            // Arrange
            var invalidSettings = new Settings
            {
                DatabaseName = null,
                ContainerName = "TestContainer"
            };
            _mockOptions.Setup(x => x.Value).Returns(invalidSettings);

            // Act & Assert
            var exception = Assert.Throws<ArgumentNullException>(() =>
                new CosmosRepository(_mockCosmosClient.Object, _mockOptions.Object, _mockLogger.Object));

            exception.Message.Should().Contain("DatabaseName cannot be null or empty");
        }

        [Fact]
        public void Constructor_WithEmptyDatabaseName_ShouldThrowArgumentNullException()
        {
            // Arrange
            var invalidSettings = new Settings
            {
                DatabaseName = string.Empty,
                ContainerName = "TestContainer"
            };
            _mockOptions.Setup(x => x.Value).Returns(invalidSettings);

            // Act & Assert
            var exception = Assert.Throws<ArgumentNullException>(() =>
                new CosmosRepository(_mockCosmosClient.Object, _mockOptions.Object, _mockLogger.Object));

            exception.Message.Should().Contain("DatabaseName cannot be null or empty");
        }

        [Fact]
        public void Constructor_WithNullContainerName_ShouldThrowArgumentNullException()
        {
            // Arrange
            var invalidSettings = new Settings
            {
                DatabaseName = "TestDatabase",
                ContainerName = null
            };
            _mockOptions.Setup(x => x.Value).Returns(invalidSettings);

            // Act & Assert
            var exception = Assert.Throws<ArgumentNullException>(() =>
                new CosmosRepository(_mockCosmosClient.Object, _mockOptions.Object, _mockLogger.Object));

            exception.Message.Should().Contain("ContainerName cannot be null or empty");
        }

        [Fact]
        public void Constructor_WithEmptyContainerName_ShouldThrowArgumentNullException()
        {
            // Arrange
            var invalidSettings = new Settings
            {
                DatabaseName = "TestDatabase",
                ContainerName = string.Empty
            };
            _mockOptions.Setup(x => x.Value).Returns(invalidSettings);

            // Act & Assert
            var exception = Assert.Throws<ArgumentNullException>(() =>
                new CosmosRepository(_mockCosmosClient.Object, _mockOptions.Object, _mockLogger.Object));

            exception.Message.Should().Contain("ContainerName cannot be null or empty");
        }

        [Fact]
        public async Task CreateFoodDocument_WithValidDocument_ShouldCallCreateItemAsync()
        {
            // Arrange
            var repository = new CosmosRepository(_mockCosmosClient.Object, _mockOptions.Object, _mockLogger.Object);
            var foodDocument = CreateValidFoodDocument();

            _mockContainer.Setup(x => x.CreateItemAsync(
                It.IsAny<FoodDocument>(),
                It.IsAny<PartitionKey>(),
                It.IsAny<ItemRequestOptions>(),
                It.IsAny<CancellationToken>()))
                .Returns(Task.FromResult(Mock.Of<ItemResponse<FoodDocument>>()));

            // Act
            await repository.CreateFoodDocument(foodDocument);

            // Assert
            _mockContainer.Verify(x => x.CreateItemAsync(
                It.Is<FoodDocument>(doc => doc == foodDocument),
                It.Is<PartitionKey>(pk => pk.ToString().Contains(foodDocument.DocumentType!)),
                It.Is<ItemRequestOptions>(opts => opts.EnableContentResponseOnWrite == false),
                It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task CreateFoodDocument_WhenCosmosThrowsException_ShouldLogErrorAndRethrow()
        {
            // Arrange
            var repository = new CosmosRepository(_mockCosmosClient.Object, _mockOptions.Object, _mockLogger.Object);
            var foodDocument = CreateValidFoodDocument();
            var cosmosException = new CosmosException("Test exception", System.Net.HttpStatusCode.InternalServerError, 0, "test", 0);

            _mockContainer.Setup(x => x.CreateItemAsync(
                It.IsAny<FoodDocument>(),
                It.IsAny<PartitionKey>(),
                It.IsAny<ItemRequestOptions>(),
                It.IsAny<CancellationToken>()))
                .ThrowsAsync(cosmosException);

            // Act & Assert
            var exception = await Assert.ThrowsAsync<CosmosException>(() =>
                repository.CreateFoodDocument(foodDocument));

            exception.Should().Be(cosmosException);

            // Verify logging occurred
            _mockLogger.Verify(
                x => x.Log(
                    LogLevel.Error,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Exception thrown in CreateActivityDocument")),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
                Times.Once);
        }

        [Fact]
        public async Task CreateFoodDocument_WhenGeneralExceptionThrown_ShouldLogErrorAndRethrow()
        {
            // Arrange
            var repository = new CosmosRepository(_mockCosmosClient.Object, _mockOptions.Object, _mockLogger.Object);
            var foodDocument = CreateValidFoodDocument();
            var generalException = new InvalidOperationException("General test exception");

            _mockContainer.Setup(x => x.CreateItemAsync(
                It.IsAny<FoodDocument>(),
                It.IsAny<PartitionKey>(),
                It.IsAny<ItemRequestOptions>(),
                It.IsAny<CancellationToken>()))
                .ThrowsAsync(generalException);

            // Act & Assert
            var exception = await Assert.ThrowsAsync<InvalidOperationException>(() =>
                repository.CreateFoodDocument(foodDocument));

            exception.Should().Be(generalException);

            // Verify logging occurred
            _mockLogger.Verify(
                x => x.Log(
                    LogLevel.Error,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Exception thrown in CreateActivityDocument")),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
                Times.Once);
        }

        [Fact]
        public async Task CreateFoodDocument_ShouldUseCorrectItemRequestOptions()
        {
            // Arrange
            var repository = new CosmosRepository(_mockCosmosClient.Object, _mockOptions.Object, _mockLogger.Object);
            var foodDocument = CreateValidFoodDocument();
            ItemRequestOptions? capturedOptions = null;

            _mockContainer.Setup(x => x.CreateItemAsync(
                It.IsAny<FoodDocument>(),
                It.IsAny<PartitionKey>(),
                It.IsAny<ItemRequestOptions>(),
                It.IsAny<CancellationToken>()))
                .Callback<FoodDocument, PartitionKey?, ItemRequestOptions?, CancellationToken>(
                    (doc, pk, opts, ct) => capturedOptions = opts)
                .Returns(Task.FromResult(Mock.Of<ItemResponse<FoodDocument>>()));

            // Act
            await repository.CreateFoodDocument(foodDocument);

            // Assert
            capturedOptions.Should().NotBeNull();
            capturedOptions!.EnableContentResponseOnWrite.Should().BeFalse();
        }

        [Fact]
        public async Task CreateFoodDocument_ShouldUseDocumentTypeAsPartitionKey()
        {
            // Arrange
            var repository = new CosmosRepository(_mockCosmosClient.Object, _mockOptions.Object, _mockLogger.Object);
            var foodDocument = CreateValidFoodDocument();
            PartitionKey? capturedPartitionKey = null;

            _mockContainer.Setup(x => x.CreateItemAsync(
                It.IsAny<FoodDocument>(),
                It.IsAny<PartitionKey>(),
                It.IsAny<ItemRequestOptions>(),
                It.IsAny<CancellationToken>()))
                .Callback<FoodDocument, PartitionKey?, ItemRequestOptions?, CancellationToken>(
                    (doc, pk, opts, ct) => capturedPartitionKey = pk)
                .Returns(Task.FromResult(Mock.Of<ItemResponse<FoodDocument>>()));

            // Act
            await repository.CreateFoodDocument(foodDocument);

            // Assert
            capturedPartitionKey.Should().NotBeNull();
            capturedPartitionKey.ToString().Should().Contain(foodDocument.DocumentType!);
        }

        private FoodDocument CreateValidFoodDocument()
        {
            return new FoodDocument
            {
                Id = _fixture.Create<string>(),
                DocumentType = "food",
                Date = DateTime.UtcNow.ToString("yyyy-MM-dd"),
                Food = new FoodResponse
                {
                    foods = _fixture.CreateMany<Models.FitbitEntities.Food>().ToList(),
                    goals = _fixture.Create<Goals>(),
                    summary = _fixture.Create<Summary>()
                }
            };
        }
    }
}
