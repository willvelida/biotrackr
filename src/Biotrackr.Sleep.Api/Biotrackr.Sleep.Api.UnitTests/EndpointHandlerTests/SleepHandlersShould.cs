using AutoFixture;
using Biotrackr.Sleep.Api.EndpointHandlers;
using Biotrackr.Sleep.Api.Models;
using Biotrackr.Sleep.Api.Repositories.Interfaces;
using FluentAssertions;
using Microsoft.AspNetCore.Http.HttpResults;
using Moq;

namespace Biotrackr.Sleep.Api.UnitTests.EndpointHandlerTests
{
    public class SleepHandlersShould
    {
        private readonly Mock<ICosmosRepository> _cosmosRepositoryMock;

        public SleepHandlersShould()
        {
            _cosmosRepositoryMock = new Mock<ICosmosRepository>();
        }

        [Fact]
        public async Task GetSleepByDate_ShouldReturnOk_WhenSleepDocumentIsFound()
        {
            // Arrange
            var date = "2022-01-01";
            var fixture = new Fixture();
            var sleepDocument = fixture.Create<SleepDocument>();
            sleepDocument.Date = date;

            _cosmosRepositoryMock.Setup(x => x.GetSleepSummaryByDate(date)).ReturnsAsync(sleepDocument);

            // Act
            var result = await SleepHandlers.GetSleepByDate(_cosmosRepositoryMock.Object, date);

            // Assert
            result.Result.Should().BeOfType<Ok<SleepDocument>>();
            var okResult = result.Result as Ok<SleepDocument>;
            okResult.Value.Should().BeEquivalentTo(sleepDocument);
            okResult.Value.Date.Should().Be(date);
        }

        [Fact]
        public async Task GetSleepByDate_ShouldReturnNotFound_WhenSleepDocumentIsNotFound()
        {
            // Arrange
            var date = "2022-01-01";
            _cosmosRepositoryMock.Setup(x => x.GetSleepSummaryByDate(date)).ReturnsAsync((SleepDocument)null);

            // Act
            var result = await SleepHandlers.GetSleepByDate(_cosmosRepositoryMock.Object, date);

            // Assert
            result.Result.Should().BeOfType<NotFound>();
        }

        [Fact]
        public async Task GetSleepByDate_ShouldCallRepositoryWithCorrectDate()
        {
            // Arrange
            var date = "2023-05-15";
            var fixture = new Fixture();
            var sleepDocument = fixture.Create<SleepDocument>();
            sleepDocument.Date = date;

            _cosmosRepositoryMock.Setup(x => x.GetSleepSummaryByDate(date)).ReturnsAsync(sleepDocument);

            // Act
            await SleepHandlers.GetSleepByDate(_cosmosRepositoryMock.Object, date);

            // Assert
            _cosmosRepositoryMock.Verify(x => x.GetSleepSummaryByDate(date), Times.Once);
        }

        [Fact]
        public async Task GetSleepByDate_ShouldPropagateException_WhenRepositoryThrowsException()
        {
            // Arrange
            var date = "2022-01-01";
            var expectedException = new InvalidOperationException("Database connection failed");

            _cosmosRepositoryMock.Setup(x => x.GetSleepSummaryByDate(date))
                .ThrowsAsync(expectedException);

            // Act & Assert
            var actualException = await Assert.ThrowsAsync<InvalidOperationException>(
                () => SleepHandlers.GetSleepByDate(_cosmosRepositoryMock.Object, date));

            actualException.Should().Be(expectedException);
            actualException.Message.Should().Be("Database connection failed");
        }

        [Fact]
        public async Task GetSleepByDate_ShouldPropagateArgumentException_WhenRepositoryThrowsArgumentException()
        {
            // Arrange
            var date = "invalid-date";
            var expectedException = new ArgumentException("Invalid date format", nameof(date));

            _cosmosRepositoryMock.Setup(x => x.GetSleepSummaryByDate(date))
                .ThrowsAsync(expectedException);

            // Act & Assert
            var actualException = await Assert.ThrowsAsync<ArgumentException>(
                () => SleepHandlers.GetSleepByDate(_cosmosRepositoryMock.Object, date));

            actualException.Should().Be(expectedException);
            actualException.ParamName.Should().Be("date");
        }

