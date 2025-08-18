using AutoFixture;
using Biotrackr.Weight.Api.EndpointHandlers;
using Biotrackr.Weight.Api.Models;
using Biotrackr.Weight.Api.Repositories.Interfaces;
using FluentAssertions;
using Microsoft.AspNetCore.Http.HttpResults;
using Moq;

namespace Biotrackr.Weight.Api.UnitTests.EndpointHandlerTests
{
    public class WeightHandlersShould
    {
        private readonly Mock<ICosmosRepository> _cosmosRepositoryMock;

        public WeightHandlersShould()
        {
            _cosmosRepositoryMock = new Mock<ICosmosRepository>();
        }

        [Fact]
        public async Task GetWeightByDate_ShouldReturnOk_WhenWeightDocumentIsFound()
        {
            // Arrange
            var date = "2022-01-01";
            var fixture = new Fixture();
            var weightDocument = fixture.Create<WeightDocument>();
            weightDocument.Date = date;

            _cosmosRepositoryMock.Setup(x => x.GetWeightDocumentByDate(date)).ReturnsAsync(weightDocument);

            // Act
            var result = await WeightHandlers.GetWeightByDate(_cosmosRepositoryMock.Object, date);

            // Assert
            result.Result.Should().BeOfType<Ok<WeightDocument>>();
            var okResult = result.Result as Ok<WeightDocument>;
            okResult!.Value.Should().BeEquivalentTo(weightDocument);
            okResult.Value.Date.Should().Be(date);
        }

        [Fact]
        public async Task GetWeightByDate_ShouldReturnNotFound_WhenWeightDocumentIsNotFound()
        {
            // Arrange
            var date = "2022-01-01";
            _cosmosRepositoryMock.Setup(x => x.GetWeightDocumentByDate(date)).ReturnsAsync((WeightDocument?)null);

            // Act
            var result = await WeightHandlers.GetWeightByDate(_cosmosRepositoryMock.Object, date);

            // Assert
            result.Result.Should().BeOfType<NotFound>();
        }

        [Fact]
        public async Task GetWeightByDate_ShouldCallRepositoryWithCorrectDate()
        {
            // Arrange
            var date = "2023-05-15";
            var fixture = new Fixture();
            var weightDocument = fixture.Create<WeightDocument>();
            weightDocument.Date = date;

            _cosmosRepositoryMock.Setup(x => x.GetWeightDocumentByDate(date)).ReturnsAsync(weightDocument);

            // Act
            await WeightHandlers.GetWeightByDate(_cosmosRepositoryMock.Object, date);

            // Assert
            _cosmosRepositoryMock.Verify(x => x.GetWeightDocumentByDate(date), Times.Once);
        }

        [Fact]
        public async Task GetWeightByDate_ShouldPropagateException_WhenRepositoryThrowsException()
        {
            // Arrange
            var date = "2022-01-01";
            var expectedException = new InvalidOperationException("Database connection failed");

            _cosmosRepositoryMock.Setup(x => x.GetWeightDocumentByDate(date))
                .ThrowsAsync(expectedException);

            // Act & Assert
            var actualException = await Assert.ThrowsAsync<InvalidOperationException>(
                () => WeightHandlers.GetWeightByDate(_cosmosRepositoryMock.Object, date));

            actualException.Should().Be(expectedException);
            actualException.Message.Should().Be("Database connection failed");
        }

        [Fact]
        public async Task GetWeightByDate_ShouldPropagateArgumentException_WhenRepositoryThrowsArgumentException()
        {
            // Arrange
            var date = "invalid-date";
            var expectedException = new ArgumentException("Invalid date format", nameof(date));

            _cosmosRepositoryMock.Setup(x => x.GetWeightDocumentByDate(date))
                .ThrowsAsync(expectedException);

            // Act & Assert
            var actualException = await Assert.ThrowsAsync<ArgumentException>(
                () => WeightHandlers.GetWeightByDate(_cosmosRepositoryMock.Object, date));

            actualException.Should().Be(expectedException);
            actualException.ParamName.Should().Be("date");
        }

        [Fact]
        public async Task GetWeightByDate_ShouldPropagateTaskCanceledException_WhenOperationIsCanceled()
        {
            // Arrange
            var date = "2022-01-01";
            var expectedException = new TaskCanceledException("Operation was canceled");

            _cosmosRepositoryMock.Setup(x => x.GetWeightDocumentByDate(date))
                .ThrowsAsync(expectedException);

            // Act & Assert
            var actualException = await Assert.ThrowsAsync<TaskCanceledException>(
                () => WeightHandlers.GetWeightByDate(_cosmosRepositoryMock.Object, date));

            actualException.Should().Be(expectedException);
        }

