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

        [Fact]
        public async Task GetSleepsByDateRange_ShouldReturnOkWithPaginationResponse_WhenValidDatesProvided()
        {
            // Arrange
            var startDate = "2022-01-01";
            var endDate = "2022-01-31";
            var fixture = new Fixture();
            var sleepDocuments = fixture.CreateMany<SleepDocument>(5).ToList();
            var paginationResponse = new PaginationResponse<SleepDocument>
            {
                Items = sleepDocuments,
                TotalCount = 10,
                PageNumber = 1,
                PageSize = 20
            };

            _cosmosRepositoryMock.Setup(x => x.GetSleepDocumentsByDateRange(startDate, endDate, It.IsAny<PaginationRequest>()))
                .ReturnsAsync(paginationResponse);

            // Act
            var result = await SleepHandlers.GetSleepsByDateRange(_cosmosRepositoryMock.Object, startDate, endDate);

            // Assert
            result.Result.Should().BeOfType<Ok<PaginationResponse<SleepDocument>>>();
            var okResult = result.Result as Ok<PaginationResponse<SleepDocument>>;
            okResult.Value.Should().BeEquivalentTo(paginationResponse);
        }

        [Fact]
        public async Task GetSleepsByDateRange_ShouldReturnBadRequest_WhenStartDateIsInvalid()
        {
            // Arrange
            var startDate = "invalid-date";
            var endDate = "2022-01-31";

            // Act
            var result = await SleepHandlers.GetSleepsByDateRange(_cosmosRepositoryMock.Object, startDate, endDate);

            // Assert
            result.Result.Should().BeOfType<BadRequest>();
            _cosmosRepositoryMock.Verify(x => x.GetSleepDocumentsByDateRange(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<PaginationRequest>()), Times.Never);
        }

        [Fact]
        public async Task GetSleepsByDateRange_ShouldReturnBadRequest_WhenEndDateIsInvalid()
        {
            // Arrange
            var startDate = "2022-01-01";
            var endDate = "invalid-date";

            // Act
            var result = await SleepHandlers.GetSleepsByDateRange(_cosmosRepositoryMock.Object, startDate, endDate);

            // Assert
            result.Result.Should().BeOfType<BadRequest>();
            _cosmosRepositoryMock.Verify(x => x.GetSleepDocumentsByDateRange(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<PaginationRequest>()), Times.Never);
        }

        [Fact]
        public async Task GetSleepsByDateRange_ShouldReturnBadRequest_WhenBothDatesAreInvalid()
        {
            // Arrange
            var startDate = "invalid-start-date";
            var endDate = "invalid-end-date";

            // Act
            var result = await SleepHandlers.GetSleepsByDateRange(_cosmosRepositoryMock.Object, startDate, endDate);

            // Assert
            result.Result.Should().BeOfType<BadRequest>();
            _cosmosRepositoryMock.Verify(x => x.GetSleepDocumentsByDateRange(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<PaginationRequest>()), Times.Never);
        }

        [Fact]
        public async Task GetSleepsByDateRange_ShouldReturnBadRequest_WhenStartDateIsAfterEndDate()
        {
            // Arrange
            var startDate = "2022-01-31";
            var endDate = "2022-01-01";

            // Act
            var result = await SleepHandlers.GetSleepsByDateRange(_cosmosRepositoryMock.Object, startDate, endDate);

            // Assert
            result.Result.Should().BeOfType<BadRequest>();
            _cosmosRepositoryMock.Verify(x => x.GetSleepDocumentsByDateRange(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<PaginationRequest>()), Times.Never);
        }

        [Fact]
        public async Task GetSleepsByDateRange_ShouldReturnOk_WhenStartDateEqualsEndDate()
        {
            // Arrange
            var startDate = "2022-01-15";
            var endDate = "2022-01-15";
            var fixture = new Fixture();
            var sleepDocuments = fixture.CreateMany<SleepDocument>(2).ToList();
            var paginationResponse = new PaginationResponse<SleepDocument>
            {
                Items = sleepDocuments,
                TotalCount = 2,
                PageNumber = 1,
                PageSize = 20
            };

            _cosmosRepositoryMock.Setup(x => x.GetSleepDocumentsByDateRange(startDate, endDate, It.IsAny<PaginationRequest>()))
                .ReturnsAsync(paginationResponse);

            // Act
            var result = await SleepHandlers.GetSleepsByDateRange(_cosmosRepositoryMock.Object, startDate, endDate);

            // Assert
            result.Result.Should().BeOfType<Ok<PaginationResponse<SleepDocument>>>();
            var okResult = result.Result as Ok<PaginationResponse<SleepDocument>>;
            okResult.Value.Should().BeEquivalentTo(paginationResponse);
        }

        [Fact]
        public async Task GetSleepsByDateRange_ShouldUseDefaultPaginationParameters_WhenNotProvided()
        {
            // Arrange
            var startDate = "2022-01-01";
            var endDate = "2022-01-31";
            var fixture = new Fixture();
            var paginationResponse = new PaginationResponse<SleepDocument>
            {
                Items = fixture.CreateMany<SleepDocument>().ToList(),
                TotalCount = 5,
                PageNumber = 1,
                PageSize = 20
            };

            _cosmosRepositoryMock.Setup(x => x.GetSleepDocumentsByDateRange(startDate, endDate, It.Is<PaginationRequest>(p =>
                p.PageNumber == 1 && p.PageSize == 20)))
                .ReturnsAsync(paginationResponse);

            // Act
            var result = await SleepHandlers.GetSleepsByDateRange(_cosmosRepositoryMock.Object, startDate, endDate);

            // Assert
            result.Result.Should().BeOfType<Ok<PaginationResponse<SleepDocument>>>();
            _cosmosRepositoryMock.Verify(x => x.GetSleepDocumentsByDateRange(startDate, endDate, It.Is<PaginationRequest>(p =>
                p.PageNumber == 1 && p.PageSize == 20)), Times.Once);
        }

        [Fact]
        public async Task GetSleepsByDateRange_ShouldUseProvidedPaginationParameters()
        {
            // Arrange
            var startDate = "2022-01-01";
            var endDate = "2022-01-31";
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

            _cosmosRepositoryMock.Setup(x => x.GetSleepDocumentsByDateRange(startDate, endDate, It.Is<PaginationRequest>(p =>
                p.PageNumber == pageNumber && p.PageSize == pageSize)))
                .ReturnsAsync(paginationResponse);

            // Act
            var result = await SleepHandlers.GetSleepsByDateRange(_cosmosRepositoryMock.Object, startDate, endDate, pageNumber, pageSize);

            // Assert
            result.Result.Should().BeOfType<Ok<PaginationResponse<SleepDocument>>>();
            var okResult = result.Result as Ok<PaginationResponse<SleepDocument>>;
            okResult.Value.PageNumber.Should().Be(pageNumber);
            okResult.Value.PageSize.Should().Be(pageSize);

            _cosmosRepositoryMock.Verify(x => x.GetSleepDocumentsByDateRange(startDate, endDate, It.Is<PaginationRequest>(p =>
                p.PageNumber == pageNumber && p.PageSize == pageSize)), Times.Once);
        }

        [Fact]
        public async Task GetSleepsByDateRange_ShouldUseDefaultPageSize_WhenOnlyPageNumberProvided()
        {
            // Arrange
            var startDate = "2022-01-01";
            var endDate = "2022-01-31";
            var pageNumber = 3;
            var fixture = new Fixture();
            var paginationResponse = new PaginationResponse<SleepDocument>
            {
                Items = fixture.CreateMany<SleepDocument>(20).ToList(),
                PageNumber = pageNumber,
                PageSize = 20,
                TotalCount = 100
            };

            _cosmosRepositoryMock.Setup(x => x.GetSleepDocumentsByDateRange(startDate, endDate,
                It.Is<PaginationRequest>(r => r.PageNumber == pageNumber && r.PageSize == 20)))
                .ReturnsAsync(paginationResponse);

            // Act
            var result = await SleepHandlers.GetSleepsByDateRange(_cosmosRepositoryMock.Object, startDate, endDate, pageNumber, null);

            // Assert
            result.Result.Should().BeOfType<Ok<PaginationResponse<SleepDocument>>>();
            _cosmosRepositoryMock.Verify(x => x.GetSleepDocumentsByDateRange(startDate, endDate,
                It.Is<PaginationRequest>(r => r.PageNumber == pageNumber && r.PageSize == 20)), Times.Once);
        }

        [Fact]
        public async Task GetSleepsByDateRange_ShouldUseDefaultPageNumber_WhenOnlyPageSizeProvided()
        {
            // Arrange
            var startDate = "2022-01-01";
            var endDate = "2022-01-31";
            var pageSize = 15;
            var fixture = new Fixture();
            var paginationResponse = new PaginationResponse<SleepDocument>
            {
                Items = fixture.CreateMany<SleepDocument>().ToList(),
                TotalCount = 30,
                PageNumber = 1,
                PageSize = pageSize
            };

            _cosmosRepositoryMock.Setup(x => x.GetSleepDocumentsByDateRange(startDate, endDate, It.Is<PaginationRequest>(p =>
                p.PageNumber == 1 && p.PageSize == pageSize)))
                .ReturnsAsync(paginationResponse);

            // Act
            var result = await SleepHandlers.GetSleepsByDateRange(_cosmosRepositoryMock.Object, startDate, endDate, null, pageSize);

            // Assert
            result.Result.Should().BeOfType<Ok<PaginationResponse<SleepDocument>>>();
            _cosmosRepositoryMock.Verify(x => x.GetSleepDocumentsByDateRange(startDate, endDate, It.Is<PaginationRequest>(p =>
                p.PageNumber == 1 && p.PageSize == pageSize)), Times.Once);
        }

        [Fact]
        public async Task GetSleepsByDateRange_ShouldCallRepositoryWithCorrectParameters()
        {
            // Arrange
            var startDate = "2022-01-01";
            var endDate = "2022-01-31";
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

            _cosmosRepositoryMock.Setup(x => x.GetSleepDocumentsByDateRange(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<PaginationRequest>()))
                .ReturnsAsync(paginationResponse);

            // Act
            await SleepHandlers.GetSleepsByDateRange(_cosmosRepositoryMock.Object, startDate, endDate, pageNumber, pageSize);

            // Assert
            _cosmosRepositoryMock.Verify(x => x.GetSleepDocumentsByDateRange(
                startDate,
                endDate,
                It.Is<PaginationRequest>(r =>
                    r.PageNumber == pageNumber &&
                    r.PageSize == pageSize)), Times.Once);
        }

        [Fact]
        public async Task GetSleepsByDateRange_ShouldReturnEmptyResult_WhenNoSleepDocumentsInRange()
        {
            // Arrange
            var startDate = "2022-01-01";
            var endDate = "2022-01-31";
            var paginationResponse = new PaginationResponse<SleepDocument>
            {
                Items = new List<SleepDocument>(),
                TotalCount = 0,
                PageNumber = 1,
                PageSize = 20
            };

            _cosmosRepositoryMock.Setup(x => x.GetSleepDocumentsByDateRange(startDate, endDate, It.IsAny<PaginationRequest>()))
                .ReturnsAsync(paginationResponse);

            // Act
            var result = await SleepHandlers.GetSleepsByDateRange(_cosmosRepositoryMock.Object, startDate, endDate);

            // Assert
            result.Result.Should().BeOfType<Ok<PaginationResponse<SleepDocument>>>();
            var okResult = result.Result as Ok<PaginationResponse<SleepDocument>>;
            okResult.Value.Items.Should().BeEmpty();
            okResult.Value.TotalCount.Should().Be(0);
        }

        [Fact]
        public async Task GetSleepsByDateRange_ShouldPropagateException_WhenRepositoryThrowsException()
        {
            // Arrange
            var startDate = "2022-01-01";
            var endDate = "2022-01-31";
            var expectedException = new InvalidOperationException("Database connection failed");

            _cosmosRepositoryMock.Setup(x => x.GetSleepDocumentsByDateRange(startDate, endDate, It.IsAny<PaginationRequest>()))
                .ThrowsAsync(expectedException);

            // Act & Assert
            var actualException = await Assert.ThrowsAsync<InvalidOperationException>(
                () => SleepHandlers.GetSleepsByDateRange(_cosmosRepositoryMock.Object, startDate, endDate));

            actualException.Should().Be(expectedException);
            actualException.Message.Should().Be("Database connection failed");
        }

        [Fact]
        public async Task GetSleepsByDateRange_ShouldPropagateArgumentException_WhenRepositoryThrowsArgumentException()
        {
            // Arrange
            var startDate = "2022-01-01";
            var endDate = "2022-01-31";
            var expectedException = new ArgumentException("Invalid date range parameters");

            _cosmosRepositoryMock.Setup(x => x.GetSleepDocumentsByDateRange(startDate, endDate, It.IsAny<PaginationRequest>()))
                .ThrowsAsync(expectedException);

            // Act & Assert
            var actualException = await Assert.ThrowsAsync<ArgumentException>(
                () => SleepHandlers.GetSleepsByDateRange(_cosmosRepositoryMock.Object, startDate, endDate, 1, 20));

            actualException.Should().Be(expectedException);
            actualException.Message.Should().Be("Invalid date range parameters");
        }

        [Fact]
        public async Task GetSleepsByDateRange_ShouldPropagateTimeoutException_WhenRepositoryTimesOut()
        {
            // Arrange
            var startDate = "2022-01-01";
            var endDate = "2022-01-31";
            var expectedException = new TimeoutException("Request timed out");

            _cosmosRepositoryMock.Setup(x => x.GetSleepDocumentsByDateRange(startDate, endDate, It.IsAny<PaginationRequest>()))
                .ThrowsAsync(expectedException);

            // Act & Assert
            var actualException = await Assert.ThrowsAsync<TimeoutException>(
                () => SleepHandlers.GetSleepsByDateRange(_cosmosRepositoryMock.Object, startDate, endDate, 1, 20));

            actualException.Should().Be(expectedException);
            actualException.Message.Should().Be("Request timed out");
        }

        [Fact]
        public async Task GetSleepsByDateRange_ShouldPropagateTaskCanceledException_WhenOperationIsCanceled()
        {
            // Arrange
            var startDate = "2022-01-01";
            var endDate = "2022-01-31";
            var expectedException = new TaskCanceledException("Operation was canceled");

            _cosmosRepositoryMock.Setup(x => x.GetSleepDocumentsByDateRange(startDate, endDate, It.IsAny<PaginationRequest>()))
                .ThrowsAsync(expectedException);

            // Act & Assert
            var actualException = await Assert.ThrowsAsync<TaskCanceledException>(
                () => SleepHandlers.GetSleepsByDateRange(_cosmosRepositoryMock.Object, startDate, endDate));

            actualException.Should().Be(expectedException);
        }

        [Fact]
        public async Task GetSleepsByDateRange_ShouldHandleVariousValidDateFormats()
        {
            // Arrange - Test only reliably valid ISO format dates
            var testCases = new[]
            {
                ("2022-01-01", "2022-01-31"),   // ISO format
                ("2022-12-31", "2023-01-01"),   // Year boundary
                ("2022-02-28", "2022-03-01"),   // Month boundary
                ("2024-02-29", "2024-03-01")    // Leap year test
            };

            foreach (var (startDate, endDate) in testCases)
            {
                var fixture = new Fixture();
                var paginationResponse = new PaginationResponse<SleepDocument>
                {
                    Items = fixture.CreateMany<SleepDocument>(2).ToList(),
                    TotalCount = 2,
                    PageNumber = 1,
                    PageSize = 20
                };

                _cosmosRepositoryMock.Setup(x => x.GetSleepDocumentsByDateRange(startDate, endDate, It.IsAny<PaginationRequest>()))
                    .ReturnsAsync(paginationResponse);

                // Act
                var result = await SleepHandlers.GetSleepsByDateRange(_cosmosRepositoryMock.Object, startDate, endDate);

                // Assert
                result.Result.Should().BeOfType<Ok<PaginationResponse<SleepDocument>>>();

                // Reset the mock for the next iteration
                _cosmosRepositoryMock.Reset();
            }
        }

        [Fact]
        public async Task GetSleepsByDateRange_ShouldHandleVariousInvalidDateFormats()
        {
            // Arrange - Test clearly invalid date formats that will definitely fail DateOnly.TryParse
            var invalidDateCases = new[]
            {
                ("2022-13-01", "2022-01-31"), // Invalid month
                ("2022-01-32", "2022-01-31"), // Invalid day
                ("2022-02-30", "2022-03-01"), // Invalid day for February
                ("invalid", "2022-01-31"),     // Completely invalid format
                ("2022-01-01", "not-a-date"),  // Invalid end date
                ("", "2022-01-31"),            // Empty start date
                ("2022-01-01", ""),            // Empty end date
                ("abc-def-ghi", "2022-01-31"), // Completely invalid
                ("2022-01-01", "xyz-abc-def")  // Invalid end date format
            };

            foreach (var (startDate, endDate) in invalidDateCases)
            {
                // Act
                var result = await SleepHandlers.GetSleepsByDateRange(_cosmosRepositoryMock.Object, startDate, endDate);

                // Assert
                result.Result.Should().BeOfType<BadRequest>($"Expected BadRequest for dates: {startDate}, {endDate}");
            }

            // Verify repository was never called for any invalid dates
            _cosmosRepositoryMock.Verify(x => x.GetSleepDocumentsByDateRange(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<PaginationRequest>()), Times.Never);
        }

        [Fact]
        public async Task GetSleepsByDateRange_ShouldStillCallRepository_EvenWhenExceptionExpected()
        {
            // Arrange
            var startDate = "2022-01-01";
            var endDate = "2022-01-31";
            var expectedException = new InvalidOperationException("Test exception");

            _cosmosRepositoryMock.Setup(x => x.GetSleepDocumentsByDateRange(startDate, endDate, It.IsAny<PaginationRequest>()))
                .ThrowsAsync(expectedException);

            // Act & Assert
            await Assert.ThrowsAsync<InvalidOperationException>(
                () => SleepHandlers.GetSleepsByDateRange(_cosmosRepositoryMock.Object, startDate, endDate, 1, 10));

            // Verify the repository was still called with correct parameters
            _cosmosRepositoryMock.Verify(x => x.GetSleepDocumentsByDateRange(startDate, endDate,
                It.Is<PaginationRequest>(r => r.PageNumber == 1 && r.PageSize == 10)), Times.Once);
        }
    }
}
