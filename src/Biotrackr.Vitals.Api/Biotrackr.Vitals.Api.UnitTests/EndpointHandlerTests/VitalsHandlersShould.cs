using AutoFixture;
using Biotrackr.Vitals.Api.EndpointHandlers;
using Biotrackr.Vitals.Api.Models;
using Biotrackr.Vitals.Api.Repositories.Interfaces;
using FluentAssertions;
using Microsoft.AspNetCore.Http.HttpResults;
using Moq;

namespace Biotrackr.Vitals.Api.UnitTests.EndpointHandlerTests
{
    public class VitalsHandlersShould
    {
        private readonly Mock<ICosmosRepository> _cosmosRepositoryMock;

        public VitalsHandlersShould()
        {
            _cosmosRepositoryMock = new Mock<ICosmosRepository>();
        }

        [Fact]
        public async Task GetVitalsByDate_ShouldReturnOk_WhenVitalsDocumentIsFound()
        {
            // Arrange
            var date = "2022-01-01";
            var fixture = new Fixture();
            var vitalsDocument = fixture.Create<VitalsDocument>();
            vitalsDocument.Date = date;

            _cosmosRepositoryMock.Setup(x => x.GetVitalsDocumentByDate(date)).ReturnsAsync(vitalsDocument);

            // Act
            var result = await VitalsHandlers.GetVitalsByDate(_cosmosRepositoryMock.Object, date);

            // Assert
            result.Result.Should().BeOfType<Ok<VitalsDocument>>();
            var okResult = result.Result as Ok<VitalsDocument>;
            okResult!.Value.Should().BeEquivalentTo(vitalsDocument);
            okResult.Value.Date.Should().Be(date);
        }

        [Fact]
        public async Task GetVitalsByDate_ShouldReturnNotFound_WhenVitalsDocumentIsNotFound()
        {
            // Arrange
            var date = "2022-01-01";
            _cosmosRepositoryMock.Setup(x => x.GetVitalsDocumentByDate(date)).ReturnsAsync((VitalsDocument?)null);

            // Act
            var result = await VitalsHandlers.GetVitalsByDate(_cosmosRepositoryMock.Object, date);

            // Assert
            result.Result.Should().BeOfType<NotFound>();
        }

        [Fact]
        public async Task GetVitalsByDate_ShouldCallRepositoryWithCorrectDate()
        {
            // Arrange
            var date = "2023-05-15";
            var fixture = new Fixture();
            var vitalsDocument = fixture.Create<VitalsDocument>();
            vitalsDocument.Date = date;

            _cosmosRepositoryMock.Setup(x => x.GetVitalsDocumentByDate(date)).ReturnsAsync(vitalsDocument);

            // Act
            await VitalsHandlers.GetVitalsByDate(_cosmosRepositoryMock.Object, date);

            // Assert
            _cosmosRepositoryMock.Verify(x => x.GetVitalsDocumentByDate(date), Times.Once);
        }

        [Fact]
        public async Task GetVitalsByDate_ShouldPropagateException_WhenRepositoryThrowsException()
        {
            // Arrange
            var date = "2022-01-01";
            var expectedException = new InvalidOperationException("Database connection failed");

            _cosmosRepositoryMock.Setup(x => x.GetVitalsDocumentByDate(date))
                .ThrowsAsync(expectedException);

            // Act & Assert
            var actualException = await Assert.ThrowsAsync<InvalidOperationException>(
                () => VitalsHandlers.GetVitalsByDate(_cosmosRepositoryMock.Object, date));

            actualException.Should().Be(expectedException);
            actualException.Message.Should().Be("Database connection failed");
        }

        [Fact]
        public async Task GetVitalsByDate_ShouldPropagateArgumentException_WhenRepositoryThrowsArgumentException()
        {
            // Arrange
            var date = "invalid-date";
            var expectedException = new ArgumentException("Invalid date format", nameof(date));

            _cosmosRepositoryMock.Setup(x => x.GetVitalsDocumentByDate(date))
                .ThrowsAsync(expectedException);

            // Act & Assert
            var actualException = await Assert.ThrowsAsync<ArgumentException>(
                () => VitalsHandlers.GetVitalsByDate(_cosmosRepositoryMock.Object, date));

            actualException.Should().Be(expectedException);
            actualException.ParamName.Should().Be("date");
        }

