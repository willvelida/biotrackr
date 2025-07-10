using AutoFixture;
using Biotrackr.Sleep.Api.Configuration;
using Biotrackr.Sleep.Api.Models;
using Biotrackr.Sleep.Api.Repositories;
using FluentAssertions;
using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;

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
        public async Task GetAllSleepDocuments_ShouldReturnPaginationResponseWithSleepDocuments()
        {
            // Arrange
            var fixture = new Fixture();
            var sleepDocuments = fixture.CreateMany<SleepDocument>(5).ToList();
            var totalCount = 10;
            var request = new PaginationRequest { PageNumber = 1, PageSize = 5 };

            SetupMocksForPagination(sleepDocuments, totalCount);

            // Act
            var result = await _repository.GetAllSleepDocuments(request);

            // Assert
            result.Should().NotBeNull();
            result.Items.Should().BeEquivalentTo(sleepDocuments);
            result.TotalCount.Should().Be(totalCount);
            result.PageNumber.Should().Be(request.PageNumber);
            result.PageSize.Should().Be(request.PageSize);
        }

        [Fact]
        public async Task GetAllSleepDocuments_ShouldReturnEmptyPaginationResponse_WhenNoSleepDocumentsExist()
        {
            // Arrange
            var request = new PaginationRequest { PageNumber = 1, PageSize = 5 };

            SetupMocksForPagination(new List<SleepDocument>(), 0);

            // Act
            var result = await _repository.GetAllSleepDocuments(request);

            // Assert
            result.Should().NotBeNull();
            result.Items.Should().BeEmpty();
            result.TotalCount.Should().Be(0);
            result.PageNumber.Should().Be(request.PageNumber);
            result.PageSize.Should().Be(request.PageSize);
        }

        [Fact]
        public async Task GetAllSleepDocuments_ShouldLogErrorAndThrowException_WhenExceptionOccurs()
        {
            // Arrange
            var request = new PaginationRequest { PageNumber = 1, PageSize = 5 };
            var exceptionMessage = "Test Exception";

            // Setup count query to succeed
            var countFeedResponse = new Mock<FeedResponse<int>>();
            countFeedResponse.Setup(f => f.GetEnumerator()).Returns(new List<int> { 10 }.GetEnumerator());

            var mockCountIterator = new Mock<FeedIterator<int>>();
            mockCountIterator.SetupSequence(i => i.HasMoreResults)
                .Returns(true)
                .Returns(false);
            mockCountIterator.Setup(i => i.ReadNextAsync(default))
                .ReturnsAsync(countFeedResponse.Object);

            _containerMock.Setup(c => c.GetItemQueryIterator<int>(
                It.Is<QueryDefinition>(q => q.QueryText.Contains("COUNT")),
                It.IsAny<string>(),
                It.IsAny<QueryRequestOptions>()))
                .Returns(mockCountIterator.Object);

            // Setup main query to throw exception
            _containerMock.Setup(c => c.GetItemQueryIterator<SleepDocument>(
                It.Is<QueryDefinition>(q => q.QueryText.Contains("ORDER BY")),
                It.IsAny<string>(),
                It.IsAny<QueryRequestOptions>()))
                .Throws(new Exception(exceptionMessage));

            // Act
            Func<Task> act = async () => await _repository.GetAllSleepDocuments(request);

            // Assert
            await act.Should().ThrowAsync<Exception>().WithMessage(exceptionMessage);
            _loggerMock.VerifyLog(logger => logger.LogError($"Exception thrown in GetAllSleepDocuments: Test Exception"));
        }

        [Fact]
        public async Task GetAllSleepDocuments_ShouldReturnPaginatedResults()
        {
            // Arrange
            var fixture = new Fixture();
            var sleepDocuments = fixture.CreateMany<SleepDocument>(10).ToList();
            var totalCount = 100;
            var request = new PaginationRequest { PageNumber = 2, PageSize = 10 };

            SetupMocksForPagination(sleepDocuments, totalCount);

            // Act
            var result = await _repository.GetAllSleepDocuments(request);

            // Assert
            result.Should().NotBeNull();
            result.Items.Should().BeEquivalentTo(sleepDocuments);
            result.PageNumber.Should().Be(2);
            result.PageSize.Should().Be(10);
            result.TotalCount.Should().Be(100);
            result.TotalPages.Should().Be(10);
            result.HasPreviousPage.Should().BeTrue();
            result.HasNextPage.Should().BeTrue();
        }

        [Fact]
        public async Task GetAllSleepDocuments_ShouldReturnCorrectPaginationMetadata_ForFirstPage()
        {
            // Arrange
            var fixture = new Fixture();
            var sleepDocuments = fixture.CreateMany<SleepDocument>(20).ToList();
            var totalCount = 50;
            var request = new PaginationRequest { PageNumber = 1, PageSize = 20 };

            SetupMocksForPagination(sleepDocuments, totalCount);

            // Act
            var result = await _repository.GetAllSleepDocuments(request);

            // Assert
            result.PageNumber.Should().Be(1);
            result.PageSize.Should().Be(20);
            result.TotalCount.Should().Be(50);
            result.TotalPages.Should().Be(3);
            result.HasPreviousPage.Should().BeFalse();
            result.HasNextPage.Should().BeTrue();
        }

        [Fact]
        public async Task GetAllSleepDocuments_ShouldReturnCorrectPaginationMetadata_ForLastPage()
        {
            // Arrange
            var fixture = new Fixture();
            var sleepDocuments = fixture.CreateMany<SleepDocument>(10).ToList();
            var totalCount = 50;
            var request = new PaginationRequest { PageNumber = 3, PageSize = 20 };

            SetupMocksForPagination(sleepDocuments, totalCount);

            // Act
            var result = await _repository.GetAllSleepDocuments(request);

            // Assert
            result.PageNumber.Should().Be(3);
            result.PageSize.Should().Be(20);
            result.TotalCount.Should().Be(50);
            result.TotalPages.Should().Be(3);
            result.HasPreviousPage.Should().BeTrue();
            result.HasNextPage.Should().BeFalse();
        }

        [Fact]
        public async Task GetAllSleepDocuments_ShouldUseCorrectOffsetAndLimit()
        {
            // Arrange
            var fixture = new Fixture();
            var sleepDocuments = fixture.CreateMany<SleepDocument>(15).ToList();
            var request = new PaginationRequest { PageNumber = 3, PageSize = 15 };

            SetupMocksForPagination(sleepDocuments, 100);

            // Act
            await _repository.GetAllSleepDocuments(request);

            // Assert
            _containerMock.Verify(x => x.GetItemQueryIterator<SleepDocument>(
                It.Is<QueryDefinition>(q =>
                    q.QueryText.Contains("OFFSET @offset LIMIT @limit")),
                It.IsAny<string>(),
                It.IsAny<QueryRequestOptions>()), Times.Once);
        }

        [Fact]
        public async Task GetAllSleepDocuments_ShouldHandleEmptyResults()
        {
            // Arrange
            var request = new PaginationRequest { PageNumber = 1, PageSize = 20 };

            SetupMocksForPagination(new List<SleepDocument>(), 0);

            // Act
            var result = await _repository.GetAllSleepDocuments(request);

            // Assert
            result.Items.Should().BeEmpty();
            result.TotalCount.Should().Be(0);
            result.TotalPages.Should().Be(0);
            result.HasPreviousPage.Should().BeFalse();
            result.HasNextPage.Should().BeFalse();
        }

        [Fact]
        public async Task GetAllSleepDocuments_ShouldUsePaginationParametersCorrectly()
        {
            // Arrange
            var request = new PaginationRequest { PageNumber = 2, PageSize = 10 };
            var expectedSkip = 10; // (2-1) * 10
            var sleepDocuments = new List<SleepDocument>();
            var totalCount = 25;

            SetupMocksForPagination(sleepDocuments, totalCount);

            // Act
            var result = await _repository.GetAllSleepDocuments(request);

            // Assert
            result.Should().NotBeNull();
            result.PageNumber.Should().Be(2);
            result.PageSize.Should().Be(10);
            result.TotalCount.Should().Be(totalCount);

            // Verify that the correct query was constructed with pagination parameters
            _containerMock.Verify(c => c.GetItemQueryIterator<SleepDocument>(
                It.Is<QueryDefinition>(q => q.QueryText.Contains("OFFSET") && q.QueryText.Contains("LIMIT")),
                It.IsAny<string>(),
                It.IsAny<QueryRequestOptions>()), Times.Once);
        }

        [Fact]
        public async Task GetAllSleepDocuments_ShouldContinueWhenCountQueryFails()
        {
            // Arrange
            var request = new PaginationRequest { PageNumber = 1, PageSize = 5 };
            var fixture = new Fixture();
            var sleepDocuments = fixture.CreateMany<SleepDocument>(3).ToList();

            // Setup count query to fail (GetTotalSleepCount will return 0)
            _containerMock.Setup(c => c.GetItemQueryIterator<int>(
                It.Is<QueryDefinition>(q => q.QueryText.Contains("COUNT")),
                It.IsAny<string>(),
                It.IsAny<QueryRequestOptions>()))
                .Throws(new Exception("Count query failed"));

            // Setup for sleep documents query to succeed
            var feedResponse = new Mock<FeedResponse<SleepDocument>>();
            feedResponse.Setup(f => f.GetEnumerator()).Returns(sleepDocuments.GetEnumerator());

            var mockFeedIterator = new Mock<FeedIterator<SleepDocument>>();
            mockFeedIterator.SetupSequence(i => i.HasMoreResults)
                .Returns(true)
                .Returns(false);
            mockFeedIterator.Setup(i => i.ReadNextAsync(default))
                .ReturnsAsync(feedResponse.Object);

            _containerMock.Setup(c => c.GetItemQueryIterator<SleepDocument>(
                It.Is<QueryDefinition>(q => q.QueryText.Contains("ORDER BY")),
                It.IsAny<string>(),
                It.IsAny<QueryRequestOptions>()))
                .Returns(mockFeedIterator.Object);

            // Act
            var result = await _repository.GetAllSleepDocuments(request);

            // Assert
            result.Should().NotBeNull();
            result.Items.Should().BeEquivalentTo(sleepDocuments);
            result.TotalCount.Should().Be(0); // Should be 0 because count query failed
            result.PageNumber.Should().Be(request.PageNumber);
            result.PageSize.Should().Be(request.PageSize);
        }

        private void SetupMocksForPagination(List<SleepDocument> sleepDocuments, int totalCount)
        {
            // Mock the count query
            var countFeedResponse = new Mock<FeedResponse<int>>();
            countFeedResponse.Setup(x => x.GetEnumerator()).Returns(new List<int> { totalCount }.GetEnumerator());

            var countIterator = new Mock<FeedIterator<int>>();
            countIterator.SetupSequence(x => x.HasMoreResults).Returns(true).Returns(false);
            countIterator.Setup(x => x.ReadNextAsync(default)).ReturnsAsync(countFeedResponse.Object);

            // Mock the data query
            var dataFeedResponse = new Mock<FeedResponse<SleepDocument>>();
            dataFeedResponse.Setup(x => x.GetEnumerator()).Returns(sleepDocuments.GetEnumerator());

            var dataIterator = new Mock<FeedIterator<SleepDocument>>();
            dataIterator.SetupSequence(x => x.HasMoreResults).Returns(true).Returns(false);
            dataIterator.Setup(x => x.ReadNextAsync(default)).ReturnsAsync(dataFeedResponse.Object);

            _containerMock.Setup(x => x.GetItemQueryIterator<int>(
                It.Is<QueryDefinition>(q => q.QueryText.Contains("COUNT")),
                It.IsAny<string>(),
                It.IsAny<QueryRequestOptions>()))
                .Returns(countIterator.Object);

            _containerMock.Setup(x => x.GetItemQueryIterator<SleepDocument>(
                It.Is<QueryDefinition>(q => q.QueryText.Contains("OFFSET") && q.QueryText.Contains("LIMIT")),
                It.IsAny<string>(),
                It.IsAny<QueryRequestOptions>()))
                .Returns(dataIterator.Object);
        }
    }
}