        [Fact]
        public async Task GetSleepByDate_ShouldPropagateTaskCanceledException_WhenOperationIsCanceled()
        {
            // Arrange
            var date = "2022-01-01";
            var expectedException = new TaskCanceledException("Operation was canceled");

            _cosmosRepositoryMock.Setup(x => x.GetSleepSummaryByDate(date))
                .ThrowsAsync(expectedException);

            // Act & Assert
            var actualException = await Assert.ThrowsAsync<TaskCanceledException>(
                () => SleepHandlers.GetSleepByDate(_cosmosRepositoryMock.Object, date));

            actualException.Should().Be(expectedException);
        }

        [Fact]
        public async Task GetAllSleeps_ShouldReturnPaginationResponseWithSleepDocuments()
        {
            // Arrange
            var fixture = new Fixture();
            var sleepDocuments = fixture.CreateMany<SleepDocument>(5).ToList();
            var paginationResponse = new PaginationResponse<SleepDocument>
            {
                Items = sleepDocuments,
                TotalCount = 10,
                PageNumber = 1,
                PageSize = 20
            };

            _cosmosRepositoryMock.Setup(x => x.GetAllSleepDocuments(It.IsAny<PaginationRequest>()))
                .ReturnsAsync(paginationResponse);

            // Act
            var result = await SleepHandlers.GetAllSleeps(_cosmosRepositoryMock.Object);

            // Assert
            result.Should().BeOfType<Ok<PaginationResponse<SleepDocument>>>();
            var okResult = result as Ok<PaginationResponse<SleepDocument>>;
            okResult.Value.Should().BeEquivalentTo(paginationResponse);
        }

        [Fact]
        public async Task GetAllSleeps_ShouldReturnPaginatedResult_WhenPaginationParametersProvided()
        {
            // Arrange
            var fixture = new Fixture();
            var sleepDocuments = fixture.CreateMany<SleepDocument>(10).ToList();
            var paginatedResponse = new PaginationResponse<SleepDocument>
            {
                Items = sleepDocuments,
                PageNumber = 2,
                PageSize = 10,
                TotalCount = 50
            };

            _cosmosRepositoryMock.Setup(x => x.GetAllSleepDocuments(It.IsAny<PaginationRequest>()))
                                .ReturnsAsync(paginatedResponse);

            // Act
            var result = await SleepHandlers.GetAllSleeps(_cosmosRepositoryMock.Object, 2, 10);

            // Assert
            result.Should().BeOfType<Ok<PaginationResponse<SleepDocument>>>();
            var okResult = result as Ok<PaginationResponse<SleepDocument>>;
            okResult.Value.Should().BeEquivalentTo(paginatedResponse);
        }

        [Fact]
        public async Task GetAllSleeps_ShouldUseDefaultPaginationParameters_WhenNotProvided()
        {
            // Arrange
            var fixture = new Fixture();
            var paginationResponse = new PaginationResponse<SleepDocument>
            {
                Items = fixture.CreateMany<SleepDocument>().ToList(),
                TotalCount = 5,
                PageNumber = 1,
                PageSize = 20
            };

            _cosmosRepositoryMock.Setup(x => x.GetAllSleepDocuments(It.Is<PaginationRequest>(p =>
                p.PageNumber == 1 && p.PageSize == 20)))
                .ReturnsAsync(paginationResponse);

            // Act
            var result = await SleepHandlers.GetAllSleeps(_cosmosRepositoryMock.Object);

            // Assert
            result.Should().BeOfType<Ok<PaginationResponse<SleepDocument>>>();
            _cosmosRepositoryMock.Verify(x => x.GetAllSleepDocuments(It.Is<PaginationRequest>(p =>
                p.PageNumber == 1 && p.PageSize == 20)), Times.Once);
        }