        [Fact]
        public async Task GetVitalsByDate_ShouldPropagateTaskCanceledException_WhenOperationIsCanceled()
        {
            // Arrange
            var date = "2022-01-01";
            var expectedException = new TaskCanceledException("Operation was canceled");

            _cosmosRepositoryMock.Setup(x => x.GetVitalsDocumentByDate(date))
                .ThrowsAsync(expectedException);

            // Act & Assert
            var actualException = await Assert.ThrowsAsync<TaskCanceledException>(
                () => VitalsHandlers.GetVitalsByDate(_cosmosRepositoryMock.Object, date));

            actualException.Should().Be(expectedException);
        }

        [Fact]
        public async Task GetAllVitals_ShouldReturnPaginationResponseWithVitalsDocuments()
        {
            // Arrange
            var fixture = new Fixture();
            var vitalsDocuments = fixture.CreateMany<VitalsDocument>(5).ToList();
            var paginationResponse = new PaginationResponse<VitalsDocument>
            {
                Items = vitalsDocuments,
                TotalCount = 10,
                PageNumber = 1,
                PageSize = 20
            };

            _cosmosRepositoryMock.Setup(x => x.GetAllVitalsDocuments(It.IsAny<PaginationRequest>()))
                .ReturnsAsync(paginationResponse);

            // Act
            var result = await VitalsHandlers.GetAllVitals(_cosmosRepositoryMock.Object);

            // Assert
            result.Should().BeOfType<Ok<PaginationResponse<VitalsDocument>>>();
            var okResult = result as Ok<PaginationResponse<VitalsDocument>>;
            okResult!.Value.Should().BeEquivalentTo(paginationResponse);
        }

        [Fact]
        public async Task GetAllVitals_ShouldReturnPaginatedResult_WhenPaginationParametersProvided()
        {
            // Arrange
            var fixture = new Fixture();
            var vitalsDocuments = fixture.CreateMany<VitalsDocument>(10).ToList();
            var paginatedResponse = new PaginationResponse<VitalsDocument>
            {
                Items = vitalsDocuments,
                PageNumber = 2,
                PageSize = 10,
                TotalCount = 50
            };

            _cosmosRepositoryMock.Setup(x => x.GetAllVitalsDocuments(It.IsAny<PaginationRequest>()))
                                .ReturnsAsync(paginatedResponse);

            // Act
            var result = await VitalsHandlers.GetAllVitals(_cosmosRepositoryMock.Object, 2, 10);

            // Assert
            result.Should().BeOfType<Ok<PaginationResponse<VitalsDocument>>>();
            var okResult = result as Ok<PaginationResponse<VitalsDocument>>;
            okResult!.Value.Should().BeEquivalentTo(paginatedResponse);
        }

        [Fact]
        public async Task GetAllVitals_ShouldUseDefaultPaginationParameters_WhenNotProvided()
        {
            // Arrange
            var fixture = new Fixture();
            var paginationResponse = new PaginationResponse<VitalsDocument>
            {
                Items = fixture.CreateMany<VitalsDocument>().ToList(),
                TotalCount = 5,
                PageNumber = 1,
                PageSize = 20
            };

            _cosmosRepositoryMock.Setup(x => x.GetAllVitalsDocuments(It.Is<PaginationRequest>(p =>
                p.PageNumber == 1 && p.PageSize == 20)))
                .ReturnsAsync(paginationResponse);

            // Act
            var result = await VitalsHandlers.GetAllVitals(_cosmosRepositoryMock.Object);

            // Assert
            result.Should().BeOfType<Ok<PaginationResponse<VitalsDocument>>>();
            _cosmosRepositoryMock.Verify(x => x.GetAllVitalsDocuments(It.Is<PaginationRequest>(p =>
                p.PageNumber == 1 && p.PageSize == 20)), Times.Once);
        }

