using AutoFixture;
using Biotrackr.Activity.Api.EndpointHandlers;
using Biotrackr.Activity.Api.Models;
using Biotrackr.Activity.Api.Repositories.Interfaces;
using Biotrackr.Activity.Api.Services;
using FluentAssertions;
using Microsoft.AspNetCore.Http.HttpResults;
using Moq;

namespace Biotrackr.Activity.Api.UnitTests.EndpointHandlerTests
{
    public class ActivityHandlersShould
    {
        private readonly Mock<ICosmosRepository> _cosmosRepositoryMock;
        private readonly ActivityDocumentTranslator _translator = new();

        public ActivityHandlersShould()
        {
            _cosmosRepositoryMock = new Mock<ICosmosRepository>();
        }

        // Stored documents as version 1 (no schemaVersion, no Google payload), the shape every Fitbit-era day has.
        private static Fixture CreateFixture()
        {
            var fixture = new Fixture();
            fixture.Customize<ActivityStoredDocument>(composer => composer
                .With(d => d.SchemaVersion, (int?)null)
                .With(d => d.Provider, (string?)null)
                .Without(d => d.Google));
            return fixture;
        }

        [Fact]
        public async Task GetActivityByDate_ShouldReturnOk_WhenActivityIsFound()
        {
            // Arrange
            var date = "2022-01-01";
            var fixture = CreateFixture();
            var activityDocument = fixture.Create<ActivityStoredDocument>();
            activityDocument.Date = date;

            _cosmosRepositoryMock.Setup(x => x.GetActivitySummaryByDate(date)).ReturnsAsync(activityDocument);

            // Act
            var result = await ActivityHandlers.GetActivityByDate(_cosmosRepositoryMock.Object, _translator, date);

            // Assert
            result.Result.Should().BeOfType<Ok<ActivityDocument>>();
        }

        [Fact]
        public async Task GetActivityByDate_ShouldReturnNotFound_WhenActivityIsNotFound()
        {
            // Arrange
            var date = "2022-01-01";
            _cosmosRepositoryMock.Setup(x => x.GetActivitySummaryByDate(date))
                .ReturnsAsync((ActivityStoredDocument)null);

            // Act
            var result = await ActivityHandlers.GetActivityByDate(_cosmosRepositoryMock.Object, _translator, date);

            // Assert
            result.Result.Should().BeOfType<NotFound>();
        }

        [Fact]
        public async Task GetActivityByDate_ShouldReturnBadRequest_WhenDateFormatIsInvalid()
        {
            // Arrange
            var invalidDate = "invalid-date-format";

            // Act
            var result = await ActivityHandlers.GetActivityByDate(_cosmosRepositoryMock.Object, _translator, invalidDate);

            // Assert
            result.Result.Should().BeOfType<BadRequest>();
            _cosmosRepositoryMock.Verify(x => x.GetActivitySummaryByDate(It.IsAny<string>()), Times.Never);
        }

        [Fact]
        public async Task GetAllActivities_ShouldReturnPaginatedResult_WhenPaginationParametersProvided()
        {
            // Arrange
            var fixture = CreateFixture();
            var activityDocuments = fixture.CreateMany<ActivityStoredDocument>(10).ToList();
            var paginatedResponse = new PaginationResponse<ActivityStoredDocument>
            {
                Items = activityDocuments,
                PageNumber = 2,
                PageSize = 10,
                TotalCount = 50
            };

            _cosmosRepositoryMock.Setup(x => x.GetAllActivitySummaries(It.IsAny<PaginationRequest>()))
                                .ReturnsAsync(paginatedResponse);

            // Act
            var result = await ActivityHandlers.GetAllActivities(_cosmosRepositoryMock.Object, _translator, 2, 10);

            // Assert
            result.Should().BeOfType<Ok<PaginationResponse<ActivityDocument>>>();
            var okResult = result as Ok<PaginationResponse<ActivityDocument>>;
            okResult.Value.Should().BeEquivalentTo(new { paginatedResponse.TotalCount, paginatedResponse.PageNumber, paginatedResponse.PageSize });
            okResult.Value.Items.Select(i => i.Id).Should().Equal(paginatedResponse.Items.Select(i => i.Id));
        }

        [Fact]
        public async Task GetAllActivities_ShouldUseDefaultPageSize_WhenOnlyPageNumberProvided()
        {
            // Arrange
            var fixture = CreateFixture();
            var paginatedResponse = new PaginationResponse<ActivityStoredDocument>
            {
                Items = fixture.CreateMany<ActivityStoredDocument>(20).ToList(),
                PageNumber = 2,
                PageSize = 20,
                TotalCount = 100
            };

            _cosmosRepositoryMock.Setup(x => x.GetAllActivitySummaries(
                It.Is<PaginationRequest>(r => r.PageNumber == 2 && r.PageSize == 20)))
                                .ReturnsAsync(paginatedResponse);

            // Act
            var result = await ActivityHandlers.GetAllActivities(_cosmosRepositoryMock.Object, _translator, 2, null);

            // Assert
            result.Should().BeOfType<Ok<PaginationResponse<ActivityDocument>>>();
            _cosmosRepositoryMock.Verify(x => x.GetAllActivitySummaries(
                It.Is<PaginationRequest>(r => r.PageNumber == 2 && r.PageSize == 20)), Times.Once);
        }

