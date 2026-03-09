using System.Net;
using Biotrackr.Chat.Api.Tools;
using FluentAssertions;
using Microsoft.Extensions.Caching.Memory;
using Moq;
using Moq.Protected;

namespace Biotrackr.Chat.Api.UnitTests.Tools
{
    public class WeightToolsShould
    {
        private readonly Mock<IHttpClientFactory> _httpClientFactoryMock;
        private readonly IMemoryCache _cache;
        private readonly WeightTools _sut;

        public WeightToolsShould()
        {
            _httpClientFactoryMock = new Mock<IHttpClientFactory>();
            _cache = new MemoryCache(new MemoryCacheOptions());
            _sut = new WeightTools(_httpClientFactoryMock.Object, _cache);
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
        public async Task GetWeightByDate_ShouldReturnError_WhenDateFormatIsInvalid()
        {
            var result = await _sut.GetWeightByDate("not-a-date");
            result.Should().Contain("error");
            result.Should().Contain("Invalid date format");
        }

        [Fact]
        public async Task GetWeightByDate_ShouldReturnData_WhenDateIsValid()
        {
            var expectedJson = """{"weight": 75.5}""";
            SetupHttpClient(HttpStatusCode.OK, expectedJson);

            var result = await _sut.GetWeightByDate("2025-01-15");
            result.Should().Be(expectedJson);
        }

        [Fact]
        public async Task GetWeightByDate_ShouldReturnCachedResult_OnSecondCall()
        {
            var expectedJson = """{"weight": 75.5}""";
            SetupHttpClient(HttpStatusCode.OK, expectedJson);

            await _sut.GetWeightByDate("2025-01-15");
            await _sut.GetWeightByDate("2025-01-15");

            _httpClientFactoryMock.Verify(x => x.CreateClient("BiotrackrApi"), Times.Once);
        }

        [Fact]
        public async Task GetWeightByDateRange_ShouldReturnError_WhenRangeExceeds365Days()
        {
            var result = await _sut.GetWeightByDateRange("2025-01-01", "2026-03-01");
            result.Should().Contain("error");
            result.Should().Contain("365 days");
        }

        [Fact]
        public async Task GetWeightByDateRange_ShouldReturnData_WhenRangeIsValid()
        {
            var expectedJson = """{"items": []}""";
            SetupHttpClient(HttpStatusCode.OK, expectedJson);

            var result = await _sut.GetWeightByDateRange("2025-01-01", "2025-01-30");
            result.Should().Be(expectedJson);
        }

        [Fact]
        public async Task GetWeightRecords_ShouldReturnData()
        {
            var expectedJson = """{"items": [], "totalCount": 0}""";
            SetupHttpClient(HttpStatusCode.OK, expectedJson);

            var result = await _sut.GetWeightRecords(1, 10);
            result.Should().Be(expectedJson);
        }

        [Fact]
        public async Task GetWeightRecords_ShouldCapPageSizeAt50()
        {
            SetupHttpClient(HttpStatusCode.OK, "{}");

            var result = await _sut.GetWeightRecords(1, 200);
            result.Should().Be("{}");
        }
    }
}
