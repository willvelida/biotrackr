using AutoFixture;
using Biotrackr.Food.Api.EndpointHandlers;
using Biotrackr.Food.Api.Models;
using Biotrackr.Food.Api.Repositories.Interfaces;
using Biotrackr.Food.Api.Services;
using Biotrackr.Food.Api.UnitTests.Fixtures;
using FluentAssertions;
using Microsoft.AspNetCore.Http.HttpResults;
using Moq;

namespace Biotrackr.Food.Api.UnitTests.EndpointHandlerTests
{
    public class FoodHandlersShould
    {
        private readonly Mock<ICosmosRepository> _cosmosRepositoryMock;
        private readonly FoodDocumentTranslator _translator = new();

        public FoodHandlersShould()
        {
            _cosmosRepositoryMock = new Mock<ICosmosRepository>();
        }

        [Fact]
        public async Task GetFoodLogByDate_ShouldReturnOk_WhenFoodLogIsFound()
        {
            // Arrange
            var date = "2022-01-01";
            var fixture = new Fixture();
            var foodDocument = fixture.Build<FoodStoredDocument>().Without(x => x.Google).Without(x => x.SchemaVersion).Create();
            foodDocument.Date = date;

            _cosmosRepositoryMock.Setup(x => x.GetFoodLogByDateAsync(date)).ReturnsAsync(foodDocument);

            // Act
            var result = await FoodHandlers.GetFoodLogByDate(_cosmosRepositoryMock.Object, _translator, date);

            // Assert
            result.Result.Should().BeOfType<Ok<FoodDocument>>();
        }

        [Fact]
        public async Task GetFoodLogByDate_ShouldReturnNotFound_WhenFoodLogIsNotFound()
        {
            // Arrange
            var date = "2022-01-01";
            _cosmosRepositoryMock.Setup(x => x.GetFoodLogByDateAsync(date))
                .ReturnsAsync((FoodStoredDocument?)null);

            // Act
            var result = await FoodHandlers.GetFoodLogByDate(_cosmosRepositoryMock.Object, _translator, date);

            // Assert
            result.Result.Should().BeOfType<NotFound>();
        }

        [Fact]
        public async Task GetFoodLogByDate_ShouldReturnBadRequest_WhenDateFormatIsInvalid()
        {
            // Arrange
            var invalidDate = "invalid-date-format";

            // Act
            var result = await FoodHandlers.GetFoodLogByDate(_cosmosRepositoryMock.Object, _translator, invalidDate);

            // Assert
            result.Result.Should().BeOfType<BadRequest>();
            _cosmosRepositoryMock.Verify(x => x.GetFoodLogByDateAsync(It.IsAny<string>()), Times.Never);
        }

        [Fact]
        public async Task GetAllFoodLogs_ShouldReturnPaginatedResult_WhenPaginationParametersProvided()
        {
            // Arrange
            var fixture = new Fixture();
            var foodDocuments = fixture.Build<FoodStoredDocument>().Without(x => x.Google).Without(x => x.SchemaVersion).CreateMany(10).ToList();
            var totalCount = 50;

            _cosmosRepositoryMock.Setup(x => x.GetAllFoodLogsAsync(2, 10))
                                .ReturnsAsync(foodDocuments);
            _cosmosRepositoryMock.Setup(x => x.GetTotalFoodLogsCountAsync())
                                .ReturnsAsync(totalCount);

            // Act
            var result = await FoodHandlers.GetAllFoodLogs(_cosmosRepositoryMock.Object, _translator, 2, 10);

            // Assert
            result.Should().BeOfType<Ok<PaginationResponse<FoodDocument>>>();
            var okResult = result as Ok<PaginationResponse<FoodDocument>>;
            okResult.Should().NotBeNull();
            okResult!.Value.Items.Should().HaveCount(10);
            okResult.Value.PageNumber.Should().Be(2);
            okResult.Value.PageSize.Should().Be(10);
            okResult.Value.TotalCount.Should().Be(totalCount);
        }