        [Fact]
        public async Task GetAllActivities_ShouldUseDefaultPageNumber_WhenOnlyPageSizeProvided()
        {
            // Arrange
            var fixture = CreateFixture();
            var paginatedResponse = new PaginationResponse<ActivityStoredDocument>
            {
                Items = fixture.CreateMany<ActivityStoredDocument>(50).ToList(),
                PageNumber = 1,
                PageSize = 50,
                TotalCount = 100
            };

            _cosmosRepositoryMock.Setup(x => x.GetAllActivitySummaries(
                It.Is<PaginationRequest>(r => r.PageNumber == 1 && r.PageSize == 50)))
                                .ReturnsAsync(paginatedResponse);

            // Act
            var result = await ActivityHandlers.GetAllActivities(_cosmosRepositoryMock.Object, _translator, null, 50);

            // Assert
            result.Should().BeOfType<Ok<PaginationResponse<ActivityDocument>>>();
            _cosmosRepositoryMock.Verify(x => x.GetAllActivitySummaries(
                It.Is<PaginationRequest>(r => r.PageNumber == 1 && r.PageSize == 50)), Times.Once);
        }

        [Fact]
        public async Task GetActivitiesByDateRange_ShouldReturnOk_WhenValidDateRangeProvided()
        {
            // Arrange
            var startDate = "2023-01-01";
            var endDate = "2023-01-31";
            var fixture = CreateFixture();
            var activityDocuments = fixture.CreateMany<ActivityStoredDocument>(5).ToList();
            var paginatedResponse = new PaginationResponse<ActivityStoredDocument>
            {
                Items = activityDocuments,
                PageNumber = 1,
                PageSize = 20,
                TotalCount = 5
            };

            _cosmosRepositoryMock.Setup(x => x.GetActivitiesByDateRange(startDate, endDate, It.IsAny<PaginationRequest>()))
                                .ReturnsAsync(paginatedResponse);

            // Act
            var result = await ActivityHandlers.GetActivitiesByDateRange(_cosmosRepositoryMock.Object, _translator, startDate, endDate);

            // Assert
            result.Result.Should().BeOfType<Ok<PaginationResponse<ActivityDocument>>>();
            var okResult = result.Result as Ok<PaginationResponse<ActivityDocument>>;
            okResult.Value.Should().BeEquivalentTo(new { paginatedResponse.TotalCount, paginatedResponse.PageNumber, paginatedResponse.PageSize });
            okResult.Value.Items.Select(i => i.Id).Should().Equal(paginatedResponse.Items.Select(i => i.Id));
        }

        [Fact]
        public async Task GetActivitiesByDateRange_ShouldReturnBadRequest_WhenStartDateIsInvalid()
        {
            // Arrange
            var invalidStartDate = "invalid-date";
            var endDate = "2023-01-31";

            // Act
            var result = await ActivityHandlers.GetActivitiesByDateRange(_cosmosRepositoryMock.Object, _translator, invalidStartDate, endDate);

            // Assert
            result.Result.Should().BeOfType<BadRequest>();
            _cosmosRepositoryMock.Verify(x => x.GetActivitiesByDateRange(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<PaginationRequest>()), Times.Never);
        }

        [Fact]
        public async Task GetActivitiesByDateRange_ShouldReturnBadRequest_WhenEndDateIsInvalid()
        {
            // Arrange
            var startDate = "2023-01-01";
            var invalidEndDate = "invalid-date";

            // Act
            var result = await ActivityHandlers.GetActivitiesByDateRange(_cosmosRepositoryMock.Object, _translator, startDate, invalidEndDate);

            // Assert
            result.Result.Should().BeOfType<BadRequest>();
            _cosmosRepositoryMock.Verify(x => x.GetActivitiesByDateRange(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<PaginationRequest>()), Times.Never);
        }

        [Fact]
        public async Task GetActivitiesByDateRange_ShouldReturnBadRequest_WhenStartDateIsAfterEndDate()
        {
            // Arrange
            var startDate = "2023-01-31";
            var endDate = "2023-01-01";

            // Act
            var result = await ActivityHandlers.GetActivitiesByDateRange(_cosmosRepositoryMock.Object, _translator, startDate, endDate);

            // Assert
            result.Result.Should().BeOfType<BadRequest>();
            _cosmosRepositoryMock.Verify(x => x.GetActivitiesByDateRange(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<PaginationRequest>()), Times.Never);
        }