        [Fact]
        public async Task GetAllWeights_ShouldReturnPaginationResponseWithWeightDocuments()
        {
            // Arrange
            var fixture = new Fixture();
            var weightDocuments = fixture.CreateMany<WeightDocument>(5).ToList();
            var paginationResponse = new PaginationResponse<WeightDocument>
            {
                Items = weightDocuments,
                TotalCount = 10,
                PageNumber = 1,
                PageSize = 20
            };

            _cosmosRepositoryMock.Setup(x => x.GetAllWeightDocuments(It.IsAny<PaginationRequest>()))
                .ReturnsAsync(paginationResponse);

            // Act
            var result = await WeightHandlers.GetAllWeights(_cosmosRepositoryMock.Object);

            // Assert
            result.Should().BeOfType<Ok<PaginationResponse<WeightDocument>>>();
            var okResult = result as Ok<PaginationResponse<WeightDocument>>;
            okResult!.Value.Should().BeEquivalentTo(paginationResponse);
        }

        [Fact]
        public async Task GetAllWeights_ShouldReturnPaginatedResult_WhenPaginationParametersProvided()
        {
            // Arrange
            var fixture = new Fixture();
            var weightDocuments = fixture.CreateMany<WeightDocument>(10).ToList();
            var paginatedResponse = new PaginationResponse<WeightDocument>
            {
                Items = weightDocuments,
                PageNumber = 2,
                PageSize = 10,
                TotalCount = 50
            };

            _cosmosRepositoryMock.Setup(x => x.GetAllWeightDocuments(It.IsAny<PaginationRequest>()))
                                .ReturnsAsync(paginatedResponse);

            // Act
            var result = await WeightHandlers.GetAllWeights(_cosmosRepositoryMock.Object, 2, 10);

            // Assert
            result.Should().BeOfType<Ok<PaginationResponse<WeightDocument>>>();
            var okResult = result as Ok<PaginationResponse<WeightDocument>>;
            okResult!.Value.Should().BeEquivalentTo(paginatedResponse);
        }

        [Fact]
        public async Task GetAllWeights_ShouldUseDefaultPaginationParameters_WhenNotProvided()
        {
            // Arrange
            var fixture = new Fixture();
            var paginationResponse = new PaginationResponse<WeightDocument>
            {
                Items = fixture.CreateMany<WeightDocument>().ToList(),
                TotalCount = 5,
                PageNumber = 1,
                PageSize = 20
            };

            _cosmosRepositoryMock.Setup(x => x.GetAllWeightDocuments(It.Is<PaginationRequest>(p =>
                p.PageNumber == 1 && p.PageSize == 20)))
                .ReturnsAsync(paginationResponse);

            // Act
            var result = await WeightHandlers.GetAllWeights(_cosmosRepositoryMock.Object);

            // Assert
            result.Should().BeOfType<Ok<PaginationResponse<WeightDocument>>>();
            _cosmosRepositoryMock.Verify(x => x.GetAllWeightDocuments(It.Is<PaginationRequest>(p =>
                p.PageNumber == 1 && p.PageSize == 20)), Times.Once);
        }

        [Fact]
        public async Task GetAllWeights_ShouldUseProvidedPaginationParameters()
        {
            // Arrange
            var pageNumber = 2;
            var pageSize = 10;
            var fixture = new Fixture();
            var paginationResponse = new PaginationResponse<WeightDocument>
            {
                Items = fixture.CreateMany<WeightDocument>().ToList(),
                TotalCount = 25,
                PageNumber = pageNumber,
                PageSize = pageSize
            };

            _cosmosRepositoryMock.Setup(x => x.GetAllWeightDocuments(It.Is<PaginationRequest>(p =>
                p.PageNumber == pageNumber && p.PageSize == pageSize)))
                .ReturnsAsync(paginationResponse);

            // Act
            var result = await WeightHandlers.GetAllWeights(_cosmosRepositoryMock.Object, pageNumber, pageSize);

            // Assert
            result.Should().BeOfType<Ok<PaginationResponse<WeightDocument>>>();
            var okResult = result as Ok<PaginationResponse<WeightDocument>>;
            okResult!.Value.PageNumber.Should().Be(pageNumber);
            okResult.Value.PageSize.Should().Be(pageSize);

            _cosmosRepositoryMock.Verify(x => x.GetAllWeightDocuments(It.Is<PaginationRequest>(p =>
                p.PageNumber == pageNumber && p.PageSize == pageSize)), Times.Once);
        }

