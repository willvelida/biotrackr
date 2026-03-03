using System.Net;
using System.Text.Json;
using Biotrackr.Mcp.Server.Models;
using Biotrackr.Mcp.Server.Models.Activity;
using Biotrackr.Mcp.Server.Tools;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;

namespace Biotrackr.Mcp.Server.UnitTests.Tools
{
    public class ActivityToolsShould
    {
        private readonly Mock<ILogger<ActivityTools>> _loggerMock;

        public ActivityToolsShould()
        {
            _loggerMock = new Mock<ILogger<ActivityTools>>();
        }

        private ActivityTools CreateSut(HttpResponseMessage response)
        {
            var handler = new MockHttpMessageHandler(response);
            var httpClient = new HttpClient(handler) { BaseAddress = new Uri("https://test.api.com") };
            return new ActivityTools(httpClient, _loggerMock.Object);
        }

        private ActivityTools CreateSut(Exception exception)
        {
            var handler = new MockHttpMessageHandler(exception);
            var httpClient = new HttpClient(handler) { BaseAddress = new Uri("https://test.api.com") };
            return new ActivityTools(httpClient, _loggerMock.Object);
        }

        private static HttpResponseMessage CreateSuccessResponse<T>(T data)
        {
            var json = JsonSerializer.Serialize(data);
            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(json, System.Text.Encoding.UTF8, "application/json")
            };
        }

        #region GetActivityByDate Tests

        [Fact]
        public async Task GetActivityByDate_ShouldReturnData_WhenDateIsValid()
        {
            // Arrange
            var activityItem = new ActivityItem { Date = "2025-01-15" };
            var sut = CreateSut(CreateSuccessResponse(activityItem));

            // Act
            var result = await sut.GetActivityByDate("2025-01-15");

            // Assert
            var parsed = JsonSerializer.Deserialize<JsonElement>(result);
            parsed.GetProperty("date").GetString().Should().Be("2025-01-15");
        }

        [Theory]
        [InlineData("invalid")]
        [InlineData("01-15-2025")]
        [InlineData("2025/01/15")]
        [InlineData("")]
        public async Task GetActivityByDate_ShouldReturnError_WhenDateIsInvalid(string date)
        {
            // Arrange
            var sut = CreateSut(new HttpResponseMessage(HttpStatusCode.OK));

            // Act
            var result = await sut.GetActivityByDate(date);

            // Assert
            var error = JsonSerializer.Deserialize<JsonElement>(result);
            error.GetProperty("error").GetString().Should().Contain("Invalid date format");
        }

        [Fact]
        public async Task GetActivityByDate_ShouldReturnError_WhenApiReturns404()
        {
            // Arrange
            var sut = CreateSut(new HttpResponseMessage(HttpStatusCode.NotFound));

            // Act
            var result = await sut.GetActivityByDate("2025-01-15");

            // Assert
            var error = JsonSerializer.Deserialize<JsonElement>(result);
            error.GetProperty("error").GetString().Should().Contain("NotFound");
        }

        [Fact]
        public async Task GetActivityByDate_ShouldReturnError_WhenNetworkFails()
        {
            // Arrange
            var sut = CreateSut(new HttpRequestException("Connection refused"));

            // Act
            var result = await sut.GetActivityByDate("2025-01-15");

            // Assert
            var error = JsonSerializer.Deserialize<JsonElement>(result);
            error.GetProperty("error").GetString().Should().Contain("Network error");
        }

        #endregion

        #region GetActivityByDateRange Tests

        [Fact]
        public async Task GetActivityByDateRange_ShouldReturnData_WhenDatesAreValid()
        {
            // Arrange
            var paginatedResponse = new PaginatedResponse<ActivityItem>
            {
                Items = [new ActivityItem { Date = "2025-01-15" }],
                TotalCount = 1,
                PageNumber = 1,
                PageSize = 20
            };
            var sut = CreateSut(CreateSuccessResponse(paginatedResponse));

            // Act
            var result = await sut.GetActivityByDateRange("2025-01-01", "2025-01-31");

            // Assert
            var parsed = JsonSerializer.Deserialize<JsonElement>(result);
            parsed.GetProperty("items").GetArrayLength().Should().Be(1);
        }

        [Fact]
        public async Task GetActivityByDateRange_ShouldReturnError_WhenStartDateIsInvalid()
        {
            // Arrange
            var sut = CreateSut(new HttpResponseMessage(HttpStatusCode.OK));

            // Act
            var result = await sut.GetActivityByDateRange("invalid", "2025-01-31");

            // Assert
            var error = JsonSerializer.Deserialize<JsonElement>(result);
            error.GetProperty("error").GetString().Should().Contain("Invalid startDate format");
        }

