using AutoFixture;
using Biotrackr.Activity.Api.EndpointHandlers;
using Biotrackr.Activity.Api.Models;
using Biotrackr.Activity.Api.Repositories.Interfaces;
using FluentAssertions;
using Microsoft.AspNetCore.Http.HttpResults;
using Moq;

namespace Biotrackr.Activity.Api.UnitTests.EndpointHandlerTests
{
    public class ActivityHandlersShould
    {
        private readonly Mock<ICosmosRepository> _cosmosRepositoryMock;

        public ActivityHandlersShould()
        {
            _cosmosRepositoryMock = new Mock<ICosmosRepository>();
        }

        [Fact]
        public async Task GetActivityByDate_ShouldReturnOk_WhenActivityIsFound()
        {
            // Arrange
            var date = "2022-01-01";
            var fixture = new Fixture();
            var activityDocument = fixture.Create<ActivityDocument>();
            activityDocument.Date = date;

            _cosmosRepositoryMock.Setup(x => x.GetActivitySummaryByDate(date)).ReturnsAsync(activityDocument);

            // Act
            var result = await ActivityHandlers.GetActivityByDate(_cosmosRepositoryMock.Object, date);

            // Assert
            result.Result.Should().BeOfType<Ok<ActivityDocument>>();
        }

        [Fact]
        public async Task GetActivityByDate_ShouldReturnNotFound_WhenActivityIsNotFound()
        {
            // Arrange
            var date = "2022-01-01";
            _cosmosRepositoryMock.Setup(x => x.GetActivitySummaryByDate(date))
                .ReturnsAsync((ActivityDocument)null);

            // Act
            var result = await ActivityHandlers.GetActivityByDate(_cosmosRepositoryMock.Object, date);

            // Assert
            result.Result.Should().BeOfType<NotFound>();
        }

        [Fact]
        public async Task GetAllActivities_ShouldReturnPaginatedResult_WhenPaginationParametersProvided()
        {
            // Arrange
            var fixture = new Fixture();
            var activityDocuments = fixture.CreateMany<ActivityDocument>(10).ToList();
            var paginatedResponse = new PaginationResponse<ActivityDocument>
            {
                Items = activityDocuments,
                PageNumber = 2,
                PageSize = 10,
                TotalCount = 50
            };

            _cosmosRepositoryMock.Setup(x => x.GetAllActivitySummaries(It.IsAny<PaginationRequest>()))
                                .ReturnsAsync(paginatedResponse);

            // Act
            var result = await ActivityHandlers.GetAllActivities(_cosmosRepositoryMock.Object, 2, 10);

            // Assert
            result.Should().BeOfType<Ok<PaginationResponse<ActivityDocument>>>();
            var okResult = result as Ok<PaginationResponse<ActivityDocument>>;
            okResult.Value.Should().BeEquivalentTo(paginatedResponse);
        }

        [Fact]
        public async Task GetAllActivities_ShouldUseDefaultPageSize_WhenOnlyPageNumberProvided()
        {
            // Arrange
            var fixture = new Fixture();
            var paginatedResponse = new PaginationResponse<ActivityDocument>
            {
                Items = fixture.CreateMany<ActivityDocument>(20).ToList(),
                PageNumber = 2,
                PageSize = 20,
                TotalCount = 100
            };

            _cosmosRepositoryMock.Setup(x => x.GetAllActivitySummaries(
                It.Is<PaginationRequest>(r => r.PageNumber == 2 && r.PageSize == 20)))
                                .ReturnsAsync(paginatedResponse);

            // Act
            var result = await ActivityHandlers.GetAllActivities(_cosmosRepositoryMock.Object, 2, null);

            // Assert
            result.Should().BeOfType<Ok<PaginationResponse<ActivityDocument>>>();
            _cosmosRepositoryMock.Verify(x => x.GetAllActivitySummaries(
                It.Is<PaginationRequest>(r => r.PageNumber == 2 && r.PageSize == 20)), Times.Once);
        }

        [Fact]
        public async Task GetAllActivities_ShouldUseDefaultPageNumber_WhenOnlyPageSizeProvided()
        {
            // Arrange
            var fixture = new Fixture();
            var paginatedResponse = new PaginationResponse<ActivityDocument>
            {
                Items = fixture.CreateMany<ActivityDocument>(50).ToList(),
                PageNumber = 1,
                PageSize = 50,
                TotalCount = 100
            };

            _cosmosRepositoryMock.Setup(x => x.GetAllActivitySummaries(
                It.Is<PaginationRequest>(r => r.PageNumber == 1 && r.PageSize == 50)))
                                .ReturnsAsync(paginatedResponse);

            // Act
            var result = await ActivityHandlers.GetAllActivities(_cosmosRepositoryMock.Object, null, 50);

            // Assert
            result.Should().BeOfType<Ok<PaginationResponse<ActivityDocument>>>();
            _cosmosRepositoryMock.Verify(x => x.GetAllActivitySummaries(
                It.Is<PaginationRequest>(r => r.PageNumber == 1 && r.PageSize == 50)), Times.Once);
        }
    }
}