        [Fact]
        public async Task GetAllSleeps_ShouldUseProvidedPaginationParameters()
        {
            // Arrange
            var pageNumber = 2;
            var pageSize = 10;
            var fixture = new Fixture();
            var paginationResponse = new PaginationResponse<SleepDocument>
            {
                Items = fixture.CreateMany<SleepDocument>().ToList(),
                TotalCount = 25,
                PageNumber = pageNumber,
                PageSize = pageSize
            };

            _cosmosRepositoryMock.Setup(x => x.GetAllSleepDocuments(It.Is<PaginationRequest>(p =>
                p.PageNumber == pageNumber && p.PageSize == pageSize)))
                .ReturnsAsync(paginationResponse);

            // Act
            var result = await SleepHandlers.GetAllSleeps(_cosmosRepositoryMock.Object, pageNumber, pageSize);

            // Assert
            result.Should().BeOfType<Ok<PaginationResponse<SleepDocument>>>();
            var okResult = result as Ok<PaginationResponse<SleepDocument>>;
            okResult.Value.PageNumber.Should().Be(pageNumber);
            okResult.Value.PageSize.Should().Be(pageSize);

            _cosmosRepositoryMock.Verify(x => x.GetAllSleepDocuments(It.Is<PaginationRequest>(p =>
                p.PageNumber == pageNumber && p.PageSize == pageSize)), Times.Once);
        }

        [Fact]
        public async Task GetAllSleeps_ShouldUseDefaultPageSize_WhenOnlyPageNumberProvided()
        {
            // Arrange
            var fixture = new Fixture();
            var paginatedResponse = new PaginationResponse<SleepDocument>
            {
                Items = fixture.CreateMany<SleepDocument>(20).ToList(),
                PageNumber = 2,
                PageSize = 20,
                TotalCount = 100
            };

            _cosmosRepositoryMock.Setup(x => x.GetAllSleepDocuments(
                It.Is<PaginationRequest>(r => r.PageNumber == 2 && r.PageSize == 20)))
                                .ReturnsAsync(paginatedResponse);

            // Act
            var result = await SleepHandlers.GetAllSleeps(_cosmosRepositoryMock.Object, 2, null);

            // Assert
            result.Should().BeOfType<Ok<PaginationResponse<SleepDocument>>>();
            _cosmosRepositoryMock.Verify(x => x.GetAllSleepDocuments(
                It.Is<PaginationRequest>(r => r.PageNumber == 2 && r.PageSize == 20)), Times.Once);
        }

        [Fact]
        public async Task GetAllSleeps_ShouldUseDefaultPageNumber_WhenOnlyPageSizeProvided()
        {
            // Arrange
            var pageSize = 15;
            var fixture = new Fixture();
            var paginationResponse = new PaginationResponse<SleepDocument>
            {
                Items = fixture.CreateMany<SleepDocument>().ToList(),
                TotalCount = 30,
                PageNumber = 1,
                PageSize = pageSize
            };

            _cosmosRepositoryMock.Setup(x => x.GetAllSleepDocuments(It.Is<PaginationRequest>(p =>
                p.PageNumber == 1 && p.PageSize == pageSize)))
                .ReturnsAsync(paginationResponse);

            // Act
            var result = await SleepHandlers.GetAllSleeps(_cosmosRepositoryMock.Object, null, pageSize);

            // Assert
            result.Should().BeOfType<Ok<PaginationResponse<SleepDocument>>>();
            _cosmosRepositoryMock.Verify(x => x.GetAllSleepDocuments(It.Is<PaginationRequest>(p =>
                p.PageNumber == 1 && p.PageSize == pageSize)), Times.Once);
        }

        [Fact]
        public async Task GetAllSleeps_ShouldCallRepositoryWithCorrectPaginationRequest()
        {
            // Arrange
            var pageNumber = 3;
            var pageSize = 25;
            var fixture = new Fixture();
            var paginationResponse = new PaginationResponse<SleepDocument>
            {
                Items = fixture.CreateMany<SleepDocument>().ToList(),
                TotalCount = 100,
                PageNumber = pageNumber,
                PageSize = pageSize
            };

            _cosmosRepositoryMock.Setup(x => x.GetAllSleepDocuments(It.IsAny<PaginationRequest>()))
                .ReturnsAsync(paginationResponse);

            // Act
            await SleepHandlers.GetAllSleeps(_cosmosRepositoryMock.Object, pageNumber, pageSize);

            // Assert
            _cosmosRepositoryMock.Verify(x => x.GetAllSleepDocuments(
                It.Is<PaginationRequest>(r =>
                    r.PageNumber == pageNumber &&
                    r.PageSize == pageSize)), Times.Once);
        }