        [Fact]
        public async Task GetAllVitals_ShouldUseProvidedPaginationParameters()
        {
            // Arrange
            var pageNumber = 2;
            var pageSize = 10;
            var fixture = new Fixture();
            var paginationResponse = new PaginationResponse<VitalsDocument>
            {
                Items = fixture.CreateMany<VitalsDocument>().ToList(),
                TotalCount = 25,
                PageNumber = pageNumber,
                PageSize = pageSize
            };

            _cosmosRepositoryMock.Setup(x => x.GetAllVitalsDocuments(It.Is<PaginationRequest>(p =>
                p.PageNumber == pageNumber && p.PageSize == pageSize)))
                .ReturnsAsync(paginationResponse);

            // Act
            var result = await VitalsHandlers.GetAllVitals(_cosmosRepositoryMock.Object, pageNumber, pageSize);

            // Assert
            result.Should().BeOfType<Ok<PaginationResponse<VitalsDocument>>>();
            var okResult = result as Ok<PaginationResponse<VitalsDocument>>;
            okResult!.Value.PageNumber.Should().Be(pageNumber);
            okResult.Value.PageSize.Should().Be(pageSize);

            _cosmosRepositoryMock.Verify(x => x.GetAllVitalsDocuments(It.Is<PaginationRequest>(p =>
                p.PageNumber == pageNumber && p.PageSize == pageSize)), Times.Once);
        }

        [Fact]
        public async Task GetAllVitals_ShouldUseDefaultPageSize_WhenOnlyPageNumberProvided()
        {
            // Arrange
            var fixture = new Fixture();
            var paginatedResponse = new PaginationResponse<VitalsDocument>
            {
                Items = fixture.CreateMany<VitalsDocument>(20).ToList(),
                PageNumber = 2,
                PageSize = 20,
                TotalCount = 100
            };

            _cosmosRepositoryMock.Setup(x => x.GetAllVitalsDocuments(
                It.Is<PaginationRequest>(r => r.PageNumber == 2 && r.PageSize == 20)))
                                .ReturnsAsync(paginatedResponse);

            // Act
            var result = await VitalsHandlers.GetAllVitals(_cosmosRepositoryMock.Object, 2, null);

            // Assert
            result.Should().BeOfType<Ok<PaginationResponse<VitalsDocument>>>();
            _cosmosRepositoryMock.Verify(x => x.GetAllVitalsDocuments(
                It.Is<PaginationRequest>(r => r.PageNumber == 2 && r.PageSize == 20)), Times.Once);
        }

        [Fact]
        public async Task GetAllVitals_ShouldUseDefaultPageNumber_WhenOnlyPageSizeProvided()
        {
            // Arrange
            var pageSize = 15;
            var fixture = new Fixture();
            var paginationResponse = new PaginationResponse<VitalsDocument>
            {
                Items = fixture.CreateMany<VitalsDocument>().ToList(),
                TotalCount = 30,
                PageNumber = 1,
                PageSize = pageSize
            };

            _cosmosRepositoryMock.Setup(x => x.GetAllVitalsDocuments(It.Is<PaginationRequest>(p =>
                p.PageNumber == 1 && p.PageSize == pageSize)))
                .ReturnsAsync(paginationResponse);

            // Act
            var result = await VitalsHandlers.GetAllVitals(_cosmosRepositoryMock.Object, null, pageSize);

            // Assert
            result.Should().BeOfType<Ok<PaginationResponse<VitalsDocument>>>();
            _cosmosRepositoryMock.Verify(x => x.GetAllVitalsDocuments(It.Is<PaginationRequest>(p =>
                p.PageNumber == 1 && p.PageSize == pageSize)), Times.Once);
        }

        [Fact]
        public async Task GetAllVitals_ShouldCallRepositoryWithCorrectPaginationRequest()
        {
            // Arrange
            var pageNumber = 3;
            var pageSize = 25;
            var fixture = new Fixture();
            var paginationResponse = new PaginationResponse<VitalsDocument>
            {
                Items = fixture.CreateMany<VitalsDocument>().ToList(),
                TotalCount = 100,
                PageNumber = pageNumber,
                PageSize = pageSize
            };

            _cosmosRepositoryMock.Setup(x => x.GetAllVitalsDocuments(It.IsAny<PaginationRequest>()))
                .ReturnsAsync(paginationResponse);

            // Act
            await VitalsHandlers.GetAllVitals(_cosmosRepositoryMock.Object, pageNumber, pageSize);

            // Assert
            _cosmosRepositoryMock.Verify(x => x.GetAllVitalsDocuments(
                It.Is<PaginationRequest>(r =>
                    r.PageNumber == pageNumber &&
                    r.PageSize == pageSize)), Times.Once);
        }

