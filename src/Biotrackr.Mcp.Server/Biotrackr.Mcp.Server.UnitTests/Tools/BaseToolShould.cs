using System.Net;
using System.Text.Json;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;

namespace Biotrackr.Mcp.Server.UnitTests.Tools
{
    public class BaseToolShould
    {
        private readonly Mock<ILogger> _loggerMock;

        public BaseToolShould()
        {
            _loggerMock = new Mock<ILogger>();
        }

        #region IsValidDate Tests

        [Theory]
        [InlineData("2025-01-01", true)]
        [InlineData("2025-12-31", true)]
        [InlineData("2025-06-15", true)]
        [InlineData("2000-01-01", true)]
        [InlineData("01-01-2025", false)]
        [InlineData("2025/01/01", false)]
        [InlineData("2025-1-1", false)]
        [InlineData("2025-13-01", false)]
        [InlineData("2025-01-32", false)]
        [InlineData("not-a-date", false)]
        [InlineData("", false)]
        [InlineData("2025-02-29", false)]
        [InlineData("2024-02-29", true)]
        public void IsValidDate_ShouldReturnExpectedResult(string date, bool expected)
        {
            // Act
            var result = TestableBaseTool.IsValidDate(date);

            // Assert
            result.Should().Be(expected);
        }

        #endregion

        #region IsValidDateRange Tests

        [Fact]
        public void IsValidDateRange_ShouldReturnTrue_WhenStartDateIsBeforeEndDate()
        {
            // Act
            var result = TestableBaseTool.IsValidDateRange("2025-01-01", "2025-01-31");

            // Assert
            result.Should().BeTrue();
        }

        [Fact]
        public void IsValidDateRange_ShouldReturnTrue_WhenStartDateEqualsEndDate()
        {
            // Act
            var result = TestableBaseTool.IsValidDateRange("2025-01-15", "2025-01-15");

            // Assert
            result.Should().BeTrue();
        }

        [Fact]
        public void IsValidDateRange_ShouldReturnFalse_WhenStartDateIsAfterEndDate()
        {
            // Act
            var result = TestableBaseTool.IsValidDateRange("2025-02-01", "2025-01-01");

            // Assert
            result.Should().BeFalse();
        }

        [Fact]
        public void IsValidDateRange_ShouldReturnFalse_WhenStartDateIsInvalid()
        {
            // Act
            var result = TestableBaseTool.IsValidDateRange("invalid", "2025-01-01");

            // Assert
            result.Should().BeFalse();
        }

        [Fact]
        public void IsValidDateRange_ShouldReturnFalse_WhenEndDateIsInvalid()
        {
            // Act
            var result = TestableBaseTool.IsValidDateRange("2025-01-01", "invalid");

            // Assert
            result.Should().BeFalse();
        }

        #endregion

        #region BuildPaginatedEndpoint Tests

        [Fact]
        public void BuildPaginatedEndpoint_ShouldBuildCorrectEndpoint_WithDefaults()
        {
            // Act
            var result = TestableBaseTool.BuildPaginatedEndpoint("/activity", 1, 20);

            // Assert
            result.Should().Be("/activity?pageNumber=1&pageSize=20");
        }

        [Fact]
        public void BuildPaginatedEndpoint_ShouldClampPageNumberToMinimumOne()
        {
            // Act
            var result = TestableBaseTool.BuildPaginatedEndpoint("/activity", 0, 20);

            // Assert
            result.Should().Be("/activity?pageNumber=1&pageSize=20");
        }

        [Fact]
        public void BuildPaginatedEndpoint_ShouldClampNegativePageNumberToOne()
        {
            // Act
            var result = TestableBaseTool.BuildPaginatedEndpoint("/activity", -5, 20);

            // Assert
            result.Should().Be("/activity?pageNumber=1&pageSize=20");
        }

        [Fact]
        public void BuildPaginatedEndpoint_ShouldClampPageSizeToMinimumOne()
        {
            // Act
            var result = TestableBaseTool.BuildPaginatedEndpoint("/activity", 1, 0);

            // Assert
            result.Should().Be("/activity?pageNumber=1&pageSize=1");
        }

        [Fact]
        public void BuildPaginatedEndpoint_ShouldClampPageSizeToMaximumHundred()
        {
            // Act
            var result = TestableBaseTool.BuildPaginatedEndpoint("/activity", 1, 200);

            // Assert
            result.Should().Be("/activity?pageNumber=1&pageSize=100");
        }

        [Fact]
        public void BuildPaginatedEndpoint_ShouldPreserveCustomValues_WithinBounds()
        {
            // Act
            var result = TestableBaseTool.BuildPaginatedEndpoint("/food/range/2025-01-01/2025-01-31", 3, 50);

            // Assert
            result.Should().Be("/food/range/2025-01-01/2025-01-31?pageNumber=3&pageSize=50");
        }

        #endregion

        #region GetAsync Tests

        [Fact]
        public async Task GetAsync_ShouldReturnContent_WhenApiReturnsSuccessfulResponse()
        {
            // Arrange
            var expectedData = new TestModel { Name = "test", Value = 42 };
            var json = JsonSerializer.Serialize(expectedData);
            var handler = new MockHttpMessageHandler(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(json, System.Text.Encoding.UTF8, "application/json")
            });
            var httpClient = new HttpClient(handler) { BaseAddress = new Uri("https://test.api.com") };
            var sut = new TestableBaseTool(httpClient, _loggerMock.Object);

