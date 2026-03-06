using System.Net;
using System.Text.Json;
using Biotrackr.UI.Models;
using Biotrackr.UI.Models.Activity;
using Biotrackr.UI.Models.Food;
using Biotrackr.UI.Models.Sleep;
using Biotrackr.UI.Models.Weight;
using Biotrackr.UI.Services;
using Biotrackr.UI.UnitTests.Helpers;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;

namespace Biotrackr.UI.UnitTests.Services
{
    public class BiotrackrApiServiceShould
    {
        private readonly Mock<ILogger<BiotrackrApiService>> _loggerMock;

        public BiotrackrApiServiceShould()
        {
            _loggerMock = new Mock<ILogger<BiotrackrApiService>>();
        }

        private BiotrackrApiService CreateSut(HttpResponseMessage response)
        {
            var handler = new MockHttpMessageHandler(response);
            var httpClient = new HttpClient(handler) { BaseAddress = new Uri("https://test.api.com") };
            return new BiotrackrApiService(httpClient, _loggerMock.Object);
        }

        private BiotrackrApiService CreateSut(Exception exception)
        {
            var handler = new MockHttpMessageHandler(exception);
            var httpClient = new HttpClient(handler) { BaseAddress = new Uri("https://test.api.com") };
            return new BiotrackrApiService(httpClient, _loggerMock.Object);
        }

        private static HttpResponseMessage CreateSuccessResponse<T>(T data) => new(HttpStatusCode.OK)
        {
            Content = new StringContent(JsonSerializer.Serialize(data))
        };

        private static HttpResponseMessage CreateNotFoundResponse() => new(HttpStatusCode.NotFound);

        // Activity Tests
        [Fact]
        public async Task GetActivitiesAsync_ShouldReturnPaginatedResponse_WhenApiReturnsData()
        {
            var expected = new PaginatedResponse<ActivityItem>
            {
                Items = [new ActivityItem { Date = "2025-01-15" }],
                TotalCount = 1,
                PageNumber = 1,
                PageSize = 20
            };
            var sut = CreateSut(CreateSuccessResponse(expected));

            var result = await sut.GetActivitiesAsync();

            result.Items.Should().HaveCount(1);
            result.Items[0].Date.Should().Be("2025-01-15");
        }

        [Fact]
        public async Task GetActivityByDateAsync_ShouldReturnItem_WhenDateExists()
        {
            var expected = new ActivityItem { Date = "2025-01-15" };
            var sut = CreateSut(CreateSuccessResponse(expected));

            var result = await sut.GetActivityByDateAsync("2025-01-15");

            result.Should().NotBeNull();
            result!.Date.Should().Be("2025-01-15");
        }

        [Fact]
        public async Task GetActivityByDateAsync_ShouldReturnNull_WhenNotFound()
        {
            var sut = CreateSut(CreateNotFoundResponse());

            var result = await sut.GetActivityByDateAsync("2025-01-15");

            result.Should().BeNull();
        }

        [Fact]
        public async Task GetActivitiesByDateRangeAsync_ShouldReturnPaginatedResponse_WhenApiReturnsData()
        {
            var expected = new PaginatedResponse<ActivityItem>
            {
                Items = [new ActivityItem { Date = "2025-01-15" }, new ActivityItem { Date = "2025-01-16" }],
                TotalCount = 2,
                PageNumber = 1,
                PageSize = 20
            };
            var sut = CreateSut(CreateSuccessResponse(expected));

            var result = await sut.GetActivitiesByDateRangeAsync("2025-01-15", "2025-01-16");

            result.Items.Should().HaveCount(2);
        }

        // Food Tests
        [Fact]
        public async Task GetFoodLogsAsync_ShouldReturnPaginatedResponse_WhenApiReturnsData()
        {
            var expected = new PaginatedResponse<FoodItem>
            {
                Items = [new FoodItem { Date = "2025-01-15" }],
                TotalCount = 1,
                PageNumber = 1,
                PageSize = 20
            };
            var sut = CreateSut(CreateSuccessResponse(expected));

            var result = await sut.GetFoodLogsAsync();

            result.Items.Should().HaveCount(1);
        }