        [Fact]
        public async Task GetAllWeights_ShouldUseDefaultPageSize_WhenOnlyPageNumberProvided()
        {
            // Arrange
            var fixture = new Fixture();
            var paginatedResponse = new PaginationResponse<WeightDocument>
            {
                Items = fixture.CreateMany<WeightDocument>(20).ToList(),
                PageNumber = 2,
                PageSize = 20,
                TotalCount = 100
            };

            _cosmosRepositoryMock.Setup(x => x.GetAllWeightDocuments(
                It.Is<PaginationRequest>(r => r.PageNumber == 2 && r.PageSize == 20)))
                                .ReturnsAsync(paginatedResponse);

            // Act
            var result = await WeightHandlers.GetAllWeights(_cosmosRepositoryMock.Object, 2, null);

            // Assert
            result.Should().BeOfType<Ok<PaginationResponse<WeightDocument>>>();
            _cosmosRepositoryMock.Verify(x => x.GetAllWeightDocuments(
                It.Is<PaginationRequest>(r => r.PageNumber == 2 && r.PageSize == 20)), Times.Once);
        }

        [Fact]
        public async Task GetAllWeights_ShouldUseDefaultPageNumber_WhenOnlyPageSizeProvided()
        {
            // Arrange
            var pageSize = 15;
            var fixture = new Fixture();
            var paginationResponse = new PaginationResponse<WeightDocument>
            {
                Items = fixture.CreateMany<WeightDocument>().ToList(),
                TotalCount = 30,
                PageNumber = 1,
                PageSize = pageSize
            };

            _cosmosRepositoryMock.Setup(x => x.GetAllWeightDocuments(It.Is<PaginationRequest>(p =>
                p.PageNumber == 1 && p.PageSize == pageSize)))
                .ReturnsAsync(paginationResponse);

            // Act
            var result = await WeightHandlers.GetAllWeights(_cosmosRepositoryMock.Object, null, pageSize);

            // Assert
            result.Should().BeOfType<Ok<PaginationResponse<WeightDocument>>>();
            _cosmosRepositoryMock.Verify(x => x.GetAllWeightDocuments(It.Is<PaginationRequest>(p =>
                p.PageNumber == 1 && p.PageSize == pageSize)), Times.Once);
        }

        [Fact]
        public async Task GetAllWeights_ShouldCallRepositoryWithCorrectPaginationRequest()
        {
            // Arrange
            var pageNumber = 3;
            var pageSize = 25;
            var fixture = new Fixture();
            var paginationResponse = new PaginationResponse<WeightDocument>
            {
                Items = fixture.CreateMany<WeightDocument>().ToList(),
                TotalCount = 100,
                PageNumber = pageNumber,
                PageSize = pageSize
            };

            _cosmosRepositoryMock.Setup(x => x.GetAllWeightDocuments(It.IsAny<PaginationRequest>()))
                .ReturnsAsync(paginationResponse);

            // Act
            await WeightHandlers.GetAllWeights(_cosmosRepositoryMock.Object, pageNumber, pageSize);

            // Assert
            _cosmosRepositoryMock.Verify(x => x.GetAllWeightDocuments(
                It.Is<PaginationRequest>(r =>
                    r.PageNumber == pageNumber &&
                    r.PageSize == pageSize)), Times.Once);
        }

        [Fact]
        public async Task GetAllWeights_ShouldPropagateException_WhenRepositoryThrowsException()
        {
            // Arrange
            var expectedException = new InvalidOperationException("Database connection failed");

            _cosmosRepositoryMock.Setup(x => x.GetAllWeightDocuments(It.IsAny<PaginationRequest>()))
                .ThrowsAsync(expectedException);

            // Act & Assert
            var actualException = await Assert.ThrowsAsync<InvalidOperationException>(
                () => WeightHandlers.GetAllWeights(_cosmosRepositoryMock.Object));

            actualException.Should().Be(expectedException);
            actualException.Message.Should().Be("Database connection failed");
        }

        [Fact]
        public async Task GetAllWeights_ShouldPropagateArgumentException_WhenRepositoryThrowsArgumentException()
        {
            // Arrange
            var expectedException = new ArgumentException("Invalid pagination parameters");

            _cosmosRepositoryMock.Setup(x => x.GetAllWeightDocuments(It.IsAny<PaginationRequest>()))
                .ThrowsAsync(expectedException);

            // Act & Assert
            var actualException = await Assert.ThrowsAsync<ArgumentException>(
                () => WeightHandlers.GetAllWeights(_cosmosRepositoryMock.Object, 2, 10));

            actualException.Should().Be(expectedException);
            actualException.Message.Should().Be("Invalid pagination parameters");
        }

