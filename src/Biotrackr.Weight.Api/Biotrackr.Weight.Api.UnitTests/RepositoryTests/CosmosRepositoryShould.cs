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
                DatabaseName = "TestDatabase",
                ContainerName = "TestContainer"
            });

            _cosmosClientMock.Setup(x => x.GetContainer(It.IsAny<string>(), It.IsAny<string>()))
                .Returns(_containerMock.Object);
            _loggerMock = new Mock<ILogger<CosmosRepository>>();
            _repository = new CosmosRepository(_cosmosClientMock.Object, _optionsMock.Object, _loggerMock.Object);
        }

        [Fact]
        public async Task GetWeightDocumentByDate_ShouldReturnWeightDocument()
        {
            // Arrange
            var date = "2022-01-01";
            var fixture = new Fixture();
            var weightDocument = fixture.Create<WeightDocument>();
            weightDocument.Date = date;

            var feedResponse = new Mock<FeedResponse<WeightDocument>>();
            feedResponse.Setup(x => x.GetEnumerator()).Returns(new List<WeightDocument> { weightDocument }.GetEnumerator());

            var iterator = new Mock<FeedIterator<WeightDocument>>();
            iterator.SetupSequence(x => x.HasMoreResults).Returns(true).Returns(false);
            iterator.Setup(x => x.ReadNextAsync(default)).ReturnsAsync(feedResponse.Object);

            _containerMock.Setup(x => x.GetItemQueryIterator<WeightDocument>(It.IsAny<QueryDefinition>(), It.IsAny<string>(), It.IsAny<QueryRequestOptions>())).Returns(iterator.Object);

            // Act
            var result = await _repository.GetWeightDocumentByDate(date);

            // Assert
            result.Should().BeEquivalentTo(weightDocument);
            result.Date.Should().Be(date);
        }

        [Fact]
        public async Task GetWeightDocumentByDate_ShouldReturnNull_WhenWeightDoesNotExist()
        {
            // Arrange
            var date = "2022-01-01";

            var feedResponse = new Mock<FeedResponse<WeightDocument>>();
            feedResponse.Setup(x => x.GetEnumerator()).Returns(new List<WeightDocument>().GetEnumerator());

            var iterator = new Mock<FeedIterator<WeightDocument>>();
            iterator.SetupSequence(x => x.HasMoreResults).Returns(true).Returns(false);
            iterator.Setup(x => x.ReadNextAsync(default)).ReturnsAsync(feedResponse.Object);

            _containerMock.Setup(x => x.GetItemQueryIterator<WeightDocument>(It.IsAny<QueryDefinition>(), It.IsAny<string>(), It.IsAny<QueryRequestOptions>())).Returns(iterator.Object);

            // Act
            var result = await _repository.GetWeightDocumentByDate(date);

            // Assert
            result.Should().BeNull();
        }

        [Fact]
        public async Task GetWeightDocumentByDate_ShouldLogErrorAndThrowException_WhenExceptionOccurs()
        {
            // Arrange
            var date = "2022-01-01";
            var exceptionMessage = "Test Exception";
            _containerMock.Setup(c => c.GetItemQueryIterator<WeightDocument>(It.IsAny<QueryDefinition>(), It.IsAny<string>(), It.IsAny<QueryRequestOptions>()))
                          .Throws(new Exception(exceptionMessage));

            // Act
            Func<Task> act = async () => await _repository.GetWeightDocumentByDate(date);

            // Assert
            await act.Should().ThrowAsync<Exception>().WithMessage(exceptionMessage);
            _loggerMock.Verify(
                x => x.Log(
                    LogLevel.Error,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString().Contains($"Exception thrown in {nameof(CosmosRepository.GetWeightDocumentByDate)}: {exceptionMessage}")),
                    It.IsAny<Exception>(),
                    It.Is<Func<It.IsAnyType, Exception, string>>((v, t) => true)),
                Times.Once);
        }

        [Fact]
        public async Task GetAllWeightDocuments_ShouldReturnPaginationResponseWithWeightDocuments()
        {
            // Arrange
            var fixture = new Fixture();
            var weightDocuments = fixture.CreateMany<WeightDocument>(5).ToList();
            var totalCount = 10;
            var request = new PaginationRequest { PageNumber = 1, PageSize = 5 };

            SetupMocksForPagination(weightDocuments, totalCount);

            // Act
            var result = await _repository.GetAllWeightDocuments(request);

            // Assert
            result.Should().NotBeNull();
            result.Items.Should().BeEquivalentTo(weightDocuments);
            result.TotalCount.Should().Be(totalCount);
            result.PageNumber.Should().Be(request.PageNumber);
            result.PageSize.Should().Be(request.PageSize);
        }

        [Fact]
        public async Task GetAllWeightDocuments_ShouldReturnEmptyPaginationResponse_WhenNoWeightDocumentsExist()
        {
            // Arrange
            var request = new PaginationRequest { PageNumber = 1, PageSize = 5 };

            SetupMocksForPagination(new List<WeightDocument>(), 0);

            // Act
            var result = await _repository.GetAllWeightDocuments(request);

            // Assert
            result.Should().NotBeNull();
            result.Items.Should().BeEmpty();
            result.TotalCount.Should().Be(0);
            result.PageNumber.Should().Be(request.PageNumber);
            result.PageSize.Should().Be(request.PageSize);
        }

        [Fact]
        public async Task GetAllWeightDocuments_ShouldLogErrorAndThrowException_WhenExceptionOccurs()
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
            _containerMock.Setup(c => c.GetItemQueryIterator<WeightDocument>(
                It.Is<QueryDefinition>(q => q.QueryText.Contains("ORDER BY")),
                It.IsAny<string>(),
                It.IsAny<QueryRequestOptions>()))
                .Throws(new Exception(exceptionMessage));

            // Act
            Func<Task> act = async () => await _repository.GetAllWeightDocuments(request);

            // Assert
            await act.Should().ThrowAsync<Exception>().WithMessage(exceptionMessage);
            _loggerMock.Verify(
                x => x.Log(
                    LogLevel.Error,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString().Contains($"Exception thrown in {nameof(CosmosRepository.GetAllWeightDocuments)}: {exceptionMessage}")),
                    It.IsAny<Exception>(),
                    It.Is<Func<It.IsAnyType, Exception, string>>((v, t) => true)),
                Times.Once);
        }

        [Fact]
        public async Task GetAllWeightDocuments_ShouldReturnPaginatedResults()
        {
            // Arrange
            var fixture = new Fixture();
            var weightDocuments = fixture.CreateMany<WeightDocument>(10).ToList();
            var totalCount = 100;
            var request = new PaginationRequest { PageNumber = 2, PageSize = 10 };

            SetupMocksForPagination(weightDocuments, totalCount);

            // Act
            var result = await _repository.GetAllWeightDocuments(request);

            // Assert
            result.Should().NotBeNull();
            result.Items.Should().BeEquivalentTo(weightDocuments);
            result.PageNumber.Should().Be(2);
            result.PageSize.Should().Be(10);
            result.TotalCount.Should().Be(100);
            result.TotalPages.Should().Be(10);
            result.HasPreviousPage.Should().BeTrue();
            result.HasNextPage.Should().BeTrue();
        }

        [Fact]
        public async Task GetAllWeightDocuments_ShouldReturnCorrectPaginationMetadata_ForFirstPage()
        {
            // Arrange
            var fixture = new Fixture();
            var weightDocuments = fixture.CreateMany<WeightDocument>(20).ToList();
            var totalCount = 50;
            var request = new PaginationRequest { PageNumber = 1, PageSize = 20 };

            SetupMocksForPagination(weightDocuments, totalCount);

            // Act
            var result = await _repository.GetAllWeightDocuments(request);

            // Assert
            result.PageNumber.Should().Be(1);
            result.PageSize.Should().Be(20);
            result.TotalCount.Should().Be(50);
            result.TotalPages.Should().Be(3);
            result.HasPreviousPage.Should().BeFalse();
            result.HasNextPage.Should().BeTrue();
        }

        [Fact]
        public async Task GetAllWeightDocuments_ShouldReturnCorrectPaginationMetadata_ForLastPage()
        {
            // Arrange
            var fixture = new Fixture();
            var weightDocuments = fixture.CreateMany<WeightDocument>(10).ToList();
            var totalCount = 50;
            var request = new PaginationRequest { PageNumber = 3, PageSize = 20 };

            SetupMocksForPagination(weightDocuments, totalCount);

            // Act
            var result = await _repository.GetAllWeightDocuments(request);

            // Assert
            result.PageNumber.Should().Be(3);
            result.PageSize.Should().Be(20);
            result.TotalCount.Should().Be(50);
            result.TotalPages.Should().Be(3);
            result.HasPreviousPage.Should().BeTrue();
            result.HasNextPage.Should().BeFalse();
        }

        [Fact]
        public async Task GetAllWeightDocuments_ShouldUseCorrectOffsetAndLimit()
        {
            // Arrange
            var fixture = new Fixture();
            var weightDocuments = fixture.CreateMany<WeightDocument>(15).ToList();
            var request = new PaginationRequest { PageNumber = 3, PageSize = 15 };

            SetupMocksForPagination(weightDocuments, 100);

            // Act
            await _repository.GetAllWeightDocuments(request);

            // Assert
            _containerMock.Verify(x => x.GetItemQueryIterator<WeightDocument>(
                It.Is<QueryDefinition>(q =>
                    q.QueryText.Contains("OFFSET @offset LIMIT @limit")),
                It.IsAny<string>(),
                It.IsAny<QueryRequestOptions>()), Times.Once);
        }

        [Fact]
        public async Task GetAllWeightDocuments_ShouldHandleEmptyResults()
        {
            // Arrange
            var request = new PaginationRequest { PageNumber = 1, PageSize = 20 };

            SetupMocksForPagination(new List<WeightDocument>(), 0);

            // Act
            var result = await _repository.GetAllWeightDocuments(request);

            // Assert
            result.Items.Should().BeEmpty();
            result.TotalCount.Should().Be(0);
            result.TotalPages.Should().Be(0);
            result.HasPreviousPage.Should().BeFalse();
            result.HasNextPage.Should().BeFalse();
        }

        [Fact]
        public async Task GetAllWeightDocuments_ShouldUsePaginationParametersCorrectly()
        {
            // Arrange
            var request = new PaginationRequest { PageNumber = 2, PageSize = 10 };
            var expectedSkip = 10; // (2-1) * 10
            var weightDocuments = new List<WeightDocument>();
            var totalCount = 25;

            SetupMocksForPagination(weightDocuments, totalCount);

            // Act
            var result = await _repository.GetAllWeightDocuments(request);

            // Assert
            result.Should().NotBeNull();
            result.PageNumber.Should().Be(2);
            result.PageSize.Should().Be(10);
            result.TotalCount.Should().Be(totalCount);

            // Verify that the correct query was constructed with pagination parameters
            _containerMock.Verify(c => c.GetItemQueryIterator<WeightDocument>(
                It.Is<QueryDefinition>(q => q.QueryText.Contains("OFFSET") && q.QueryText.Contains("LIMIT")),
                It.IsAny<string>(),
                It.IsAny<QueryRequestOptions>()), Times.Once);
        }

        [Fact]
        public async Task GetAllWeightDocuments_ShouldContinueWhenCountQueryFails()
        {
            // Arrange
            var request = new PaginationRequest { PageNumber = 1, PageSize = 5 };
            var fixture = new Fixture();
            var weightDocuments = fixture.CreateMany<WeightDocument>(3).ToList();

            // Setup count query to fail (GetTotalWeightCount will return 0)
            _containerMock.Setup(c => c.GetItemQueryIterator<int>(
                It.Is<QueryDefinition>(q => q.QueryText.Contains("COUNT")),
                It.IsAny<string>(),
                It.IsAny<QueryRequestOptions>()))
                .Throws(new Exception("Count query failed"));

            // Setup for weight documents query to succeed
            var feedResponse = new Mock<FeedResponse<WeightDocument>>();
            feedResponse.Setup(f => f.GetEnumerator()).Returns(weightDocuments.GetEnumerator());

            var mockFeedIterator = new Mock<FeedIterator<WeightDocument>>();
            mockFeedIterator.SetupSequence(i => i.HasMoreResults)
                .Returns(true)
                .Returns(false);
            mockFeedIterator.Setup(i => i.ReadNextAsync(default))
                .ReturnsAsync(feedResponse.Object);

            _containerMock.Setup(c => c.GetItemQueryIterator<WeightDocument>(
                It.Is<QueryDefinition>(q => q.QueryText.Contains("ORDER BY")),
                It.IsAny<string>(),
                It.IsAny<QueryRequestOptions>()))
                .Returns(mockFeedIterator.Object);

            // Act
            var result = await _repository.GetAllWeightDocuments(request);

            // Assert
            result.Should().NotBeNull();
            result.Items.Should().BeEquivalentTo(weightDocuments);
            result.TotalCount.Should().Be(0); // Should be 0 because count query failed
            result.PageNumber.Should().Be(request.PageNumber);
            result.PageSize.Should().Be(request.PageSize);
        }

        [Fact]
        public async Task GetWeightsByDateRange_ShouldReturnPaginatedWeightDocuments_WhenWeightsExistInRange()
        {
            // Arrange
            var startDate = "2022-01-01";
            var endDate = "2022-01-31";
            var paginationRequest = new PaginationRequest { PageNumber = 1, PageSize = 5 };
            var fixture = new Fixture();
            var weightDocuments = fixture.CreateMany<WeightDocument>(5).ToList();
            var totalCount = 15;

            // Set dates within range for test data
            for (int i = 0; i < weightDocuments.Count; i++)
            {
                weightDocuments[i].Date = $"2022-01-{(i + 1):D2}";
            }

            // Setup count query
            var countFeedResponse = new Mock<FeedResponse<int>>();
            countFeedResponse.Setup(x => x.GetEnumerator()).Returns(new List<int> { totalCount }.GetEnumerator());

            var countIterator = new Mock<FeedIterator<int>>();
            countIterator.SetupSequence(x => x.HasMoreResults).Returns(true).Returns(false);
            countIterator.Setup(x => x.ReadNextAsync(default)).ReturnsAsync(countFeedResponse.Object);

            _containerMock.Setup(x => x.GetItemQueryIterator<int>(
                It.Is<QueryDefinition>(q => q.QueryText.Contains("COUNT(1)")),
                It.IsAny<string>(),
                It.IsAny<QueryRequestOptions>()))
                .Returns(countIterator.Object);

            // Setup main query
            var feedResponse = new Mock<FeedResponse<WeightDocument>>();
            feedResponse.Setup(x => x.GetEnumerator()).Returns(weightDocuments.GetEnumerator());

            var iterator = new Mock<FeedIterator<WeightDocument>>();
            iterator.SetupSequence(x => x.HasMoreResults).Returns(true).Returns(false);
            iterator.Setup(x => x.ReadNextAsync(default)).ReturnsAsync(feedResponse.Object);

            _containerMock.Setup(x => x.GetItemQueryIterator<WeightDocument>(
                It.Is<QueryDefinition>(q => q.QueryText.Contains("OFFSET @offset LIMIT @limit")),
                It.IsAny<string>(),
                It.IsAny<QueryRequestOptions>()))
                .Returns(iterator.Object);

            // Act
            var result = await _repository.GetWeightsByDateRange(startDate, endDate, paginationRequest);

            // Assert
            result.Should().NotBeNull();
            result.Items.Should().HaveCount(5);
            result.Items.Should().BeEquivalentTo(weightDocuments);
            result.TotalCount.Should().Be(totalCount);
            result.PageNumber.Should().Be(1);
            result.PageSize.Should().Be(5);
        }

        [Fact]
        public async Task GetWeightsByDateRange_ShouldReturnCorrectPaginationMetadata()
        {
            // Arrange
            var startDate = "2022-01-01";
            var endDate = "2022-01-31";
            var paginationRequest = new PaginationRequest { PageNumber = 2, PageSize = 10 };
            var weightDocuments = new List<WeightDocument>();
            var totalCount = 25;

            SetupMocksForDateRangePagination(weightDocuments, totalCount);

            // Act
            var result = await _repository.GetWeightsByDateRange(startDate, endDate, paginationRequest);

            // Assert
            result.PageNumber.Should().Be(2);
            result.PageSize.Should().Be(10);
            result.TotalCount.Should().Be(25);
            result.TotalPages.Should().Be(3);
            result.HasPreviousPage.Should().BeTrue();
            result.HasNextPage.Should().BeTrue();
        }

        private void SetupMocksForPagination(List<WeightDocument> weightDocuments, int totalCount)
        {
            // Mock the count query
            var countFeedResponse = new Mock<FeedResponse<int>>();
            countFeedResponse.Setup(x => x.GetEnumerator()).Returns(new List<int> { totalCount }.GetEnumerator());

            var countIterator = new Mock<FeedIterator<int>>();
            countIterator.SetupSequence(x => x.HasMoreResults).Returns(true).Returns(false);
            countIterator.Setup(x => x.ReadNextAsync(default)).ReturnsAsync(countFeedResponse.Object);

            // Mock the data query
            var dataFeedResponse = new Mock<FeedResponse<WeightDocument>>();
            dataFeedResponse.Setup(x => x.GetEnumerator()).Returns(weightDocuments.GetEnumerator());

            var dataIterator = new Mock<FeedIterator<WeightDocument>>();
            dataIterator.SetupSequence(x => x.HasMoreResults).Returns(true).Returns(false);
            dataIterator.Setup(x => x.ReadNextAsync(default)).ReturnsAsync(dataFeedResponse.Object);

            _containerMock.Setup(x => x.GetItemQueryIterator<int>(
                It.Is<QueryDefinition>(q => q.QueryText.Contains("COUNT")),
                It.IsAny<string>(),
                It.IsAny<QueryRequestOptions>()))
                .Returns(countIterator.Object);

            _containerMock.Setup(x => x.GetItemQueryIterator<WeightDocument>(
                It.Is<QueryDefinition>(q => q.QueryText.Contains("OFFSET") && q.QueryText.Contains("LIMIT")),
                It.IsAny<string>(),
                It.IsAny<QueryRequestOptions>()))
                .Returns(dataIterator.Object);
        }

        private void SetupMocksForDateRangePagination(List<WeightDocument> weightDocuments, int totalCount)
        {
            // Setup count query
            var countFeedResponse = new Mock<FeedResponse<int>>();
            countFeedResponse.Setup(x => x.GetEnumerator()).Returns(new List<int> { totalCount }.GetEnumerator());

            var countIterator = new Mock<FeedIterator<int>>();
            countIterator.SetupSequence(x => x.HasMoreResults).Returns(true).Returns(false);
            countIterator.Setup(x => x.ReadNextAsync(default)).ReturnsAsync(countFeedResponse.Object);

            _containerMock.Setup(x => x.GetItemQueryIterator<int>(
                It.Is<QueryDefinition>(q => q.QueryText.Contains("COUNT(1)")),
                It.IsAny<string>(),
                It.IsAny<QueryRequestOptions>()))
                .Returns(countIterator.Object);

            // Setup main query
            var feedResponse = new Mock<FeedResponse<WeightDocument>>();
            feedResponse.Setup(x => x.GetEnumerator()).Returns(weightDocuments.GetEnumerator());

            var iterator = new Mock<FeedIterator<WeightDocument>>();
            iterator.SetupSequence(x => x.HasMoreResults).Returns(true).Returns(false);
            iterator.Setup(x => x.ReadNextAsync(default)).ReturnsAsync(feedResponse.Object);

            _containerMock.Setup(x => x.GetItemQueryIterator<WeightDocument>(
                It.Is<QueryDefinition>(q => q.QueryText.Contains("OFFSET @offset LIMIT @limit")),
                It.IsAny<string>(),
                It.IsAny<QueryRequestOptions>()))
                .Returns(iterator.Object);
        }
    }
}