        [Fact]
        public async Task GetAllSleeps_ShouldPropagateException_WhenRepositoryThrowsException()
        {
            // Arrange
            var expectedException = new InvalidOperationException("Database connection failed");

            _cosmosRepositoryMock.Setup(x => x.GetAllSleepDocuments(It.IsAny<PaginationRequest>()))
                .ThrowsAsync(expectedException);

            // Act & Assert
            var actualException = await Assert.ThrowsAsync<InvalidOperationException>(
                () => SleepHandlers.GetAllSleeps(_cosmosRepositoryMock.Object));

            actualException.Should().Be(expectedException);
            actualException.Message.Should().Be("Database connection failed");
        }

        [Fact]
        public async Task GetAllSleeps_ShouldPropagateArgumentException_WhenRepositoryThrowsArgumentException()
        {
            // Arrange
            var expectedException = new ArgumentException("Invalid pagination parameters");

            _cosmosRepositoryMock.Setup(x => x.GetAllSleepDocuments(It.IsAny<PaginationRequest>()))
                .ThrowsAsync(expectedException);

            // Act & Assert
            var actualException = await Assert.ThrowsAsync<ArgumentException>(
                () => SleepHandlers.GetAllSleeps(_cosmosRepositoryMock.Object, 2, 10));

            actualException.Should().Be(expectedException);
            actualException.Message.Should().Be("Invalid pagination parameters");
        }

        [Fact]
        public async Task GetAllSleeps_ShouldPropagateTimeoutException_WhenRepositoryTimesOut()
        {
            // Arrange
            var expectedException = new TimeoutException("Request timed out");

            _cosmosRepositoryMock.Setup(x => x.GetAllSleepDocuments(It.IsAny<PaginationRequest>()))
                .ThrowsAsync(expectedException);

            // Act & Assert
            var actualException = await Assert.ThrowsAsync<TimeoutException>(
                () => SleepHandlers.GetAllSleeps(_cosmosRepositoryMock.Object, 1, 20));

            actualException.Should().Be(expectedException);
            actualException.Message.Should().Be("Request timed out");
        }

        [Fact]
        public async Task GetAllSleeps_ShouldPropagateTaskCanceledException_WhenOperationIsCanceled()
        {
            // Arrange
            var expectedException = new TaskCanceledException("Operation was canceled");

            _cosmosRepositoryMock.Setup(x => x.GetAllSleepDocuments(It.IsAny<PaginationRequest>()))
                .ThrowsAsync(expectedException);

            // Act & Assert
            var actualException = await Assert.ThrowsAsync<TaskCanceledException>(
                () => SleepHandlers.GetAllSleeps(_cosmosRepositoryMock.Object));

            actualException.Should().Be(expectedException);
        }

        [Fact]
        public async Task GetAllSleeps_ShouldStillCallRepository_EvenWhenExceptionExpected()
        {
            // Arrange
            var expectedException = new InvalidOperationException("Test exception");

            _cosmosRepositoryMock.Setup(x => x.GetAllSleepDocuments(It.IsAny<PaginationRequest>()))
                .ThrowsAsync(expectedException);

            // Act & Assert
            await Assert.ThrowsAsync<InvalidOperationException>(
                () => SleepHandlers.GetAllSleeps(_cosmosRepositoryMock.Object, 1, 10));

            // Verify the repository was still called with correct parameters
            _cosmosRepositoryMock.Verify(x => x.GetAllSleepDocuments(
                It.Is<PaginationRequest>(r => r.PageNumber == 1 && r.PageSize == 10)), Times.Once);
        }
    }
}