        [Fact]
        public async Task GetAllWeights_ShouldPropagateTimeoutException_WhenRepositoryTimesOut()
        {
            // Arrange
            var expectedException = new TimeoutException("Request timed out");

            _cosmosRepositoryMock.Setup(x => x.GetAllWeightDocuments(It.IsAny<PaginationRequest>()))
                .ThrowsAsync(expectedException);

            // Act & Assert
            var actualException = await Assert.ThrowsAsync<TimeoutException>(
                () => WeightHandlers.GetAllWeights(_cosmosRepositoryMock.Object, 1, 20));

            actualException.Should().Be(expectedException);
            actualException.Message.Should().Be("Request timed out");
        }

        [Fact]
        public async Task GetAllWeights_ShouldPropagateTaskCanceledException_WhenOperationIsCanceled()
        {
            // Arrange
            var expectedException = new TaskCanceledException("Operation was canceled");

            _cosmosRepositoryMock.Setup(x => x.GetAllWeightDocuments(It.IsAny<PaginationRequest>()))
                .ThrowsAsync(expectedException);

            // Act & Assert
            var actualException = await Assert.ThrowsAsync<TaskCanceledException>(
                () => WeightHandlers.GetAllWeights(_cosmosRepositoryMock.Object));

            actualException.Should().Be(expectedException);
        }

        [Fact]
        public async Task GetAllWeights_ShouldStillCallRepository_EvenWhenExceptionExpected()
        {
            // Arrange
            var expectedException = new InvalidOperationException("Test exception");

            _cosmosRepositoryMock.Setup(x => x.GetAllWeightDocuments(It.IsAny<PaginationRequest>()))
                .ThrowsAsync(expectedException);

            // Act & Assert
            await Assert.ThrowsAsync<InvalidOperationException>(
                () => WeightHandlers.GetAllWeights(_cosmosRepositoryMock.Object, 1, 10));

            // Verify the repository was still called with correct parameters
            _cosmosRepositoryMock.Verify(x => x.GetAllWeightDocuments(
                It.Is<PaginationRequest>(r => r.PageNumber == 1 && r.PageSize == 10)), Times.Once);
        }

        [Fact]
        public async Task GetWeightsByDateRange_ShouldReturnPaginatedWeightDocuments_WhenValidDateRange()
        {
            // Arrange
            var startDate = "2022-01-01";
            var endDate = "2022-01-31";
            var pageNumber = 1;
            var pageSize = 10;
            var fixture = new Fixture();
            var weightDocuments = fixture.CreateMany<WeightDocument>(10).ToList();
            var paginationResponse = new PaginationResponse<WeightDocument>
            {
                Items = weightDocuments,
                TotalCount = 25,
                PageNumber = pageNumber,
                PageSize = pageSize
            };

            _cosmosRepositoryMock.Setup(x => x.GetWeightsByDateRange(startDate, endDate, It.IsAny<PaginationRequest>()))
                .ReturnsAsync(paginationResponse);

            // Act
            var result = await WeightHandlers.GetWeightsByDateRange(_cosmosRepositoryMock.Object, startDate, endDate, pageNumber, pageSize);

            // Assert
            result.Result.Should().BeOfType<Ok<PaginationResponse<WeightDocument>>>();
            var okResult = result.Result as Ok<PaginationResponse<WeightDocument>>;
            okResult!.Value.Should().BeEquivalentTo(paginationResponse);
        }

        [Fact]
        public async Task GetWeightsByDateRange_ShouldUseDefaultPagination_WhenNotProvided()
        {
            // Arrange
            var startDate = "2022-01-01";
            var endDate = "2022-01-31";
            var fixture = new Fixture();
            var paginationResponse = new PaginationResponse<WeightDocument>
            {
                Items = fixture.CreateMany<WeightDocument>(20).ToList(),
                TotalCount = 20,
                PageNumber = 1,
                PageSize = 20
            };

            _cosmosRepositoryMock.Setup(x => x.GetWeightsByDateRange(startDate, endDate,
                It.Is<PaginationRequest>(p => p.PageNumber == 1 && p.PageSize == 20)))
                .ReturnsAsync(paginationResponse);

            // Act
            var result = await WeightHandlers.GetWeightsByDateRange(_cosmosRepositoryMock.Object, startDate, endDate);

            // Assert
            result.Result.Should().BeOfType<Ok<PaginationResponse<WeightDocument>>>();
        }
    }
}
