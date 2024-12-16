using AutoFixture;
using Azure;
using Azure.Security.KeyVault.Secrets;
using Biotrackr.Weight.Svc.Models.Entities;
using Biotrackr.Weight.Svc.Services;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using Moq.Protected;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace Biotrackr.Weight.Svc.UnitTests.ServiceTests
{
    public class FitbitServiceShould
    {
        private readonly Mock<SecretClient> _mockSecretClient;
        private readonly Mock<HttpMessageHandler> _mockHttpMessageHandler;
        private readonly Mock<ILogger<FitbitService>> _mockLogger;
        private readonly HttpClient _httpClient;
        private readonly FitbitService _fitbitService;

        public FitbitServiceShould()
        {
            _mockSecretClient = new Mock<SecretClient>();
            _mockHttpMessageHandler = new Mock<HttpMessageHandler>();
            _mockLogger = new Mock<ILogger<FitbitService>>();
            _httpClient = new HttpClient(_mockHttpMessageHandler.Object);
            _fitbitService = new FitbitService(_mockSecretClient.Object, _httpClient, _mockLogger.Object);
        }

        [Fact]
        public async Task GetWeightLogs_ShouldReturnActivityResponse_WhenSuccessful()
        {
            // Arrange
            var startDate = "2023-10-01";
            var endDate = "2023-10-07";
            var accessToken = "testAccessToken";
            var fixture = new Fixture();
            var expectedResponse = fixture.Create<WeightResponse>();

            _mockSecretClient.Setup(s => s.GetSecretAsync("AccessToken", null, It.IsAny<CancellationToken>()))
                .ReturnsAsync(Response.FromValue(new KeyVaultSecret("AccessToken", accessToken), Mock.Of<Response>()));
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
            var result = await _fitbitService.GetWeightLogs(startDate, endDate);

            // Assert
            result.Should().BeEquivalentTo(expectedResponse);
        }

        [Fact]
        public async Task GetWeightLogs_ShouldHandleInvalidDateFormat()
        {
            // Arrange
            var date = "invalid-date";
            var validDate = "2023-10-01";
            var accessToken = "testAccessToken";

            _mockSecretClient.Setup(s => s.GetSecretAsync("AccessToken", null, It.IsAny<CancellationToken>()))
                .ReturnsAsync(Response.FromValue(new KeyVaultSecret("AccessToken", accessToken), Mock.Of<Response>()));
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
            Func<Task> act = async () => await _fitbitService.GetWeightLogs(date, validDate);

            // Assert
            await act.Should().ThrowAsync<HttpRequestException>();
        }

        [Fact]
        public async Task GetWeightLogs_ShouldHandleInvalidAccessToken()
        {
            // Arrange
            var startDate = "2023-10-01";
            var endDate = "2023-10-07";
            var accessToken = "invalidAccessToken";

            _mockSecretClient.Setup(s => s.GetSecretAsync("AccessToken", null, It.IsAny<CancellationToken>()))
                .ReturnsAsync(Response.FromValue(new KeyVaultSecret("AccessToken", accessToken), Mock.Of<Response>()));
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
            Func<Task> act = async () => await _fitbitService.GetWeightLogs(startDate, endDate);

            // Assert
            await act.Should().ThrowAsync<HttpRequestException>();
        }

        [Fact]
        public async Task GetWeightLogs_ShouldHandleEmptyResponse()
        {
            // Arrange
            var startDate = "2023-10-01";
            var endDate = "2023-10-07";
            var accessToken = "testAccessToken";

            _mockSecretClient.Setup(s => s.GetSecretAsync("AccessToken", null, It.IsAny<CancellationToken>()))
                .ReturnsAsync(Response.FromValue(new KeyVaultSecret("AccessToken", accessToken), Mock.Of<Response>()));
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
            Func<Task> act = async () => await _fitbitService.GetWeightLogs(startDate, endDate);

            // Assert
            await act.Should().ThrowAsync<JsonException>();
        }

        [Fact]
        public async Task GetWeightLogs_ShouldLogErrorAndThrow_WhenExceptionOccurs()
        {
            // Arrange
            var startDate = "2023-10-01";
            var endDate = "2023-10-07";
            var exceptionMessage = "Test exception";

            _mockSecretClient.Setup(s => s.GetSecretAsync("AccessToken", null, It.IsAny<CancellationToken>()))
                .ThrowsAsync(new Exception(exceptionMessage));

            // Act
            Func<Task> act = async () => await _fitbitService.GetWeightLogs(startDate, endDate);

            // Assert
            await act.Should().ThrowAsync<Exception>();
            _mockLogger.VerifyLog(logger => logger.LogError($"Exception thrown in GetWeightLogs: {exceptionMessage}"));
        }
    }
}
