using AutoFixture;
using Biotrackr.Weight.Api.Configuration;
using Biotrackr.Weight.Api.Models;
using Biotrackr.Weight.Api.Repositories;
using FluentAssertions;
using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;

namespace Biotrackr.Weight.Api.UnitTests.RepositoryTests
{
    public class CosmosRepositoryShould
    {
        private readonly Mock<CosmosClient> _mockCosmosClient;
        private readonly Mock<Container> _mockContainer;
        private readonly Mock<IOptions<Settings>> _mockSettings;
        private readonly Mock<ILogger<CosmosRepository>> _mockLogger;
        private readonly CosmosRepository _cosmosRepository;

        public CosmosRepositoryShould()
        {
            _mockCosmosClient = new Mock<CosmosClient>();
            _mockContainer = new Mock<Container>();
            _mockSettings = new Mock<IOptions<Settings>>();
            _mockLogger = new Mock<ILogger<CosmosRepository>>();

            var settings = new Settings
            {
                DatabaseName = "TestDatabase",
                ContainerName = "TestContainer"
            };

            _mockSettings.Setup(s => s.Value).Returns(settings);
            _mockCosmosClient.Setup(c => c.GetContainer(It.IsAny<string>(), It.IsAny<string>())).Returns(_mockContainer.Object);

            _cosmosRepository = new CosmosRepository(_mockCosmosClient.Object, _mockSettings.Object, _mockLogger.Object);
        }

        [Fact]
        public async Task GetAllWeightDocuments_ShouldReturnListOfWeightDocuments()
        {
            // Arrange
            var fixture = new Fixture();
            var weightDocuments = fixture.CreateMany<WeightDocument>().ToList();

            var feedResponse = new Mock<FeedResponse<WeightDocument>>();
            feedResponse.Setup(f => f.GetEnumerator()).Returns(weightDocuments.GetEnumerator());

            var mockFeedIterator = new Mock<FeedIterator<WeightDocument>>();
            mockFeedIterator.SetupSequence(i => i.HasMoreResults)
                .Returns(true)
                .Returns(false);
            mockFeedIterator.Setup(i => i.ReadNextAsync(default))
                .ReturnsAsync(feedResponse.Object);

            _mockContainer.Setup(c => c.GetItemQueryIterator<WeightDocument>(It.IsAny<QueryDefinition>(), It.IsAny<string>(), It.IsAny<QueryRequestOptions>()))
                .Returns(mockFeedIterator.Object);

            // Act
            var result = await _cosmosRepository.GetAllWeightDocuments();

            // Assert
            result.Should().BeEquivalentTo(weightDocuments);
        }

        [Fact]
        public async Task GetAllWeightDocuments_ShouldLogErrorAndThrowException_WhenExceptionOccurs()
        {
            // Arrange
            var exceptionMessage = "Test exception";
            var mockFeedIterator = new Mock<FeedIterator<WeightDocument>>();
            mockFeedIterator.Setup(i => i.HasMoreResults).Returns(true);
            mockFeedIterator.Setup(i => i.ReadNextAsync(default)).ThrowsAsync(new Exception(exceptionMessage));

            _mockContainer.Setup(c => c.GetItemQueryIterator<WeightDocument>(It.IsAny<QueryDefinition>(), It.IsAny<string>(), It.IsAny<QueryRequestOptions>()))
                .Returns(mockFeedIterator.Object);

            // Act
            Func<Task> act = async () => await _cosmosRepository.GetAllWeightDocuments();

            // Assert
            await act.Should().ThrowAsync<Exception>().WithMessage(exceptionMessage);
            _mockLogger.Verify(
                x => x.Log(
                    LogLevel.Error,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString().Contains($"Exception thrown in {nameof(CosmosRepository.GetAllWeightDocuments)}: {exceptionMessage}")),
                    It.IsAny<Exception>(),
                    It.Is<Func<It.IsAnyType, Exception, string>>((v, t) => true)),
                Times.Once);
        }
    }
}