        [Fact]
        public async Task GetActivityByDateRange_ShouldReturnError_WhenEndDateIsInvalid()
        {
            // Arrange
            var sut = CreateSut(new HttpResponseMessage(HttpStatusCode.OK));

            // Act
            var result = await sut.GetActivityByDateRange("2025-01-01", "invalid");

            // Assert
            var error = JsonSerializer.Deserialize<JsonElement>(result);
            error.GetProperty("error").GetString().Should().Contain("Invalid endDate format");
        }

        [Fact]
        public async Task GetActivityByDateRange_ShouldReturnError_WhenStartDateIsAfterEndDate()
        {
            // Arrange
            var sut = CreateSut(new HttpResponseMessage(HttpStatusCode.OK));

            // Act
            var result = await sut.GetActivityByDateRange("2025-02-01", "2025-01-01");

            // Assert
            var error = JsonSerializer.Deserialize<JsonElement>(result);
            error.GetProperty("error").GetString().Should().Contain("startDate must be on or before endDate");
        }

        [Fact]
        public async Task GetActivityByDateRange_ShouldAcceptCustomPagination()
        {
            // Arrange
            var paginatedResponse = new PaginatedResponse<ActivityItem>
            {
                Items = [],
                TotalCount = 0,
                PageNumber = 3,
                PageSize = 50
            };
            var sut = CreateSut(CreateSuccessResponse(paginatedResponse));

            // Act
            var result = await sut.GetActivityByDateRange("2025-01-01", "2025-01-31", 3, 50);

            // Assert
            var parsed = JsonSerializer.Deserialize<JsonElement>(result);
            parsed.GetProperty("pageNumber").GetInt32().Should().Be(3);
            parsed.GetProperty("pageSize").GetInt32().Should().Be(50);
        }

        [Fact]
        public async Task GetActivityByDateRange_ShouldReturnError_WhenApiReturns500()
        {
            // Arrange
            var sut = CreateSut(new HttpResponseMessage(HttpStatusCode.InternalServerError));

            // Act
            var result = await sut.GetActivityByDateRange("2025-01-01", "2025-01-31");

            // Assert
            var error = JsonSerializer.Deserialize<JsonElement>(result);
            error.GetProperty("error").GetString().Should().Contain("InternalServerError");
        }

        #endregion

        #region GetActivityRecords Tests

        [Fact]
        public async Task GetActivityRecords_ShouldReturnData_WithDefaultPagination()
        {
            // Arrange
            var paginatedResponse = new PaginatedResponse<ActivityItem>
            {
                Items = [new ActivityItem { Date = "2025-01-15" }],
                TotalCount = 1,
                PageNumber = 1,
                PageSize = 20
            };
            var sut = CreateSut(CreateSuccessResponse(paginatedResponse));

            // Act
            var result = await sut.GetActivityRecords();

            // Assert
            var parsed = JsonSerializer.Deserialize<JsonElement>(result);
            parsed.GetProperty("items").GetArrayLength().Should().Be(1);
            parsed.GetProperty("totalCount").GetInt32().Should().Be(1);
        }

        [Fact]
        public async Task GetActivityRecords_ShouldReturnData_WithCustomPagination()
        {
            // Arrange
            var paginatedResponse = new PaginatedResponse<ActivityItem>
            {
                Items = [],
                TotalCount = 100,
                PageNumber = 5,
                PageSize = 10
            };
            var sut = CreateSut(CreateSuccessResponse(paginatedResponse));

            // Act
            var result = await sut.GetActivityRecords(5, 10);

            // Assert
            var parsed = JsonSerializer.Deserialize<JsonElement>(result);
            parsed.GetProperty("pageNumber").GetInt32().Should().Be(5);
            parsed.GetProperty("pageSize").GetInt32().Should().Be(10);
        }

        [Fact]
        public async Task GetActivityRecords_ShouldReturnError_WhenApiReturnsUnauthorized()
        {
            // Arrange
            var sut = CreateSut(new HttpResponseMessage(HttpStatusCode.Unauthorized));

            // Act
            var result = await sut.GetActivityRecords();

            // Assert
            var error = JsonSerializer.Deserialize<JsonElement>(result);
            error.GetProperty("error").GetString().Should().Contain("Unauthorized");
        }

        #endregion
    }
}
