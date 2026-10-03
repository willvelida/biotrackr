using AutoFixture;
using Biotrackr.Food.Api.Configuration;
using Biotrackr.Food.Api.Models;
using Biotrackr.Food.Api.Repositories;
using FluentAssertions;
using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;

namespace Biotrackr.Food.Api.UnitTests.RepositoryTests
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
        public async Task GetAllFoodLogsAsync_ShouldReturnFoodDocuments()
        {
            // Arrange
            var pageNumber = 1;
            var pageSize = 20;
            var fixture = new Fixture();
            var foodDocuments = fixture.Build<FoodStoredDocument>().Without(x => x.Google).CreateMany(10).ToList();

            var feedResponse = new Mock<FeedResponse<FoodStoredDocument>>();
            feedResponse.Setup(x => x.GetEnumerator()).Returns(foodDocuments.GetEnumerator());

            var iterator = new Mock<FeedIterator<FoodStoredDocument>>();
            iterator.SetupSequence(x => x.HasMoreResults).Returns(true).Returns(false);
            iterator.Setup(x => x.ReadNextAsync(default)).ReturnsAsync(feedResponse.Object);

            _containerMock.Setup(x => x.GetItemQueryIterator<FoodStoredDocument>(
                It.IsAny<QueryDefinition>(), 
                It.IsAny<string>(), 
                It.IsAny<QueryRequestOptions>())).Returns(iterator.Object);

            // Act
            var result = await _repository.GetAllFoodLogsAsync(pageNumber, pageSize);

            // Assert
            result.Should().HaveCount(10);
            result.Should().BeEquivalentTo(foodDocuments);
        }

        [Fact]
        public async Task GetAllFoodLogsAsync_ShouldReturnEmptyList_WhenNoDocumentsExist()
        {
            // Arrange
            var pageNumber = 1;
            var pageSize = 20;

            var feedResponse = new Mock<FeedResponse<FoodStoredDocument>>();
            feedResponse.Setup(x => x.GetEnumerator()).Returns(new List<FoodStoredDocument>().GetEnumerator());

            var iterator = new Mock<FeedIterator<FoodStoredDocument>>();
            iterator.SetupSequence(x => x.HasMoreResults).Returns(true).Returns(false);
            iterator.Setup(x => x.ReadNextAsync(default)).ReturnsAsync(feedResponse.Object);

            _containerMock.Setup(x => x.GetItemQueryIterator<FoodStoredDocument>(
                It.IsAny<QueryDefinition>(), 
                It.IsAny<string>(), 
                It.IsAny<QueryRequestOptions>())).Returns(iterator.Object);

            // Act
            var result = await _repository.GetAllFoodLogsAsync(pageNumber, pageSize);

            // Assert
            result.Should().BeEmpty();
        }

        [Fact]
        public async Task GetAllFoodLogsAsync_ShouldThrowException_WhenErrorOccurs()
        {
            // Arrange
            var pageNumber = 1;
            var pageSize = 20;
            var exceptionMessage = "Test Exception";

            _containerMock.Setup(x => x.GetItemQueryIterator<FoodStoredDocument>(
                It.IsAny<QueryDefinition>(), 
                It.IsAny<string>(), 
                It.IsAny<QueryRequestOptions>()))
                .Throws(new Exception(exceptionMessage));

            // Act
            Func<Task> act = async () => await _repository.GetAllFoodLogsAsync(pageNumber, pageSize);

            // Assert
            await act.Should().ThrowAsync<Exception>().WithMessage(exceptionMessage);
        }

        [Fact]
        public async Task GetFoodLogByDateAsync_ShouldReturnFoodDocument_WhenExists()
        {
            // Arrange
            var date = "2022-01-01";
            var fixture = new Fixture();
            var FoodStoredDocument = fixture.Build<FoodStoredDocument>().Without(x => x.Google).Create();
            FoodStoredDocument.Date = date;

            var feedResponse = new Mock<FeedResponse<FoodStoredDocument>>();
            feedResponse.Setup(x => x.GetEnumerator()).Returns(new List<FoodStoredDocument> { FoodStoredDocument }.GetEnumerator());

            var iterator = new Mock<FeedIterator<FoodStoredDocument>>();
            iterator.SetupSequence(x => x.HasMoreResults).Returns(true).Returns(false);
            iterator.Setup(x => x.ReadNextAsync(default)).ReturnsAsync(feedResponse.Object);

            _containerMock.Setup(x => x.GetItemQueryIterator<FoodStoredDocument>(
                It.IsAny<QueryDefinition>(), 
                It.IsAny<string>(), 
                It.IsAny<QueryRequestOptions>())).Returns(iterator.Object);

            // Act
            var result = await _repository.GetFoodLogByDateAsync(date);

            // Assert
            result.Should().NotBeNull();
            result.Should().BeEquivalentTo(FoodStoredDocument);
            result!.Date.Should().Be(date);
        }

        [Fact]
        public async Task GetFoodLogByDateAsync_ShouldReturnNull_WhenNotExists()
        {
            // Arrange
            var date = "2022-01-01";

            var feedResponse = new Mock<FeedResponse<FoodStoredDocument>>();
            feedResponse.Setup(x => x.GetEnumerator()).Returns(new List<FoodStoredDocument>().GetEnumerator());

            var iterator = new Mock<FeedIterator<FoodStoredDocument>>();
            iterator.SetupSequence(x => x.HasMoreResults).Returns(true).Returns(false);
            iterator.Setup(x => x.ReadNextAsync(default)).ReturnsAsync(feedResponse.Object);

            _containerMock.Setup(x => x.GetItemQueryIterator<FoodStoredDocument>(
                It.IsAny<QueryDefinition>(), 
                It.IsAny<string>(), 
                It.IsAny<QueryRequestOptions>())).Returns(iterator.Object);

            // Act
            var result = await _repository.GetFoodLogByDateAsync(date);

            // Assert
            result.Should().BeNull();
        }

        [Fact]
        public async Task GetFoodLogByDateAsync_ShouldThrowException_WhenErrorOccurs()
        {
            // Arrange
            var date = "2022-01-01";
            var exceptionMessage = "Test Exception";

            _containerMock.Setup(x => x.GetItemQueryIterator<FoodStoredDocument>(
                It.IsAny<QueryDefinition>(), 
                It.IsAny<string>(), 
                It.IsAny<QueryRequestOptions>()))
                .Throws(new Exception(exceptionMessage));

            // Act
            Func<Task> act = async () => await _repository.GetFoodLogByDateAsync(date);

            // Assert
            await act.Should().ThrowAsync<Exception>().WithMessage(exceptionMessage);
        }

        [Fact]
        public async Task GetFoodLogsByDateRangeAsync_ShouldReturnFoodDocuments()
        {
            // Arrange
            var startDate = "2023-01-01";
            var endDate = "2023-01-31";
            var pageNumber = 1;
            var pageSize = 20;
            var fixture = new Fixture();
            var foodDocuments = fixture.Build<FoodStoredDocument>().Without(x => x.Google).CreateMany(5).ToList();

            var feedResponse = new Mock<FeedResponse<FoodStoredDocument>>();
            feedResponse.Setup(x => x.GetEnumerator()).Returns(foodDocuments.GetEnumerator());

            var iterator = new Mock<FeedIterator<FoodStoredDocument>>();
            iterator.SetupSequence(x => x.HasMoreResults).Returns(true).Returns(false);
            iterator.Setup(x => x.ReadNextAsync(default)).ReturnsAsync(feedResponse.Object);

            _containerMock.Setup(x => x.GetItemQueryIterator<FoodStoredDocument>(
                It.IsAny<QueryDefinition>(), 
                It.IsAny<string>(), 
                It.IsAny<QueryRequestOptions>())).Returns(iterator.Object);

            // Act
            var result = await _repository.GetFoodLogsByDateRangeAsync(startDate, endDate, pageNumber, pageSize);

            // Assert
            result.Should().HaveCount(5);
            result.Should().BeEquivalentTo(foodDocuments);
        }

        [Fact]
        public async Task GetFoodLogsByDateRangeAsync_ShouldReturnEmptyList_WhenNoDocumentsInRange()
        {
            // Arrange
            var startDate = "2023-01-01";
            var endDate = "2023-01-31";
            var pageNumber = 1;
            var pageSize = 20;

            var feedResponse = new Mock<FeedResponse<FoodStoredDocument>>();
            feedResponse.Setup(x => x.GetEnumerator()).Returns(new List<FoodStoredDocument>().GetEnumerator());

            var iterator = new Mock<FeedIterator<FoodStoredDocument>>();
            iterator.SetupSequence(x => x.HasMoreResults).Returns(true).Returns(false);
            iterator.Setup(x => x.ReadNextAsync(default)).ReturnsAsync(feedResponse.Object);

            _containerMock.Setup(x => x.GetItemQueryIterator<FoodStoredDocument>(
                It.IsAny<QueryDefinition>(), 
                It.IsAny<string>(), 
                It.IsAny<QueryRequestOptions>())).Returns(iterator.Object);

            // Act
            var result = await _repository.GetFoodLogsByDateRangeAsync(startDate, endDate, pageNumber, pageSize);

            // Assert
            result.Should().BeEmpty();
        }

        [Fact]
        public async Task GetFoodLogsByDateRangeAsync_ShouldThrowException_WhenErrorOccurs()
        {
            // Arrange
            var startDate = "2023-01-01";
            var endDate = "2023-01-31";
            var pageNumber = 1;
            var pageSize = 20;
            var exceptionMessage = "Test Exception";

            _containerMock.Setup(x => x.GetItemQueryIterator<FoodStoredDocument>(
                It.IsAny<QueryDefinition>(), 
                It.IsAny<string>(), 
                It.IsAny<QueryRequestOptions>()))
                .Throws(new Exception(exceptionMessage));

            // Act
            Func<Task> act = async () => await _repository.GetFoodLogsByDateRangeAsync(startDate, endDate, pageNumber, pageSize);

            // Assert
            await act.Should().ThrowAsync<Exception>().WithMessage(exceptionMessage);
        }

        [Fact]
        public async Task GetTotalFoodLogsCountAsync_ShouldReturnCount()
        {
            // Arrange
            var expectedCount = 50;

            var feedResponse = new Mock<FeedResponse<int>>();
            feedResponse.Setup(x => x.GetEnumerator()).Returns(new List<int> { expectedCount }.GetEnumerator());

            var iterator = new Mock<FeedIterator<int>>();
            iterator.SetupSequence(x => x.HasMoreResults).Returns(true).Returns(false);
            iterator.Setup(x => x.ReadNextAsync(default)).ReturnsAsync(feedResponse.Object);

            _containerMock.Setup(x => x.GetItemQueryIterator<int>(
                It.IsAny<QueryDefinition>(), 
                It.IsAny<string>(), 
                It.IsAny<QueryRequestOptions>())).Returns(iterator.Object);

            // Act
            var result = await _repository.GetTotalFoodLogsCountAsync();

            // Assert
            result.Should().Be(expectedCount);
        }

        [Fact]
        public async Task GetTotalFoodLogsCountAsync_ShouldReturnZero_WhenNoDocuments()
        {
            // Arrange
            var feedResponse = new Mock<FeedResponse<int>>();
            feedResponse.Setup(x => x.GetEnumerator()).Returns(new List<int> { 0 }.GetEnumerator());

            var iterator = new Mock<FeedIterator<int>>();
            iterator.SetupSequence(x => x.HasMoreResults).Returns(true).Returns(false);
            iterator.Setup(x => x.ReadNextAsync(default)).ReturnsAsync(feedResponse.Object);

            _containerMock.Setup(x => x.GetItemQueryIterator<int>(
                It.IsAny<QueryDefinition>(), 
                It.IsAny<string>(), 
                It.IsAny<QueryRequestOptions>())).Returns(iterator.Object);

            // Act
            var result = await _repository.GetTotalFoodLogsCountAsync();

            // Assert
            result.Should().Be(0);
        }

        [Fact]
        public async Task GetTotalFoodLogsCountAsync_ShouldPropagateException_WhenExceptionOccurs()
        {
            // Arrange
            _containerMock.Setup(x => x.GetItemQueryIterator<int>(
                It.IsAny<QueryDefinition>(), 
                It.IsAny<string>(), 
                It.IsAny<QueryRequestOptions>()))
                .Throws(new Exception("Test Exception"));

            // Act & Assert
            await _repository.Invoking(r => r.GetTotalFoodLogsCountAsync())
                .Should().ThrowAsync<Exception>()
                .WithMessage("Test Exception");
        }

        [Fact]
        public async Task GetFoodLogsCountByDateRangeAsync_ShouldReturnCount()
        {
            // Arrange
            var startDate = "2023-01-01";
            var endDate = "2023-01-31";
            var expectedCount = 15;

            var feedResponse = new Mock<FeedResponse<int>>();
            feedResponse.Setup(x => x.GetEnumerator()).Returns(new List<int> { expectedCount }.GetEnumerator());

            var iterator = new Mock<FeedIterator<int>>();
            iterator.SetupSequence(x => x.HasMoreResults).Returns(true).Returns(false);
            iterator.Setup(x => x.ReadNextAsync(default)).ReturnsAsync(feedResponse.Object);

            _containerMock.Setup(x => x.GetItemQueryIterator<int>(
                It.IsAny<QueryDefinition>(), 
                It.IsAny<string>(), 
                It.IsAny<QueryRequestOptions>())).Returns(iterator.Object);

            // Act
            var result = await _repository.GetFoodLogsCountByDateRangeAsync(startDate, endDate);

            // Assert
            result.Should().Be(expectedCount);
        }

        [Fact]
        public async Task GetFoodLogsCountByDateRangeAsync_ShouldReturnZero_WhenNoDocumentsInRange()
        {
            // Arrange
            var startDate = "2023-01-01";
            var endDate = "2023-01-31";

            var feedResponse = new Mock<FeedResponse<int>>();
            feedResponse.Setup(x => x.GetEnumerator()).Returns(new List<int> { 0 }.GetEnumerator());

            var iterator = new Mock<FeedIterator<int>>();
            iterator.SetupSequence(x => x.HasMoreResults).Returns(true).Returns(false);
            iterator.Setup(x => x.ReadNextAsync(default)).ReturnsAsync(feedResponse.Object);

            _containerMock.Setup(x => x.GetItemQueryIterator<int>(
                It.IsAny<QueryDefinition>(), 
                It.IsAny<string>(), 
                It.IsAny<QueryRequestOptions>())).Returns(iterator.Object);

            // Act
            var result = await _repository.GetFoodLogsCountByDateRangeAsync(startDate, endDate);

            // Assert
            result.Should().Be(0);
        }

        [Fact]
        public async Task GetFoodLogsCountByDateRangeAsync_ShouldPropagateException_WhenExceptionOccurs()
        {
            // Arrange
            var startDate = "2023-01-01";
            var endDate = "2023-01-31";

            _containerMock.Setup(x => x.GetItemQueryIterator<int>(
                It.IsAny<QueryDefinition>(), 
                It.IsAny<string>(), 
                It.IsAny<QueryRequestOptions>()))
                .Throws(new Exception("Test Exception"));

            // Act & Assert
            await _repository.Invoking(r => r.GetFoodLogsCountByDateRangeAsync(startDate, endDate))
                .Should().ThrowAsync<Exception>()
                .WithMessage("Test Exception");
        }
    }
}