            // Act
            var result = await sut.GetAsync<TestModel>("/test", "TestOperation");

            // Assert
            result.Should().Be(json);
        }

        [Theory]
        [InlineData(HttpStatusCode.NotFound)]
        [InlineData(HttpStatusCode.InternalServerError)]
        [InlineData(HttpStatusCode.Unauthorized)]
        [InlineData(HttpStatusCode.Forbidden)]
        [InlineData(HttpStatusCode.BadRequest)]
        [InlineData(HttpStatusCode.ServiceUnavailable)]
        public async Task GetAsync_ShouldReturnError_WhenApiReturnsNonSuccessStatusCode(HttpStatusCode statusCode)
        {
            // Arrange
            var handler = new MockHttpMessageHandler(new HttpResponseMessage(statusCode));
            var httpClient = new HttpClient(handler) { BaseAddress = new Uri("https://test.api.com") };
            var sut = new TestableBaseTool(httpClient, _loggerMock.Object);

            // Act
            var result = await sut.GetAsync<TestModel>("/test", "TestOperation");

            // Assert
            var error = JsonSerializer.Deserialize<JsonElement>(result);
            error.GetProperty("error").GetString().Should().Contain(statusCode.ToString());
        }

        [Fact]
        public async Task GetAsync_ShouldReturnError_WhenNetworkErrorOccurs()
        {
            // Arrange
            var handler = new MockHttpMessageHandler(new HttpRequestException("Connection refused"));
            var httpClient = new HttpClient(handler) { BaseAddress = new Uri("https://test.api.com") };
            var sut = new TestableBaseTool(httpClient, _loggerMock.Object);

            // Act
            var result = await sut.GetAsync<TestModel>("/test", "TestOperation");

            // Assert
            var error = JsonSerializer.Deserialize<JsonElement>(result);
            error.GetProperty("error").GetString().Should().Contain("Network error");
        }

        [Fact]
        public async Task GetAsync_ShouldReturnError_WhenTimeoutOccurs()
        {
            // Arrange
            var handler = new MockHttpMessageHandler(new TaskCanceledException("The request was canceled due to the configured HttpClient.Timeout"));
            var httpClient = new HttpClient(handler) { BaseAddress = new Uri("https://test.api.com") };
            var sut = new TestableBaseTool(httpClient, _loggerMock.Object);

            // Act
            var result = await sut.GetAsync<TestModel>("/test", "TestOperation");

            // Assert
            var error = JsonSerializer.Deserialize<JsonElement>(result);
            error.GetProperty("error").GetString().Should().Contain("Request timed out");
        }

        [Fact]
        public async Task GetAsync_ShouldReturnError_WhenResponseContainsInvalidJson()
        {
            // Arrange
            var handler = new MockHttpMessageHandler(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("not valid json {{{", System.Text.Encoding.UTF8, "application/json")
            });
            var httpClient = new HttpClient(handler) { BaseAddress = new Uri("https://test.api.com") };
            var sut = new TestableBaseTool(httpClient, _loggerMock.Object);

            // Act
            var result = await sut.GetAsync<TestModel>("/test", "TestOperation");

            // Assert
            var error = JsonSerializer.Deserialize<JsonElement>(result);
            error.GetProperty("error").GetString().Should().Contain("JSON parsing error");
        }

        [Fact]
        public async Task GetAsync_ShouldReturnError_WhenDeserializationReturnsNull()
        {
            // Arrange
            var handler = new MockHttpMessageHandler(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("null", System.Text.Encoding.UTF8, "application/json")
            });
            var httpClient = new HttpClient(handler) { BaseAddress = new Uri("https://test.api.com") };
            var sut = new TestableBaseTool(httpClient, _loggerMock.Object);

            // Act
            var result = await sut.GetAsync<TestModel>("/test", "TestOperation");

            // Assert
            var error = JsonSerializer.Deserialize<JsonElement>(result);
            error.GetProperty("error").GetString().Should().Contain("Failed to deserialize");
        }

        #endregion

        #region Constructor Tests

        [Fact]
        public void Constructor_ShouldThrowArgumentNullException_WhenHttpClientIsNull()
        {
            // Act
            var act = () => new TestableBaseTool(null!, _loggerMock.Object);

            // Assert
            act.Should().Throw<ArgumentNullException>().WithParameterName("httpClient");
        }

        [Fact]
        public void Constructor_ShouldThrowArgumentNullException_WhenLoggerIsNull()
        {
            // Act
            var act = () => new TestableBaseTool(new HttpClient(), null!);

            // Assert
            act.Should().Throw<ArgumentNullException>().WithParameterName("logger");
        }

        #endregion
    }

    #region Test Helpers

    public class TestModel
    {
        public string Name { get; set; } = string.Empty;
        public int Value { get; set; }
    }

    public class MockHttpMessageHandler : HttpMessageHandler
    {
        private readonly HttpResponseMessage? _response;
        private readonly Exception? _exception;

        public HttpRequestMessage? LastRequest { get; private set; }

        public MockHttpMessageHandler(HttpResponseMessage response)
        {
            _response = response;
        }

        public MockHttpMessageHandler(Exception exception)
        {
            _exception = exception;
        }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            LastRequest = request;

            if (_exception != null)
                throw _exception;

            return Task.FromResult(_response!);
        }
    }

    #endregion
}