        [Fact]
        public async Task GetAllFoodLogs_ShouldUseDefaultPageSize_WhenOnlyPageNumberProvided()
        {
            // Arrange
            var fixture = new Fixture();
            var foodDocuments = fixture.Build<FoodStoredDocument>().Without(x => x.Google).Without(x => x.SchemaVersion).CreateMany(20).ToList();
            var totalCount = 100;

            _cosmosRepositoryMock.Setup(x => x.GetAllFoodLogsAsync(2, 20))
                                .ReturnsAsync(foodDocuments);
            _cosmosRepositoryMock.Setup(x => x.GetTotalFoodLogsCountAsync())
                                .ReturnsAsync(totalCount);

            // Act
            var result = await FoodHandlers.GetAllFoodLogs(_cosmosRepositoryMock.Object, _translator, 2, null);

            // Assert
            result.Should().BeOfType<Ok<PaginationResponse<FoodDocument>>>();
            _cosmosRepositoryMock.Verify(x => x.GetAllFoodLogsAsync(2, 20), Times.Once);
        }

        [Fact]
        public async Task GetAllFoodLogs_ShouldUseDefaultPageNumber_WhenOnlyPageSizeProvided()
        {
            // Arrange
            var fixture = new Fixture();
            var foodDocuments = fixture.Build<FoodStoredDocument>().Without(x => x.Google).Without(x => x.SchemaVersion).CreateMany(50).ToList();
            var totalCount = 100;

            _cosmosRepositoryMock.Setup(x => x.GetAllFoodLogsAsync(1, 50))
                                .ReturnsAsync(foodDocuments);
            _cosmosRepositoryMock.Setup(x => x.GetTotalFoodLogsCountAsync())
                                .ReturnsAsync(totalCount);

            // Act
            var result = await FoodHandlers.GetAllFoodLogs(_cosmosRepositoryMock.Object, _translator, null, 50);

            // Assert
            result.Should().BeOfType<Ok<PaginationResponse<FoodDocument>>>();
            _cosmosRepositoryMock.Verify(x => x.GetAllFoodLogsAsync(1, 50), Times.Once);
        }

        [Fact]
        public async Task GetFoodLogsByDateRange_ShouldReturnOk_WhenValidDateRangeProvided()
        {
            // Arrange
            var startDate = "2023-01-01";
            var endDate = "2023-01-31";
            var fixture = new Fixture();
            var foodDocuments = fixture.Build<FoodStoredDocument>().Without(x => x.Google).Without(x => x.SchemaVersion).CreateMany(5).ToList();
            var totalCount = 5;

            _cosmosRepositoryMock.Setup(x => x.GetFoodLogsByDateRangeAsync(startDate, endDate, 1, 20))
                                .ReturnsAsync(foodDocuments);
            _cosmosRepositoryMock.Setup(x => x.GetFoodLogsCountByDateRangeAsync(startDate, endDate))
                                .ReturnsAsync(totalCount);

            // Act
            var result = await FoodHandlers.GetFoodLogsByDateRange(_cosmosRepositoryMock.Object, _translator, startDate, endDate);

            // Assert
            result.Result.Should().BeOfType<Ok<PaginationResponse<FoodDocument>>>();
            var okResult = result.Result as Ok<PaginationResponse<FoodDocument>>;
            okResult.Should().NotBeNull();
            okResult!.Value.Items.Should().HaveCount(5);
            okResult.Value.TotalCount.Should().Be(totalCount);
        }

        [Fact]
        public async Task GetFoodLogsByDateRange_ShouldReturnBadRequest_WhenStartDateIsInvalid()
        {
            // Arrange
            var invalidStartDate = "invalid-date";
            var endDate = "2023-01-31";

            // Act
            var result = await FoodHandlers.GetFoodLogsByDateRange(_cosmosRepositoryMock.Object, _translator, invalidStartDate, endDate);

            // Assert
            result.Result.Should().BeOfType<BadRequest>();
            _cosmosRepositoryMock.Verify(x => x.GetFoodLogsByDateRangeAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<int>(), It.IsAny<int>()), Times.Never);
        }