        [Fact]
        public async Task GetActivitiesByDateRange_ShouldUseDefaultPagination_WhenPaginationParametersNotProvided()
        {
            // Arrange
            var startDate = "2023-01-01";
            var endDate = "2023-01-31";
            var fixture = CreateFixture();
            var paginatedResponse = new PaginationResponse<ActivityStoredDocument>
            {
                Items = fixture.CreateMany<ActivityStoredDocument>(20).ToList(),
                PageNumber = 1,
                PageSize = 20,
                TotalCount = 20
            };

            _cosmosRepositoryMock.Setup(x => x.GetActivitiesByDateRange(startDate, endDate, 
                It.Is<PaginationRequest>(r => r.PageNumber == 1 && r.PageSize == 20)))
                                .ReturnsAsync(paginatedResponse);

            // Act
            var result = await ActivityHandlers.GetActivitiesByDateRange(_cosmosRepositoryMock.Object, _translator, startDate, endDate);

            // Assert
            result.Result.Should().BeOfType<Ok<PaginationResponse<ActivityDocument>>>();
            _cosmosRepositoryMock.Verify(x => x.GetActivitiesByDateRange(startDate, endDate, 
                It.Is<PaginationRequest>(r => r.PageNumber == 1 && r.PageSize == 20)), Times.Once);
        }

        [Fact]
        public async Task GetActivitiesByDateRange_ShouldUseCustomPagination_WhenPaginationParametersProvided()
        {
            // Arrange
            var startDate = "2023-01-01";
            var endDate = "2023-01-31";
            var pageNumber = 3;
            var pageSize = 15;
            var fixture = CreateFixture();
            var paginatedResponse = new PaginationResponse<ActivityStoredDocument>
            {
                Items = fixture.CreateMany<ActivityStoredDocument>(15).ToList(),
                PageNumber = pageNumber,
                PageSize = pageSize,
                TotalCount = 45
            };

            _cosmosRepositoryMock.Setup(x => x.GetActivitiesByDateRange(startDate, endDate, 
                It.Is<PaginationRequest>(r => r.PageNumber == pageNumber && r.PageSize == pageSize)))
                                .ReturnsAsync(paginatedResponse);

            // Act
            var result = await ActivityHandlers.GetActivitiesByDateRange(_cosmosRepositoryMock.Object, _translator, startDate, endDate, pageNumber, pageSize);

            // Assert
            result.Result.Should().BeOfType<Ok<PaginationResponse<ActivityDocument>>>();
            var okResult = result.Result as Ok<PaginationResponse<ActivityDocument>>;
            okResult.Value.PageNumber.Should().Be(pageNumber);
            okResult.Value.PageSize.Should().Be(pageSize);
            _cosmosRepositoryMock.Verify(x => x.GetActivitiesByDateRange(startDate, endDate, 
                It.Is<PaginationRequest>(r => r.PageNumber == pageNumber && r.PageSize == pageSize)), Times.Once);
        }

        [Fact]
        public async Task GetActivitiesByDateRange_ShouldHandleSameDateRange()
        {
            // Arrange
            var sameDate = "2023-01-15";
            var fixture = CreateFixture();
            var activityDocuments = fixture.CreateMany<ActivityStoredDocument>(2).ToList();
            var paginatedResponse = new PaginationResponse<ActivityStoredDocument>
            {
                Items = activityDocuments,
                PageNumber = 1,
                PageSize = 20,
                TotalCount = 2
            };

            _cosmosRepositoryMock.Setup(x => x.GetActivitiesByDateRange(sameDate, sameDate, It.IsAny<PaginationRequest>()))
                                .ReturnsAsync(paginatedResponse);

            // Act
            var result = await ActivityHandlers.GetActivitiesByDateRange(_cosmosRepositoryMock.Object, _translator, sameDate, sameDate);

            // Assert
            result.Result.Should().BeOfType<Ok<PaginationResponse<ActivityDocument>>>();
            _cosmosRepositoryMock.Verify(x => x.GetActivitiesByDateRange(sameDate, sameDate, It.IsAny<PaginationRequest>()), Times.Once);
        }

        [Theory]
        [InlineData("2023-02-29")] // Invalid leap year date
        [InlineData("2023-13-01")] // Invalid month
        [InlineData("2023-01-32")] // Invalid day
        public async Task GetActivitiesByDateRange_ShouldReturnBadRequest_WhenDateFormatIsValidButDateIsInvalid(string invalidDate)
        {
            // Arrange
            var validDate = "2023-01-01";

            // Act - Test with invalid start date
            var result1 = await ActivityHandlers.GetActivitiesByDateRange(_cosmosRepositoryMock.Object, _translator, invalidDate, validDate);
            
            // Act - Test with invalid end date
            var result2 = await ActivityHandlers.GetActivitiesByDateRange(_cosmosRepositoryMock.Object, _translator, validDate, invalidDate);

            // Assert
            result1.Result.Should().BeOfType<BadRequest>();
            result2.Result.Should().BeOfType<BadRequest>();
            _cosmosRepositoryMock.Verify(x => x.GetActivitiesByDateRange(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<PaginationRequest>()), Times.Never);
        }
    }
}
