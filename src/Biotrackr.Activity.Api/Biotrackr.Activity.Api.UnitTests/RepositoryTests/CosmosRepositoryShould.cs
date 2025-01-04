using AutoFixture;
using Biotrackr.Activity.Api.Configuration;
using Biotrackr.Activity.Api.Models;
using Biotrackr.Activity.Api.Repositories;
using FluentAssertions;
using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;

namespace Biotrackr.Activity.Api.UnitTests.RepositoryTests
{
    public class CosmosRepositoryShould
    {
        private readonly Mock<CosmosClient> _cosmosClientMock;
        private readonly Mock<Container> _containerMock;
        private readonly Mock<IOptions<Settings>> _optionsMock;
        private readonly Mock<ILogger<CosmosRepository>> _loggerMock;

        private readonly CosmosRepository _repository;

        public CosmosRepositoryShould()
        {
            _cosmosClientMock = new Mock<CosmosClient>();
            _containerMock = new Mock<Container>();
            _optionsMock = new Mock<IOptions<Settings>>();
            _optionsMock.Setup(x => x.Value).Returns(new Settings
            {
                DatabaseName = "DatabaseName",
                ContainerName = "ContainerName"
            });

            _cosmosClientMock.Setup(x => x.GetContainer(It.IsAny<string>(), It.IsAny<string>()))
                .Returns(_containerMock.Object);
            _loggerMock = new Mock<ILogger<CosmosRepository>>();
            _repository = new CosmosRepository(_cosmosClientMock.Object, _optionsMock.Object, _loggerMock.Object);
        }

        [Fact]
        public async Task GetActivitySummaryByDate_ShouldReturnActivityDocument()
        {
            // Arrange
            var date = "2022-01-01";
            var fixture = new Fixture();
            var activityDocument = fixture.Create<ActivityDocument>();
            activityDocument.Date = date;

            var feedResponse = new Mock<FeedResponse<ActivityDocument>>();
            feedResponse.Setup(x => x.GetEnumerator()).Returns(new List<ActivityDocument> { activityDocument }.GetEnumerator());

            var iterator = new Mock<FeedIterator<ActivityDocument>>();
            iterator.SetupSequence(x => x.HasMoreResults).Returns(true).Returns(false);
            iterator.Setup(x => x.ReadNextAsync(default)).ReturnsAsync(feedResponse.Object);

            _containerMock.Setup(x => x.GetItemQueryIterator<ActivityDocument>(It.IsAny<QueryDefinition>(), It.IsAny<string>(), It.IsAny<QueryRequestOptions>())).Returns(iterator.Object);

            // Act
            var result = await _repository.GetActivitySummaryByDate(date);

            // Assert
            result.Should().BeEquivalentTo(activityDocument);
            result.Date.Should().Be(date);
        }

        [Fact]
        public async Task GetActivitySummaryByDate_ShouldReturnNull_WhenActivityDoesNotExist()
        {
            // Arrange
            var date = "2022-01-01";

            var feedResponse = new Mock<FeedResponse<ActivityDocument>>();
            feedResponse.Setup(x => x.GetEnumerator()).Returns(new List<ActivityDocument>().GetEnumerator());

            var iterator = new Mock<FeedIterator<ActivityDocument>>();
            iterator.SetupSequence(x => x.HasMoreResults).Returns(true).Returns(false);
            iterator.Setup(x => x.ReadNextAsync(default)).ReturnsAsync(feedResponse.Object);

            _containerMock.Setup(x => x.GetItemQueryIterator<ActivityDocument>(It.IsAny<QueryDefinition>(), It.IsAny<string>(), It.IsAny<QueryRequestOptions>())).Returns(iterator.Object);

            // Act
            var result = await _repository.GetActivitySummaryByDate(date);

            // Assert
            result.Should().BeNull();
        }

        [Fact]
        public async Task GetActivitySummaryByDate_ShouldLogErrorAndThrowException_WhenExceptionOccurs()
        {
            // Arrange
            var date = "2022-01-01";
            var exceptionMessage = "Test Exception";
            _containerMock.Setup(c => c.GetItemQueryIterator<ActivityDocument>(It.IsAny<QueryDefinition>(), It.IsAny<string>(), It.IsAny<QueryRequestOptions>()))
                          .Throws(new Exception(exceptionMessage));

            // Act
            Func<Task> act = async () => await _repository.GetActivitySummaryByDate(date);

            // Assert
            await act.Should().ThrowAsync<Exception>().WithMessage(exceptionMessage);
            _loggerMock.VerifyLog(logger => logger.LogError($"Exception thrown in GetActivitySummaryByDate: Test Exception"));
        }

        [Fact]
        public async Task GetAllActivitySummaries_ShouldReturnListOfActivityDocuments()
        {
            // Arrange
            var fixture = new Fixture();
            var activityDocuments = fixture.CreateMany<ActivityDocument>().ToList();

            var feedResponse = new Mock<FeedResponse<ActivityDocument>>();
            feedResponse.Setup(x => x.GetEnumerator()).Returns(activityDocuments.GetEnumerator());

            var iterator = new Mock<FeedIterator<ActivityDocument>>();
            iterator.SetupSequence(x => x.HasMoreResults).Returns(true).Returns(false);
            iterator.Setup(x => x.ReadNextAsync(default)).ReturnsAsync(feedResponse.Object);

            _containerMock.Setup(x => x.GetItemQueryIterator<ActivityDocument>(It.IsAny<QueryDefinition>(), It.IsAny<string>(), It.IsAny<QueryRequestOptions>())).Returns(iterator.Object);

            // Act
            var result = await _repository.GetAllActivitySummaries();

            // Assert
            result.Should().BeEquivalentTo(activityDocuments);
        }

        [Fact]
        public async Task GetAllActivitySummaries_ShouldReturnEmptyList_WhenActivitiesDoNotExist()
        {
            // Arrange
            var feedResponse = new Mock<FeedResponse<ActivityDocument>>();
            feedResponse.Setup(x => x.GetEnumerator()).Returns(new List<ActivityDocument>().GetEnumerator());

            var iterator = new Mock<FeedIterator<ActivityDocument>>();
            iterator.SetupSequence(x => x.HasMoreResults).Returns(true).Returns(false);
            iterator.Setup(x => x.ReadNextAsync(default)).ReturnsAsync(feedResponse.Object);

            _containerMock.Setup(x => x.GetItemQueryIterator<ActivityDocument>(It.IsAny<QueryDefinition>(), It.IsAny<string>(), It.IsAny<QueryRequestOptions>())).Returns(iterator.Object);

            // Act
            var result = await _repository.GetAllActivitySummaries();

            // Assert
            result.Should().BeEmpty();
        }

        [Fact]
        public async Task GetAllActivitySummaries_ShouldLogErrorAndThrowException_WhenExceptionOccurs()
        {
            // Arrange
            var exceptionMessage = "Test Exception";
            _containerMock.Setup(c => c.GetItemQueryIterator<ActivityDocument>(It.IsAny<QueryDefinition>(), It.IsAny<string>(), It.IsAny<QueryRequestOptions>()))
                          .Throws(new Exception(exceptionMessage));

            // Act
            Func<Task> act = async () => await _repository.GetAllActivitySummaries();

            // Assert
            await act.Should().ThrowAsync<Exception>().WithMessage(exceptionMessage);
            _loggerMock.VerifyLog(logger => logger.LogError($"Exception thrown in GetAllActivitySummaries: Test Exception"));
        }
    }
}