        [Fact]
        public async Task GetFoodLogsByDateRange_ShouldReturnBadRequest_WhenEndDateIsInvalid()
        {
            // Arrange
            var startDate = "2023-01-01";
            var invalidEndDate = "invalid-date";

            // Act
            var result = await FoodHandlers.GetFoodLogsByDateRange(_cosmosRepositoryMock.Object, _translator, startDate, invalidEndDate);

            // Assert
            result.Result.Should().BeOfType<BadRequest>();
            _cosmosRepositoryMock.Verify(x => x.GetFoodLogsByDateRangeAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<int>(), It.IsAny<int>()), Times.Never);
        }

        [Fact]
        public async Task GetFoodLogsByDateRange_ShouldReturnBadRequest_WhenStartDateIsAfterEndDate()
        {
            // Arrange
            var startDate = "2023-01-31";
            var endDate = "2023-01-01";

            // Act
            var result = await FoodHandlers.GetFoodLogsByDateRange(_cosmosRepositoryMock.Object, _translator, startDate, endDate);

            // Assert
            result.Result.Should().BeOfType<BadRequest>();
            _cosmosRepositoryMock.Verify(x => x.GetFoodLogsByDateRangeAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<int>(), It.IsAny<int>()), Times.Never);
        }

        [Fact]
        public async Task GetFoodLogsByDateRange_ShouldUseDefaultPagination_WhenPaginationParametersNotProvided()
        {
            // Arrange
            var startDate = "2023-01-01";
            var endDate = "2023-01-31";
            var fixture = new Fixture();
            var foodDocuments = fixture.Build<FoodStoredDocument>().Without(x => x.Google).Without(x => x.SchemaVersion).CreateMany(20).ToList();
            var totalCount = 20;

            _cosmosRepositoryMock.Setup(x => x.GetFoodLogsByDateRangeAsync(startDate, endDate, 1, 20))
                                .ReturnsAsync(foodDocuments);
            _cosmosRepositoryMock.Setup(x => x.GetFoodLogsCountByDateRangeAsync(startDate, endDate))
                                .ReturnsAsync(totalCount);

            // Act
            var result = await FoodHandlers.GetFoodLogsByDateRange(_cosmosRepositoryMock.Object, _translator, startDate, endDate);

            // Assert
            result.Result.Should().BeOfType<Ok<PaginationResponse<FoodDocument>>>();
            _cosmosRepositoryMock.Verify(x => x.GetFoodLogsByDateRangeAsync(startDate, endDate, 1, 20), Times.Once);
        }

        [Fact]
        public async Task GetFoodLogsByDateRange_ShouldUseCustomPagination_WhenPaginationParametersProvided()
        {
            // Arrange
            var startDate = "2023-01-01";
            var endDate = "2023-01-31";
            var pageNumber = 3;
            var pageSize = 15;
            var fixture = new Fixture();
            var foodDocuments = fixture.Build<FoodStoredDocument>().Without(x => x.Google).Without(x => x.SchemaVersion).CreateMany(15).ToList();
            var totalCount = 45;

            _cosmosRepositoryMock.Setup(x => x.GetFoodLogsByDateRangeAsync(startDate, endDate, pageNumber, pageSize))
                                .ReturnsAsync(foodDocuments);
            _cosmosRepositoryMock.Setup(x => x.GetFoodLogsCountByDateRangeAsync(startDate, endDate))
                                .ReturnsAsync(totalCount);

            // Act
            var result = await FoodHandlers.GetFoodLogsByDateRange(_cosmosRepositoryMock.Object, _translator, startDate, endDate, pageNumber, pageSize);

            // Assert
            result.Result.Should().BeOfType<Ok<PaginationResponse<FoodDocument>>>();
            var okResult = result.Result as Ok<PaginationResponse<FoodDocument>>;
            okResult.Should().NotBeNull();
            okResult!.Value.PageNumber.Should().Be(pageNumber);
            okResult.Value.PageSize.Should().Be(pageSize);
            _cosmosRepositoryMock.Verify(x => x.GetFoodLogsByDateRangeAsync(startDate, endDate, pageNumber, pageSize), Times.Once);
        }

        [Fact]
        public async Task GetFoodLogsByDateRange_ShouldHandleSameDateRange()
        {
            // Arrange
            var sameDate = "2023-01-15";
            var fixture = new Fixture();
            var foodDocuments = fixture.Build<FoodStoredDocument>().Without(x => x.Google).Without(x => x.SchemaVersion).CreateMany(2).ToList();
            var totalCount = 2;

            _cosmosRepositoryMock.Setup(x => x.GetFoodLogsByDateRangeAsync(sameDate, sameDate, 1, 20))
                                .ReturnsAsync(foodDocuments);
            _cosmosRepositoryMock.Setup(x => x.GetFoodLogsCountByDateRangeAsync(sameDate, sameDate))
                                .ReturnsAsync(totalCount);

            // Act
            var result = await FoodHandlers.GetFoodLogsByDateRange(_cosmosRepositoryMock.Object, _translator, sameDate, sameDate);

            // Assert
            result.Result.Should().BeOfType<Ok<PaginationResponse<FoodDocument>>>();
            _cosmosRepositoryMock.Verify(x => x.GetFoodLogsByDateRangeAsync(sameDate, sameDate, 1, 20), Times.Once);
        }

        [Theory]
        [InlineData("2023-02-29")] // Invalid leap year date
        [InlineData("2023-13-01")] // Invalid month
        [InlineData("2023-01-32")] // Invalid day
        public async Task GetFoodLogsByDateRange_ShouldReturnBadRequest_WhenDateFormatIsValidButDateIsInvalid(string invalidDate)
        {
            // Arrange
            var validDate = "2023-01-01";

            // Act - Test with invalid start date
            var result1 = await FoodHandlers.GetFoodLogsByDateRange(_cosmosRepositoryMock.Object, _translator, invalidDate, validDate);
            
            // Act - Test with invalid end date
            var result2 = await FoodHandlers.GetFoodLogsByDateRange(_cosmosRepositoryMock.Object, _translator, validDate, invalidDate);

            // Assert
            result1.Result.Should().BeOfType<BadRequest>();
            result2.Result.Should().BeOfType<BadRequest>();
            _cosmosRepositoryMock.Verify(x => x.GetFoodLogsByDateRangeAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<int>(), It.IsAny<int>()), Times.Never);
        }

        private static FoodStoredDocument StoredSample(string json) =>
            Newtonsoft.Json.JsonConvert.DeserializeObject<FoodStoredDocument>(json, new Newtonsoft.Json.JsonSerializerSettings
            {
                ContractResolver = new Newtonsoft.Json.Serialization.CamelCasePropertyNamesContractResolver()
            })!;

        [Fact]
        public async Task GetFoodLogByDate_ShouldReturnTranslatedGoogleDocument_WhenStoredDocumentIsVersion2()
        {
            // Arrange
            var stored = StoredSample(FoodDocumentSamples.Version2Json);
            _cosmosRepositoryMock.Setup(x => x.GetFoodLogByDateAsync(stored.Date)).ReturnsAsync(stored);

            // Act
            var result = await FoodHandlers.GetFoodLogByDate(_cosmosRepositoryMock.Object, _translator, stored.Date);

            // Assert
            var document = result.Result.Should().BeOfType<Ok<FoodDocument>>().Subject.Value!;
            new object[] { document.Provider, document.SchemaVersion, document.Food.Foods.Count }
                .Should().Equal("Google", 2, 2);
        }

        [Fact]
        public async Task GetFoodLogsByDateRange_ShouldReturnVersion1AndVersion2Together_WhenRangeSpansCutover()
        {
            // Arrange
            var stored = new List<FoodStoredDocument>
            {
                StoredSample(FoodDocumentSamples.Version1Json),
                StoredSample(FoodDocumentSamples.Version2Json)
            };
            _cosmosRepositoryMock.Setup(x => x.GetFoodLogsByDateRangeAsync("2025-12-01", "2026-01-15", 1, 20)).ReturnsAsync(stored);
            _cosmosRepositoryMock.Setup(x => x.GetFoodLogsCountByDateRangeAsync("2025-12-01", "2026-01-15")).ReturnsAsync(2);

            // Act
            var result = await FoodHandlers.GetFoodLogsByDateRange(_cosmosRepositoryMock.Object, _translator, "2025-12-01", "2026-01-15");

            // Assert
            var page = result.Result.Should().BeOfType<Ok<PaginationResponse<FoodDocument>>>().Subject.Value!;
            page.Items.Select(d => (d.Provider, d.SchemaVersion)).Should().Equal(("Fitbit", 1), ("Google", 2));
        }

        [Fact]
        public async Task GetAllFoodLogs_ShouldTranslateEveryItem_WhenPageMixesVersions()
        {
            // Arrange
            var stored = new List<FoodStoredDocument>
            {
                StoredSample(FoodDocumentSamples.Version2Json),
                StoredSample(FoodDocumentSamples.Version1Json)
            };
            _cosmosRepositoryMock.Setup(x => x.GetAllFoodLogsAsync(1, 20)).ReturnsAsync(stored);
            _cosmosRepositoryMock.Setup(x => x.GetTotalFoodLogsCountAsync()).ReturnsAsync(2);

            // Act
            var result = await FoodHandlers.GetAllFoodLogs(_cosmosRepositoryMock.Object, _translator);

            // Assert
            result.Value!.Items.Select(d => d.Provider).Should().Equal("Google", "Fitbit");
        }
    }
}
