using System.Net;
using Biotrackr.Chat.Api.Tools;
using FluentAssertions;
using Microsoft.Extensions.Caching.Memory;
using Moq;
using Moq.Protected;

namespace Biotrackr.Chat.Api.UnitTests.Tools
{
    public class ActivityToolsShould
    {
        private readonly Mock<IHttpClientFactory> _httpClientFactoryMock;
        private readonly IMemoryCache _cache;
        private readonly ActivityTools _sut;

        public ActivityToolsShould()
        {
            _httpClientFactoryMock = new Mock<IHttpClientFactory>();
            _cache = new MemoryCache(new MemoryCacheOptions());
            _sut = new ActivityTools(_httpClientFactoryMock.Object, _cache);
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
        public async Task GetActivityByDate_ShouldReturnError_WhenDateFormatIsInvalid()
        {
            // Act
            var result = await _sut.GetActivityByDate("not-a-date");

            // Assert
            result.Should().Contain("error");
            result.Should().Contain("Invalid date format");
        }

        [Fact]
        public async Task GetActivityByDate_ShouldReturnData_WhenDateIsValid()
        {
            // Arrange
            var expectedJson = """{"steps": 10000}""";
            SetupHttpClient(HttpStatusCode.OK, expectedJson);

            // Act
            var result = await _sut.GetActivityByDate("2025-01-15");

            // Assert
            result.Should().Be(expectedJson);
        }

        [Fact]
        public async Task GetActivityByDate_ShouldReturnError_WhenApiReturnsNotFound()
        {
            // Arrange
            SetupHttpClient(HttpStatusCode.NotFound);

            // Act
            var result = await _sut.GetActivityByDate("2025-01-15");

            // Assert
            result.Should().Contain("error");
            result.Should().Contain("not found");
        }

        [Fact]
        public async Task GetActivityByDate_ShouldReturnCachedResult_OnSecondCall()
        {
            // Arrange
            var expectedJson = """{"steps": 10000}""";
            SetupHttpClient(HttpStatusCode.OK, expectedJson);

            // Act
            var result1 = await _sut.GetActivityByDate("2025-01-15");
            var result2 = await _sut.GetActivityByDate("2025-01-15");

            // Assert
            result1.Should().Be(expectedJson);
            result2.Should().Be(expectedJson);
            _httpClientFactoryMock.Verify(x => x.CreateClient("BiotrackrApi"), Times.Once);
        }

        [Fact]
        public async Task GetActivityByDateRange_ShouldReturnError_WhenStartDateIsInvalid()
        {
            // Act
            var result = await _sut.GetActivityByDateRange("bad-date", "2025-01-15");

            // Assert
            result.Should().Contain("error");
            result.Should().Contain("Invalid date format");
        }

        [Fact]
        public async Task GetActivityByDateRange_ShouldReturnError_WhenRangeExceeds365Days()
        {
            // Act
            var result = await _sut.GetActivityByDateRange("2025-01-01", "2026-03-01");

            // Assert
            result.Should().Contain("error");
            result.Should().Contain("365 days");
        }

        [Fact]
        public async Task GetActivityByDateRange_ShouldReturnData_WhenRangeIsValid()
        {
            // Arrange
            var expectedJson = """{"items": []}""";
            SetupHttpClient(HttpStatusCode.OK, expectedJson);

            // Act
            var result = await _sut.GetActivityByDateRange("2025-01-01", "2025-01-30");

            // Assert
            result.Should().Be(expectedJson);
        }

        [Fact]
        public async Task GetActivityRecords_ShouldReturnData()
        {
            // Arrange
            var expectedJson = """{"items": [], "totalCount": 0}""";
            SetupHttpClient(HttpStatusCode.OK, expectedJson);

            // Act
            var result = await _sut.GetActivityRecords(1, 10);

            // Assert
            result.Should().Be(expectedJson);
        }

        [Fact]
        public async Task GetActivityRecords_ShouldCapPageSizeAt50()
        {
            // Arrange
            var handler = new Mock<HttpMessageHandler>();
            handler.Protected()
                .Setup<Task<HttpResponseMessage>>("SendAsync",
                    ItExpr.Is<HttpRequestMessage>(r => r.RequestUri!.Query.Contains("pageSize=50")),
                    ItExpr.IsAny<CancellationToken>())
                .ReturnsAsync(new HttpResponseMessage
                {
                    StatusCode = HttpStatusCode.OK,
                    Content = new StringContent("{}")
                });

            var client = new HttpClient(handler.Object)
            {
                BaseAddress = new Uri("https://api.test.com")
            };
            _httpClientFactoryMock.Setup(x => x.CreateClient("BiotrackrApi")).Returns(client);

            // Act
            var result = await _sut.GetActivityRecords(1, 200);

            // Assert
            result.Should().Be("{}");
        }

        [Fact]
        public async Task GetActivityRecords_ShouldReturnError_WhenApiFails()
        {
            // Arrange
            SetupHttpClient(HttpStatusCode.InternalServerError);

            // Act
            var result = await _sut.GetActivityRecords();

            // Assert
            result.Should().Contain("error");
        }
    }
}