        [Fact]
        public async Task GetFoodLogByDateAsync_ShouldReturnItem_WhenDateExists()
        {
            var expected = new FoodItem { Date = "2025-01-15" };
            var sut = CreateSut(CreateSuccessResponse(expected));

            var result = await sut.GetFoodLogByDateAsync("2025-01-15");

            result.Should().NotBeNull();
            result!.Date.Should().Be("2025-01-15");
        }

        [Fact]
        public async Task GetFoodLogByDateAsync_ShouldReturnNull_WhenNotFound()
        {
            var sut = CreateSut(CreateNotFoundResponse());

            var result = await sut.GetFoodLogByDateAsync("2025-01-15");

            result.Should().BeNull();
        }

        [Fact]
        public async Task GetFoodLogsByDateRangeAsync_ShouldReturnPaginatedResponse_WhenApiReturnsData()
        {
            var expected = new PaginatedResponse<FoodItem>
            {
                Items = [new FoodItem { Date = "2025-01-15" }],
                TotalCount = 1,
                PageNumber = 1,
                PageSize = 20
            };
            var sut = CreateSut(CreateSuccessResponse(expected));

            var result = await sut.GetFoodLogsByDateRangeAsync("2025-01-15", "2025-01-16");

            result.Items.Should().HaveCount(1);
        }

        // Sleep Tests
        [Fact]
        public async Task GetSleepRecordsAsync_ShouldReturnPaginatedResponse_WhenApiReturnsData()
        {
            var expected = new PaginatedResponse<SleepItem>
            {
                Items = [new SleepItem { Date = "2025-01-15" }],
                TotalCount = 1,
                PageNumber = 1,
                PageSize = 20
            };
            var sut = CreateSut(CreateSuccessResponse(expected));

            var result = await sut.GetSleepRecordsAsync();

            result.Items.Should().HaveCount(1);
        }

        [Fact]
        public async Task GetSleepByDateAsync_ShouldReturnItem_WhenDateExists()
        {
            var expected = new SleepItem { Date = "2025-01-15" };
            var sut = CreateSut(CreateSuccessResponse(expected));

            var result = await sut.GetSleepByDateAsync("2025-01-15");

            result.Should().NotBeNull();
            result!.Date.Should().Be("2025-01-15");
        }

        [Fact]
        public async Task GetSleepByDateAsync_ShouldReturnNull_WhenNotFound()
        {
            var sut = CreateSut(CreateNotFoundResponse());

            var result = await sut.GetSleepByDateAsync("2025-01-15");

            result.Should().BeNull();
        }

        [Fact]
        public async Task GetSleepByDateRangeAsync_ShouldReturnPaginatedResponse_WhenApiReturnsData()
        {
            var expected = new PaginatedResponse<SleepItem>
            {
                Items = [new SleepItem { Date = "2025-01-15" }],
                TotalCount = 1,
                PageNumber = 1,
                PageSize = 20
            };
            var sut = CreateSut(CreateSuccessResponse(expected));

            var result = await sut.GetSleepByDateRangeAsync("2025-01-15", "2025-01-16");

            result.Items.Should().HaveCount(1);
        }

        // Weight Tests
        [Fact]
        public async Task GetWeightRecordsAsync_ShouldReturnPaginatedResponse_WhenApiReturnsData()
        {
            var expected = new PaginatedResponse<WeightItem>
            {
                Items = [new WeightItem { Date = "2025-01-15" }],
                TotalCount = 1,
                PageNumber = 1,
                PageSize = 20
            };
            var sut = CreateSut(CreateSuccessResponse(expected));

            var result = await sut.GetWeightRecordsAsync();

            result.Items.Should().HaveCount(1);
        }

        [Fact]
        public async Task GetWeightByDateAsync_ShouldReturnItem_WhenDateExists()
        {
            var expected = new WeightItem { Date = "2025-01-15" };
            var sut = CreateSut(CreateSuccessResponse(expected));

            var result = await sut.GetWeightByDateAsync("2025-01-15");

            result.Should().NotBeNull();
            result!.Date.Should().Be("2025-01-15");
        }

