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
        public async Task GetAllActivitySummariesPaginated_ShouldReturnPaginatedResults()
        {
            // Arrange
            var fixture = new Fixture();
            var activityDocuments = fixture.CreateMany<ActivityDocument>(10).ToList();
            var totalCount = 100;
            var request = new PaginationRequest { PageNumber = 2, PageSize = 10 };

            // Mock the count query
            var countFeedResponse = new Mock<FeedResponse<int>>();
            countFeedResponse.Setup(x => x.GetEnumerator()).Returns(new List<int> { totalCount }.GetEnumerator());

            var countIterator = new Mock<FeedIterator<int>>();
            countIterator.SetupSequence(x => x.HasMoreResults).Returns(true).Returns(false);
            countIterator.Setup(x => x.ReadNextAsync(default)).ReturnsAsync(countFeedResponse.Object);

            // Mock the data query
            var dataFeedResponse = new Mock<FeedResponse<ActivityDocument>>();
            dataFeedResponse.Setup(x => x.GetEnumerator()).Returns(activityDocuments.GetEnumerator());

            var dataIterator = new Mock<FeedIterator<ActivityDocument>>();
            dataIterator.SetupSequence(x => x.HasMoreResults).Returns(true).Returns(false);
            dataIterator.Setup(x => x.ReadNextAsync(default)).ReturnsAsync(dataFeedResponse.Object);

            _containerMock.Setup(x => x.GetItemQueryIterator<int>(
                It.Is<QueryDefinition>(q => q.QueryText.Contains("COUNT")),
                It.IsAny<string>(),
                It.IsAny<QueryRequestOptions>()))
                .Returns(countIterator.Object);

            _containerMock.Setup(x => x.GetItemQueryIterator<ActivityDocument>(
                It.Is<QueryDefinition>(q => q.QueryText.Contains("OFFSET") && q.QueryText.Contains("LIMIT")),
                It.IsAny<string>(),
                It.IsAny<QueryRequestOptions>()))
                .Returns(dataIterator.Object);

            // Act
            var result = await _repository.GetAllActivitySummaries(request);

            // Assert
            result.Should().NotBeNull();
            result.Items.Should().BeEquivalentTo(activityDocuments);
            result.PageNumber.Should().Be(2);
            result.PageSize.Should().Be(10);
            result.TotalCount.Should().Be(100);
            result.TotalPages.Should().Be(10);
            result.HasPreviousPage.Should().BeTrue();
            result.HasNextPage.Should().BeTrue();
        }

        [Fact]
        public async Task GetAllActivitySummariesPaginated_ShouldReturnCorrectPaginationMetadata_ForFirstPage()
        {
            // Arrange
            var fixture = new Fixture();
            var activityDocuments = fixture.CreateMany<ActivityDocument>(20).ToList();
            var totalCount = 50;
            var request = new PaginationRequest { PageNumber = 1, PageSize = 20 };

            SetupMocksForPagination(activityDocuments, totalCount);

            // Act
            var result = await _repository.GetAllActivitySummaries(request);

            // Assert
            result.PageNumber.Should().Be(1);
            result.PageSize.Should().Be(20);
            result.TotalCount.Should().Be(50);
            result.TotalPages.Should().Be(3);
            result.HasPreviousPage.Should().BeFalse();
            result.HasNextPage.Should().BeTrue();
        }

        [Fact]
        public async Task GetAllActivitySummariesPaginated_ShouldReturnCorrectPaginationMetadata_ForLastPage()
        {
            // Arrange
            var fixture = new Fixture();
            var activityDocuments = fixture.CreateMany<ActivityDocument>(10).ToList();
            var totalCount = 50;
            var request = new PaginationRequest { PageNumber = 3, PageSize = 20 };

            SetupMocksForPagination(activityDocuments, totalCount);

            // Act
            var result = await _repository.GetAllActivitySummaries(request);

            // Assert
            result.PageNumber.Should().Be(3);
            result.PageSize.Should().Be(20);
            result.TotalCount.Should().Be(50);
            result.TotalPages.Should().Be(3);
            result.HasPreviousPage.Should().BeTrue();
            result.HasNextPage.Should().BeFalse();
        }

        [Fact]
        public async Task GetAllActivitySummariesPaginated_ShouldUseCorrectOffsetAndLimit()
        {
            // Arrange
            var fixture = new Fixture();
            var activityDocuments = fixture.CreateMany<ActivityDocument>(15).ToList();
            var request = new PaginationRequest { PageNumber = 3, PageSize = 15 };

            SetupMocksForPagination(activityDocuments, 100);

            // Act
            await _repository.GetAllActivitySummaries(request);

            // Assert
            _containerMock.Verify(x => x.GetItemQueryIterator<ActivityDocument>(
                It.Is<QueryDefinition>(q =>
                    q.QueryText.Contains("OFFSET @offset LIMIT @limit")),
                It.IsAny<string>(),
                It.IsAny<QueryRequestOptions>()), Times.Once);
        }

        [Fact]
        public async Task GetAllActivitySummariesPaginated_ShouldHandleEmptyResults()
        {
            // Arrange
            var request = new PaginationRequest { PageNumber = 1, PageSize = 20 };

            SetupMocksForPagination(new List<ActivityDocument>(), 0);

            // Act
            var result = await _repository.GetAllActivitySummaries(request);

            // Assert
            result.Items.Should().BeEmpty();
            result.TotalCount.Should().Be(0);
            result.TotalPages.Should().Be(0);
            result.HasPreviousPage.Should().BeFalse();
            result.HasNextPage.Should().BeFalse();
        }

        [Fact]
        public async Task GetAllActivitySummariesPaginated_ShouldLogErrorAndThrowException_WhenExceptionOccurs()
        {
            // Arrange
            var request = new PaginationRequest { PageNumber = 1, PageSize = 20 };
            var exceptionMessage = "Test Exception";

            // Mock the main query to throw an exception, not the count query
            _containerMock.Setup(c => c.GetItemQueryIterator<ActivityDocument>(It.IsAny<QueryDefinition>(), It.IsAny<string>(), It.IsAny<QueryRequestOptions>()))
                          .Throws(new Exception(exceptionMessage));

            // Mock the count query to succeed so we get to the main query
            var countFeedResponse = new Mock<FeedResponse<int>>();
            countFeedResponse.Setup(x => x.GetEnumerator()).Returns(new List<int> { 100 }.GetEnumerator());

            var countIterator = new Mock<FeedIterator<int>>();
            countIterator.SetupSequence(x => x.HasMoreResults).Returns(true).Returns(false);
            countIterator.Setup(x => x.ReadNextAsync(default)).ReturnsAsync(countFeedResponse.Object);

            _containerMock.Setup(x => x.GetItemQueryIterator<int>(It.IsAny<QueryDefinition>(), It.IsAny<string>(), It.IsAny<QueryRequestOptions>()))
                          .Returns(countIterator.Object);

            // Act
            Func<Task> act = async () => await _repository.GetAllActivitySummaries(request);

            // Assert
            await act.Should().ThrowAsync<Exception>().WithMessage(exceptionMessage);
            _loggerMock.VerifyLog(logger => logger.LogError($"Exception thrown in GetAllActivitySummaries: Test Exception"));
        }

        private void SetupMocksForPagination(List<ActivityDocument> activityDocuments, int totalCount)
        {
            // Mock the count query
            var countFeedResponse = new Mock<FeedResponse<int>>();
            countFeedResponse.Setup(x => x.GetEnumerator()).Returns(new List<int> { totalCount }.GetEnumerator());

            var countIterator = new Mock<FeedIterator<int>>();
            countIterator.SetupSequence(x => x.HasMoreResults).Returns(true).Returns(false);
            countIterator.Setup(x => x.ReadNextAsync(default)).ReturnsAsync(countFeedResponse.Object);

            // Mock the data query
            var dataFeedResponse = new Mock<FeedResponse<ActivityDocument>>();
            dataFeedResponse.Setup(x => x.GetEnumerator()).Returns(activityDocuments.GetEnumerator());

            var dataIterator = new Mock<FeedIterator<ActivityDocument>>();
            dataIterator.SetupSequence(x => x.HasMoreResults).Returns(true).Returns(false);
            dataIterator.Setup(x => x.ReadNextAsync(default)).ReturnsAsync(dataFeedResponse.Object);

            _containerMock.Setup(x => x.GetItemQueryIterator<int>(
                It.Is<QueryDefinition>(q => q.QueryText.Contains("COUNT")),
                It.IsAny<string>(),
                It.IsAny<QueryRequestOptions>()))
                .Returns(countIterator.Object);

            _containerMock.Setup(x => x.GetItemQueryIterator<ActivityDocument>(
                It.Is<QueryDefinition>(q => q.QueryText.Contains("OFFSET") && q.QueryText.Contains("LIMIT")),
                It.IsAny<string>(),
                It.IsAny<QueryRequestOptions>()))
                .Returns(dataIterator.Object);
        }
    }
}