        [Fact]
        public async Task GetAllVitals_ShouldPropagateException_WhenRepositoryThrowsException()
        {
            // Arrange
            var expectedException = new InvalidOperationException("Database connection failed");

            _cosmosRepositoryMock.Setup(x => x.GetAllVitalsDocuments(It.IsAny<PaginationRequest>()))
                .ThrowsAsync(expectedException);

            // Act & Assert
            var actualException = await Assert.ThrowsAsync<InvalidOperationException>(
                () => VitalsHandlers.GetAllVitals(_cosmosRepositoryMock.Object));

            actualException.Should().Be(expectedException);
            actualException.Message.Should().Be("Database connection failed");
        }

        [Fact]
        public async Task GetAllVitals_ShouldPropagateArgumentException_WhenRepositoryThrowsArgumentException()
        {
            // Arrange
            var expectedException = new ArgumentException("Invalid pagination parameters");

            _cosmosRepositoryMock.Setup(x => x.GetAllVitalsDocuments(It.IsAny<PaginationRequest>()))
                .ThrowsAsync(expectedException);

            // Act & Assert
            var actualException = await Assert.ThrowsAsync<ArgumentException>(
                () => VitalsHandlers.GetAllVitals(_cosmosRepositoryMock.Object, 2, 10));

            actualException.Should().Be(expectedException);
            actualException.Message.Should().Be("Invalid pagination parameters");
        }

        [Fact]
        public async Task GetAllVitals_ShouldPropagateTimeoutException_WhenRepositoryTimesOut()
        {
            // Arrange
            var expectedException = new TimeoutException("Request timed out");

            _cosmosRepositoryMock.Setup(x => x.GetAllVitalsDocuments(It.IsAny<PaginationRequest>()))
                .ThrowsAsync(expectedException);

            // Act & Assert
            var actualException = await Assert.ThrowsAsync<TimeoutException>(
                () => VitalsHandlers.GetAllVitals(_cosmosRepositoryMock.Object, 1, 20));

            actualException.Should().Be(expectedException);
            actualException.Message.Should().Be("Request timed out");
        }

        [Fact]
        public async Task GetAllVitals_ShouldPropagateTaskCanceledException_WhenOperationIsCanceled()
        {
            // Arrange
            var expectedException = new TaskCanceledException("Operation was canceled");

            _cosmosRepositoryMock.Setup(x => x.GetAllVitalsDocuments(It.IsAny<PaginationRequest>()))
                .ThrowsAsync(expectedException);

            // Act & Assert
            var actualException = await Assert.ThrowsAsync<TaskCanceledException>(
                () => VitalsHandlers.GetAllVitals(_cosmosRepositoryMock.Object));

            actualException.Should().Be(expectedException);
        }

        [Fact]
        public async Task GetAllVitals_ShouldStillCallRepository_EvenWhenExceptionExpected()
        {
            // Arrange
            var expectedException = new InvalidOperationException("Test exception");

            _cosmosRepositoryMock.Setup(x => x.GetAllVitalsDocuments(It.IsAny<PaginationRequest>()))
                .ThrowsAsync(expectedException);

            // Act & Assert
            await Assert.ThrowsAsync<InvalidOperationException>(
                () => VitalsHandlers.GetAllVitals(_cosmosRepositoryMock.Object, 1, 10));

            // Verify the repository was still called with correct parameters
            _cosmosRepositoryMock.Verify(x => x.GetAllVitalsDocuments(
                It.Is<PaginationRequest>(r => r.PageNumber == 1 && r.PageSize == 10)), Times.Once);
        }

        [Fact]
        public async Task GetVitalsByDateRange_ShouldReturnPaginatedVitalsDocuments_WhenValidDateRange()
        {
            // Arrange
            var startDate = "2022-01-01";
            var endDate = "2022-01-31";
            var pageNumber = 1;
            var pageSize = 10;
            var fixture = new Fixture();
            var vitalsDocuments = fixture.CreateMany<VitalsDocument>(10).ToList();
            var paginationResponse = new PaginationResponse<VitalsDocument>
            {
                Items = vitalsDocuments,
                TotalCount = 25,
                PageNumber = pageNumber,
                PageSize = pageSize
            };

            _cosmosRepositoryMock.Setup(x => x.GetVitalsByDateRange(startDate, endDate, It.IsAny<PaginationRequest>()))
                .ReturnsAsync(paginationResponse);

            // Act
            var result = await VitalsHandlers.GetVitalsByDateRange(_cosmosRepositoryMock.Object, startDate, endDate, pageNumber, pageSize);

            // Assert
            result.Result.Should().BeOfType<Ok<PaginationResponse<VitalsDocument>>>();
            var okResult = result.Result as Ok<PaginationResponse<VitalsDocument>>;
            okResult!.Value.Should().BeEquivalentTo(paginationResponse);
        }

