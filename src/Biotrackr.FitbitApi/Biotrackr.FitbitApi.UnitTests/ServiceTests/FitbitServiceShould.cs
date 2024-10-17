using AutoFixture;
using Biotrackr.FitbitApi.FitbitEntities;
using Biotrackr.FitbitApi.Services;
using Biotrackr.FitbitApi.Services.Interfaces;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using Moq.Protected;
using System.Net;
using System.Text.Json;

namespace Biotrackr.FitbitApi.UnitTests.ServiceTests
{
    public class FitbitServiceShould
    {
        private readonly Mock<ISecretService> _mockSecretService;
        private readonly Mock<HttpMessageHandler> _mockHttpMessageHandler;
        private readonly Mock<ILogger<FitbitService>> _mockLogger;
        private readonly HttpClient _httpClient;
        private readonly FitbitService _fitbitService;

        public FitbitServiceShould()
        {
            _mockSecretService = new Mock<ISecretService>();
            _mockHttpMessageHandler = new Mock<HttpMessageHandler>();
            _mockLogger = new Mock<ILogger<FitbitService>>();
            _httpClient = new HttpClient(_mockHttpMessageHandler.Object);
            _fitbitService = new FitbitService(_mockSecretService.Object, _httpClient, _mockLogger.Object);
        }

        [Fact]
        public async Task GetActivityResponse_ShouldReturnActivityResponse_WhenSuccessful()
        {
            // Arrange
            var date = "2023-10-01";
            var accessToken = "testAccessToken";
            var fixture = new Fixture();
            var expectedResponse = fixture.Create<ActivityResponse>();

            _mockSecretService.Setup(s => s.GetSecretAsync("AccessToken")).ReturnsAsync(accessToken);
            _mockHttpMessageHandler.Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>())
                .ReturnsAsync(new HttpResponseMessage
                {
                    StatusCode = HttpStatusCode.OK,
                    Content = new StringContent(JsonSerializer.Serialize(expectedResponse))
                });

            // Act
            var result = await _fitbitService.GetActivityResponse(date);

            // Assert
            result.Should().BeEquivalentTo(expectedResponse);
        }

        [Fact]
        public async Task GetActivityResponse_ShouldHandleInvalidDateFormat()
        {
            // Arrange
            var date = "invalid-date";
            var accessToken = "testAccessToken";

            _mockSecretService.Setup(s => s.GetSecretAsync("AccessToken")).ReturnsAsync(accessToken);
            _mockHttpMessageHandler.Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>())
                .ReturnsAsync(new HttpResponseMessage
                {
                    StatusCode = HttpStatusCode.BadRequest
                });

            // Act
            Func<Task> act = async () => await _fitbitService.GetActivityResponse(date);

            // Assert
            await act.Should().ThrowAsync<HttpRequestException>();
        }

        [Fact]
        public async Task GetActivityResponse_ShouldHandleInvalidAccessToken()
        {
            // Arrange
            var date = "2023-10-01";
            var accessToken = "invalidAccessToken";

            _mockSecretService.Setup(s => s.GetSecretAsync("AccessToken")).ReturnsAsync(accessToken);
            _mockHttpMessageHandler.Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>())
                .ReturnsAsync(new HttpResponseMessage
                {
                    StatusCode = HttpStatusCode.Unauthorized
                });

            // Act
            Func<Task> act = async () => await _fitbitService.GetActivityResponse(date);

            // Assert
            await act.Should().ThrowAsync<HttpRequestException>();
        }

        [Fact]
        public async Task GetActivityResponse_ShouldHandleEmptyResponse()
        {
            // Arrange
            var date = "2023-10-01";
            var accessToken = "testAccessToken";

            _mockSecretService.Setup(s => s.GetSecretAsync("AccessToken")).ReturnsAsync(accessToken);
            _mockHttpMessageHandler.Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>())
                .ReturnsAsync(new HttpResponseMessage
                {
                    StatusCode = HttpStatusCode.OK,
                    Content = new StringContent("")
                });

            // Act
            Func<Task> act = async () => await _fitbitService.GetActivityResponse(date);

            // Assert
            await act.Should().ThrowAsync<JsonException>();
        }

        [Fact]
        public async Task GetActivityResponse_ShouldLogErrorAndThrow_WhenExceptionOccurs()
        {
            // Arrange
            var date = "2023-10-01";
            var exceptionMessage = "Test exception";

            _mockSecretService.Setup(s => s.GetSecretAsync("AccessToken")).ThrowsAsync(new Exception(exceptionMessage));

            // Act
            Func<Task> act = async () => await _fitbitService.GetActivityResponse(date);

            // Assert
            await act.Should().ThrowAsync<Exception>();
            _mockLogger.VerifyLog(logger => logger.LogError($"Exception thrown in GetActivityResponse: {exceptionMessage}"));
        }
    }
}