        [Fact]
        public async Task GetWeightByDateAsync_ShouldReturnNull_WhenNotFound()
        {
            var sut = CreateSut(CreateNotFoundResponse());

            var result = await sut.GetWeightByDateAsync("2025-01-15");

            result.Should().BeNull();
        }

        [Fact]
        public async Task GetWeightByDateRangeAsync_ShouldReturnPaginatedResponse_WhenApiReturnsData()
        {
            var expected = new PaginatedResponse<WeightItem>
            {
                Items = [new WeightItem { Date = "2025-01-15" }],
                TotalCount = 1,
                PageNumber = 1,
                PageSize = 20
            };
            var sut = CreateSut(CreateSuccessResponse(expected));

            var result = await sut.GetWeightByDateRangeAsync("2025-01-15", "2025-01-16");

            result.Items.Should().HaveCount(1);
        }

        // Error handling tests
        [Fact]
        public async Task GetActivitiesAsync_ShouldReturnEmptyResponse_WhenNetworkError()
        {
            var sut = CreateSut(new HttpRequestException("Connection refused"));

            var result = await sut.GetActivitiesAsync();

            result.Items.Should().BeEmpty();
        }

        [Fact]
        public async Task GetActivityByDateAsync_ShouldReturnNull_WhenTimeout()
        {
            var sut = CreateSut(new TaskCanceledException("Request timed out"));

            var result = await sut.GetActivityByDateAsync("2025-01-15");

            result.Should().BeNull();
        }

        [Fact]
        public async Task GetActivitiesAsync_ShouldReturnEmptyResponse_WhenInvalidJson()
        {
            var response = new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("not valid json")
            };
            var sut = CreateSut(response);

            var result = await sut.GetActivitiesAsync();

            result.Items.Should().BeEmpty();
        }

        // Pagination parameter tests
        [Fact]
        public async Task GetActivitiesAsync_ShouldClampPageSize_WhenExceedsMax()
        {
            var expected = new PaginatedResponse<ActivityItem> { Items = [], TotalCount = 0 };
            var handler = new MockHttpMessageHandler(CreateSuccessResponse(expected));
            var httpClient = new HttpClient(handler) { BaseAddress = new Uri("https://test.api.com") };
            var sut = new BiotrackrApiService(httpClient, _loggerMock.Object);

            await sut.GetActivitiesAsync(pageNumber: 1, pageSize: 200);

            handler.LastRequest!.RequestUri!.ToString().Should().Contain("pageSize=100");
        }

        [Fact]
        public async Task GetActivitiesAsync_ShouldDefaultPageNumber_WhenLessThanOne()
        {
            var expected = new PaginatedResponse<ActivityItem> { Items = [], TotalCount = 0 };
            var handler = new MockHttpMessageHandler(CreateSuccessResponse(expected));
            var httpClient = new HttpClient(handler) { BaseAddress = new Uri("https://test.api.com") };
            var sut = new BiotrackrApiService(httpClient, _loggerMock.Object);

            await sut.GetActivitiesAsync(pageNumber: 0, pageSize: 20);

            handler.LastRequest!.RequestUri!.ToString().Should().Contain("pageNumber=1");
        }

        // Constructor validation tests
        [Fact]
        public void Constructor_ShouldThrow_WhenHttpClientIsNull()
        {
            var act = () => new BiotrackrApiService(null!, _loggerMock.Object);

            act.Should().Throw<ArgumentNullException>().WithParameterName("httpClient");
        }

        [Fact]
        public void Constructor_ShouldThrow_WhenLoggerIsNull()
        {
            var httpClient = new HttpClient { BaseAddress = new Uri("https://test.api.com") };

            var act = () => new BiotrackrApiService(httpClient, null!);

            act.Should().Throw<ArgumentNullException>().WithParameterName("logger");
        }
    }
}
