using AutoFixture;
using Biotrackr.Vitals.Svc.Configuration;
using Biotrackr.Vitals.Svc.Models;
using Biotrackr.Vitals.Svc.Repositories;
using FluentAssertions;
using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;

namespace Biotrackr.Vitals.Svc.UnitTests.RepositoryTests
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
                ContainerName = "ContainerName",
                UserHeight = 1.88
            });

            _cosmosClientMock.Setup(x => x.GetContainer(It.IsAny<string>(), It.IsAny<string>()))
                .Returns(_containerMock.Object);

            _cosmosRepository = new CosmosRepository(_cosmosClientMock.Object, _optionsMock.Object, _loggerMock.Object);
        }

        [Fact]
        public async Task UpsertVitalsDocument_ShouldSucceed()
        {
            // Arrange
            var fixture = new Fixture();
            var vitalsDocument = fixture.Create<VitalsDocument>();
            var mockedItemResponse = new Mock<ItemResponse<VitalsDocument>>();

            mockedItemResponse.Setup(x => x.StatusCode)
                .Returns(System.Net.HttpStatusCode.OK);

            _containerMock.Setup(x => x.UpsertItemAsync(It.IsAny<VitalsDocument>(), It.IsAny<PartitionKey>(), It.IsAny<ItemRequestOptions>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(mockedItemResponse.Object);

            // Act
            Func<Task> repositoryAction = async () => await _cosmosRepository.UpsertVitalsDocument(vitalsDocument);

            // Assert
            await repositoryAction.Should().NotThrowAsync<Exception>();
            _containerMock.Verify(x => x.UpsertItemAsync(vitalsDocument, new PartitionKey(vitalsDocument.DocumentType), It.IsAny<ItemRequestOptions>(), It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task UpsertVitalsDocument_ShouldThrowExceptionWhenFails()
        {
            // Arrange
            var fixture = new Fixture();
            var vitalsDocument = fixture.Create<VitalsDocument>();

            _containerMock.Setup(x => x.UpsertItemAsync(It.IsAny<VitalsDocument>(), It.IsAny<PartitionKey>(), It.IsAny<ItemRequestOptions>(), It.IsAny<CancellationToken>()))
                .ThrowsAsync(new Exception("Mock Failure"));

            // Act
            Func<Task> repositoryAction = async () => await _cosmosRepository.UpsertVitalsDocument(vitalsDocument);

            // Assert
            await repositoryAction.Should().ThrowAsync<Exception>();
            _loggerMock.VerifyLog(logger => logger.LogError($"Exception thrown in UpsertVitalsDocument: Mock Failure"));
        }

        [Fact]
        public async Task GetVitalsDocumentByDate_ShouldReturnDocument_WhenDocumentExists()
        {
            // Arrange
            var fixture = new Fixture();
            var expectedDocument = fixture.Create<VitalsDocument>();
            expectedDocument.Date = "2024-01-15";

            var feedResponseMock = new Mock<FeedResponse<VitalsDocument>>();
            feedResponseMock.Setup(x => x.GetEnumerator())
                .Returns(new List<VitalsDocument> { expectedDocument }.GetEnumerator());

            var iteratorMock = new Mock<FeedIterator<VitalsDocument>>();
            iteratorMock.SetupSequence(x => x.HasMoreResults)
                .Returns(true)
                .Returns(false);
            iteratorMock.Setup(x => x.ReadNextAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(feedResponseMock.Object);

            _containerMock.Setup(x => x.GetItemQueryIterator<VitalsDocument>(
                    It.IsAny<QueryDefinition>(),
                    It.IsAny<string>(),
                    It.IsAny<QueryRequestOptions>()))
                .Returns(iteratorMock.Object);

            // Act
            var result = await _cosmosRepository.GetVitalsDocumentByDate("2024-01-15");

            // Assert
            result.Should().NotBeNull();
            result.Should().BeEquivalentTo(expectedDocument);
        }

        [Fact]
        public async Task GetVitalsDocumentByDate_ShouldReturnNull_WhenNoDocumentExists()
        {
            // Arrange
            var feedResponseMock = new Mock<FeedResponse<VitalsDocument>>();
            feedResponseMock.Setup(x => x.GetEnumerator())
                .Returns(new List<VitalsDocument>().GetEnumerator());

            var iteratorMock = new Mock<FeedIterator<VitalsDocument>>();
            iteratorMock.SetupSequence(x => x.HasMoreResults)
                .Returns(true)
                .Returns(false);
            iteratorMock.Setup(x => x.ReadNextAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(feedResponseMock.Object);

            _containerMock.Setup(x => x.GetItemQueryIterator<VitalsDocument>(
                    It.IsAny<QueryDefinition>(),
                    It.IsAny<string>(),
                    It.IsAny<QueryRequestOptions>()))
                .Returns(iteratorMock.Object);

            // Act
            var result = await _cosmosRepository.GetVitalsDocumentByDate("2024-01-15");

            // Assert
            result.Should().BeNull();
        }

        [Fact]
        public async Task GetVitalsDocumentByDate_ShouldReturnNull_WhenIteratorHasNoResults()
        {
            // Arrange
            var iteratorMock = new Mock<FeedIterator<VitalsDocument>>();
            iteratorMock.Setup(x => x.HasMoreResults).Returns(false);

            _containerMock.Setup(x => x.GetItemQueryIterator<VitalsDocument>(
                    It.IsAny<QueryDefinition>(),
                    It.IsAny<string>(),
                    It.IsAny<QueryRequestOptions>()))
                .Returns(iteratorMock.Object);

            // Act
            var result = await _cosmosRepository.GetVitalsDocumentByDate("2024-01-15");

            // Assert
            result.Should().BeNull();
        }

        [Fact]
        public async Task GetVitalsDocumentByDate_ShouldThrowException_WhenQueryFails()
        {
            // Arrange
            var iteratorMock = new Mock<FeedIterator<VitalsDocument>>();
            iteratorMock.Setup(x => x.HasMoreResults).Returns(true);
            iteratorMock.Setup(x => x.ReadNextAsync(It.IsAny<CancellationToken>()))
                .ThrowsAsync(new Exception("Query failed"));

            _containerMock.Setup(x => x.GetItemQueryIterator<VitalsDocument>(
                    It.IsAny<QueryDefinition>(),
                    It.IsAny<string>(),
                    It.IsAny<QueryRequestOptions>()))
                .Returns(iteratorMock.Object);

            // Act
            Func<Task> act = async () => await _cosmosRepository.GetVitalsDocumentByDate("2024-01-15");

            // Assert
            await act.Should().ThrowAsync<Exception>().WithMessage("Query failed");
            _loggerMock.VerifyLog(logger => logger.LogError("Exception thrown in GetVitalsDocumentByDate: Query failed"));
        }
    }
}
