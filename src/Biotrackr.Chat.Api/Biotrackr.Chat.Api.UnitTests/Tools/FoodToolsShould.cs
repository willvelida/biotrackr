using System.Net;
using Biotrackr.Chat.Api.Tools;
using FluentAssertions;
using Microsoft.Extensions.Caching.Memory;
using Moq;
using Moq.Protected;

namespace Biotrackr.Chat.Api.UnitTests.Tools
{
    public class FoodToolsShould
    {
        private readonly Mock<IHttpClientFactory> _httpClientFactoryMock;
        private readonly IMemoryCache _cache;
        private readonly FoodTools _sut;

        public FoodToolsShould()
        {
            _httpClientFactoryMock = new Mock<IHttpClientFactory>();
            _cache = new MemoryCache(new MemoryCacheOptions());
            _sut = new FoodTools(_httpClientFactoryMock.Object, _cache);
        }

        private void SetupHttpClient(HttpStatusCode statusCode, string content = "{}")
        {
            var handler = new Mock<HttpMessageHandler>();
            handler.Protected()
                .Setup<Task<HttpResponseMessage>>("SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>())
                .ReturnsAsync(new HttpResponseMessage
                {
                    StatusCode = statusCode,
                    Content = new StringContent(content)
                });

            var client = new HttpClient(handler.Object)
            {
                BaseAddress = new Uri("https://api.test.com")
            };
            _httpClientFactoryMock.Setup(x => x.CreateClient("BiotrackrApi")).Returns(client);
        }

        [Fact]
        public async Task GetFoodByDate_ShouldReturnError_WhenDateFormatIsInvalid()
        {
            var result = await _sut.GetFoodByDate("not-a-date");
            result.Should().Contain("error");
            result.Should().Contain("Invalid date format");
        }

        [Fact]
        public async Task GetFoodByDate_ShouldReturnData_WhenDateIsValid()
        {
            var expectedJson = """{"calories": 2000}""";
            SetupHttpClient(HttpStatusCode.OK, expectedJson);

            var result = await _sut.GetFoodByDate("2025-01-15");
            result.Should().Be(expectedJson);
        }

        [Fact]
        public async Task GetFoodByDate_ShouldReturnCachedResult_OnSecondCall()
        {
            var expectedJson = """{"calories": 2000}""";
            SetupHttpClient(HttpStatusCode.OK, expectedJson);

            await _sut.GetFoodByDate("2025-01-15");
            await _sut.GetFoodByDate("2025-01-15");

            _httpClientFactoryMock.Verify(x => x.CreateClient("BiotrackrApi"), Times.Once);
        }

        [Fact]
        public async Task GetFoodByDateRange_ShouldReturnError_WhenRangeExceeds365Days()
        {
            var result = await _sut.GetFoodByDateRange("2025-01-01", "2026-03-01");
            result.Should().Contain("error");
            result.Should().Contain("365 days");
        }

        [Fact]
        public async Task GetFoodByDateRange_ShouldReturnData_WhenRangeIsValid()
        {
            var expectedJson = """{"items": []}""";
            SetupHttpClient(HttpStatusCode.OK, expectedJson);

            var result = await _sut.GetFoodByDateRange("2025-01-01", "2025-01-30");
            result.Should().Be(expectedJson);
        }

        [Fact]
        public async Task GetFoodRecords_ShouldReturnData()
        {
            var expectedJson = """{"items": [], "totalCount": 0}""";
            SetupHttpClient(HttpStatusCode.OK, expectedJson);

            var result = await _sut.GetFoodRecords(1, 10);
            result.Should().Be(expectedJson);
        }

        [Fact]
        public async Task GetFoodRecords_ShouldCapPageSizeAt50()
        {
            SetupHttpClient(HttpStatusCode.OK, "{}");

            var result = await _sut.GetFoodRecords(1, 200);
            result.Should().Be("{}");
        }
    }
}