        [Fact]
        public async Task GetVitalsByDateRange_ShouldUseDefaultPagination_WhenNotProvided()
        {
            // Arrange
            var startDate = "2022-01-01";
            var endDate = "2022-01-31";
            var fixture = new Fixture();
            var paginationResponse = new PaginationResponse<VitalsDocument>
            {
                Items = fixture.CreateMany<VitalsDocument>(20).ToList(),
                TotalCount = 20,
                PageNumber = 1,
                PageSize = 20
            };

            _cosmosRepositoryMock.Setup(x => x.GetVitalsByDateRange(startDate, endDate,
                It.Is<PaginationRequest>(p => p.PageNumber == 1 && p.PageSize == 20)))
                .ReturnsAsync(paginationResponse);

            // Act
            var result = await VitalsHandlers.GetVitalsByDateRange(_cosmosRepositoryMock.Object, startDate, endDate);

            // Assert
            result.Result.Should().BeOfType<Ok<PaginationResponse<VitalsDocument>>>();
        }

        [Fact]
        public async Task GetVitalsByDateRange_ShouldReturnBadRequest_WhenStartDateIsInvalid()
        {
            // Arrange
            var invalidStartDate = "invalid-date";
            var endDate = "2024-01-31";

            // Act
            var result = await VitalsHandlers.GetVitalsByDateRange(_cosmosRepositoryMock.Object, invalidStartDate, endDate);

            // Assert
            result.Result.Should().BeOfType<BadRequest>();
        }

        [Fact]
        public async Task GetVitalsByDateRange_ShouldReturnBadRequest_WhenEndDateIsInvalid()
        {
            // Arrange
            var startDate = "2024-01-01";
            var invalidEndDate = "not-a-date";

            // Act
            var result = await VitalsHandlers.GetVitalsByDateRange(_cosmosRepositoryMock.Object, startDate, invalidEndDate);

            // Assert
            result.Result.Should().BeOfType<BadRequest>();
        }

        [Fact]
        public async Task GetVitalsByDateRange_ShouldReturnBadRequest_WhenBothDatesAreInvalid()
        {
            // Arrange
            var invalidStartDate = "invalid-start";
            var invalidEndDate = "invalid-end";

            // Act
            var result = await VitalsHandlers.GetVitalsByDateRange(_cosmosRepositoryMock.Object, invalidStartDate, invalidEndDate);

            // Assert
            result.Result.Should().BeOfType<BadRequest>();
        }

        [Fact]
        public async Task GetVitalsByDateRange_ShouldReturnBadRequest_WhenStartDateIsAfterEndDate()
        {
            // Arrange
            var startDate = "2024-12-31";
            var endDate = "2024-01-01";

            // Act
            var result = await VitalsHandlers.GetVitalsByDateRange(_cosmosRepositoryMock.Object, startDate, endDate);

            // Assert
            result.Result.Should().BeOfType<BadRequest>();
        }

        [Fact]
        public async Task GetVitalsByDateRange_ShouldReturnOk_WhenStartDateEqualsEndDate()
        {
            // Arrange
            var startDate = "2024-01-15";
            var endDate = "2024-01-15";
            var expectedResponse = new PaginationResponse<VitalsDocument>
            {
                Items = new List<VitalsDocument>(),
                TotalCount = 0,
                PageNumber = 1,
                PageSize = 20
            };

            _cosmosRepositoryMock.Setup(x => x.GetVitalsByDateRange(startDate, endDate, It.IsAny<PaginationRequest>()))
                .ReturnsAsync(expectedResponse);

            // Act
            var result = await VitalsHandlers.GetVitalsByDateRange(_cosmosRepositoryMock.Object, startDate, endDate);

            // Assert
            result.Result.Should().BeOfType<Ok<PaginationResponse<VitalsDocument>>>();
        }
    }
}
