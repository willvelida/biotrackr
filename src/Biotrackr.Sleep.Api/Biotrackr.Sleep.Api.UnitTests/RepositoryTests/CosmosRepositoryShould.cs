using AutoFixture;
using Biotrackr.Sleep.Api.Configuration;
using Biotrackr.Sleep.Api.Models;
using Biotrackr.Sleep.Api.Repositories;
using Biotrackr.Sleep.Api.Repositories.Interfaces;
using FluentAssertions;
using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Biotrackr.Sleep.Api.UnitTests.RepositoryTests
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
        public async Task GetSleepSummaryByDate_ShouldReturnSleepDocument()
        {
            // Arrange
            var date = "2022-01-01";
            var fixture = new Fixture();
            var sleepDocument = fixture.Create<SleepDocument>();
            sleepDocument.Date = date;

            var feedResponse = new Mock<FeedResponse<SleepDocument>>();
            feedResponse.Setup(x => x.GetEnumerator()).Returns(new List<SleepDocument> { sleepDocument }.GetEnumerator());

            var iterator = new Mock<FeedIterator<SleepDocument>>();
            iterator.SetupSequence(x => x.HasMoreResults).Returns(true).Returns(false);
            iterator.Setup(x => x.ReadNextAsync(default)).ReturnsAsync(feedResponse.Object);

            _containerMock.Setup(x => x.GetItemQueryIterator<SleepDocument>(It.IsAny<QueryDefinition>(), It.IsAny<string>(), It.IsAny<QueryRequestOptions>())).Returns(iterator.Object);

            // Act
            var result = await _repository.GetSleepSummaryByDate(date);

            // Assert
            result.Should().BeEquivalentTo(sleepDocument);
            result.Date.Should().Be(date);
        }

        [Fact]
        public async Task GetSleepSummaryByDate_ShouldReturnNull_WhenSleepDoesNotExist()
        {
            // Arrange
            var date = "2022-01-01";

            var feedResponse = new Mock<FeedResponse<SleepDocument>>();
            feedResponse.Setup(x => x.GetEnumerator()).Returns(new List<SleepDocument>().GetEnumerator());

            var iterator = new Mock<FeedIterator<SleepDocument>>();
            iterator.SetupSequence(x => x.HasMoreResults).Returns(true).Returns(false);
            iterator.Setup(x => x.ReadNextAsync(default)).ReturnsAsync(feedResponse.Object);

            _containerMock.Setup(x => x.GetItemQueryIterator<SleepDocument>(It.IsAny<QueryDefinition>(), It.IsAny<string>(), It.IsAny<QueryRequestOptions>())).Returns(iterator.Object);

            // Act
            var result = await _repository.GetSleepSummaryByDate(date);

            // Assert
            result.Should().BeNull();
        }

        [Fact]
        public async Task GetSleepSummaryByDate_ShouldLogErrorAndThrowException_WhenExceptionOccurs()
        {
            // Arrange
            var date = "2022-01-01";
            var exceptionMessage = "Test Exception";
            _containerMock.Setup(c => c.GetItemQueryIterator<SleepDocument>(It.IsAny<QueryDefinition>(), It.IsAny<string>(), It.IsAny<QueryRequestOptions>()))
                          .Throws(new Exception(exceptionMessage));

            // Act
            Func<Task> act = async () => await _repository.GetSleepSummaryByDate(date);

            // Assert
            await act.Should().ThrowAsync<Exception>().WithMessage(exceptionMessage);
            _loggerMock.VerifyLog(logger => logger.LogError($"Exception thrown in GetSleepSummaryByDate: Test Exception"));
        }

        [Fact]
        public async Task GetAllSleepDocuments_ShouldReturnListOfSleepDocuments()
        {
            // Arrange
            var fixture = new Fixture();
            var sleepDocuments = fixture.CreateMany<SleepDocument>().ToList();

            var feedResponse = new Mock<FeedResponse<SleepDocument>>();
            feedResponse.Setup(f => f.GetEnumerator()).Returns(sleepDocuments.GetEnumerator());

            var mockFeedIterator = new Mock<FeedIterator<SleepDocument>>();
            mockFeedIterator.SetupSequence(i => i.HasMoreResults)
                .Returns(true)
                .Returns(false);
            mockFeedIterator.Setup(i => i.ReadNextAsync(default))
                .ReturnsAsync(feedResponse.Object);

            _containerMock.Setup(c => c.GetItemQueryIterator<SleepDocument>(It.IsAny<QueryDefinition>(), It.IsAny<string>(), It.IsAny<QueryRequestOptions>()))
                .Returns(mockFeedIterator.Object);

            // Act
            var result = await _repository.GetAllSleepDocuments();

            // Assert
            result.Should().BeEquivalentTo(sleepDocuments);
        }

        [Fact]
        public async Task GetAllSleepDocuments_ShouldReturnEmptyList_WhenNoSleepDocumentsExist()
        {
            // Arrange
            var feedResponse = new Mock<FeedResponse<SleepDocument>>();
            feedResponse.Setup(f => f.GetEnumerator()).Returns(new List<SleepDocument>().GetEnumerator());

            var mockFeedIterator = new Mock<FeedIterator<SleepDocument>>();
            mockFeedIterator.SetupSequence(i => i.HasMoreResults)
                .Returns(true)
                .Returns(false);
            mockFeedIterator.Setup(i => i.ReadNextAsync(default))
                .ReturnsAsync(feedResponse.Object);

            _containerMock.Setup(c => c.GetItemQueryIterator<SleepDocument>(It.IsAny<QueryDefinition>(), It.IsAny<string>(), It.IsAny<QueryRequestOptions>()))
                .Returns(mockFeedIterator.Object);

            // Act
            var result = await _repository.GetAllSleepDocuments();

            // Assert
            result.Should().BeEmpty();
        }

        [Fact]
        public async Task GetAllSleepDocuments_ShouldLogErrorAndThrowException_WhenExceptionOccurs()
        {
            // Arrange
            var exceptionMessage = "Test Exception";
            _containerMock.Setup(c => c.GetItemQueryIterator<SleepDocument>(It.IsAny<QueryDefinition>(), It.IsAny<string>(), It.IsAny<QueryRequestOptions>()))
                          .Throws(new Exception(exceptionMessage));
            // Act
            Func<Task> act = async () => await _repository.GetAllSleepDocuments();

            // Assert
            await act.Should().ThrowAsync<Exception>().WithMessage(exceptionMessage);
            _loggerMock.VerifyLog(logger => logger.LogError($"Exception thrown in GetAllSleepDocuments: Test Exception"));
        }
    }
}
