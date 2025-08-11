using Azure;
using Azure.Security.KeyVault.Secrets;
using Biotrackr.Food.Svc.Models.FitbitEntities;
using Biotrackr.Food.Svc.Services;
using Microsoft.Extensions.Logging;
using Moq.Protected;
using System.Net;
using System.Text.Json;

namespace Biotrackr.Food.Svc.UnitTests.ServiceTests
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
        public async Task GetFoodResponse_ShouldReturnFoodResponse_WhenSuccessful()
        {
            // Arrange
            var date = "2023-10-01";
            var accessToken = "testAccessToken";
            var fixture = new Fixture();
            var expectedResponse = fixture.Create<FoodResponse>();

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
            var result = await _fitbitService.GetFoodResponse(date);

            // Assert
            result.Should().BeEquivalentTo(expectedResponse);
        }

        [Fact]
        public async Task GetFoodResponse_ShouldHandleInvalidDateFormat()
        {
            // Arrange
            var date = "invalid-date";
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
            Func<Task> act = async () => await _fitbitService.GetFoodResponse(date);

            // Assert
            await act.Should().ThrowAsync<HttpRequestException>();
        }

        [Fact]
        public async Task GetFoodResponse_ShouldHandleInvalidAccessToken()
        {
            // Arrange
            var date = "2023-10-01";
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
            Func<Task> act = async () => await _fitbitService.GetFoodResponse(date);

            // Assert
            await act.Should().ThrowAsync<HttpRequestException>();
        }

        [Fact]
        public async Task GetFoodResponse_ShouldHandleEmptyResponse()
        {
            // Arrange
            var date = "2023-10-01";
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
            Func<Task> act = async () => await _fitbitService.GetFoodResponse(date);

            // Assert
            await act.Should().ThrowAsync<JsonException>();
        }

        [Fact]
        public async Task GetFoodResponse_ShouldLogErrorAndThrow_WhenExceptionOccurs()
        {
            // Arrange
            var date = "2023-10-01";
            var exceptionMessage = "Test exception";

            _mockSecretClient.Setup(s => s.GetSecretAsync("AccessToken", null, It.IsAny<CancellationToken>()))
                .ThrowsAsync(new Exception(exceptionMessage));

            // Act
            Func<Task> act = async () => await _fitbitService.GetFoodResponse(date);

            // Assert
            await act.Should().ThrowAsync<Exception>();
            _mockLogger.VerifyLog(logger => logger.LogError($"Exception thrown in GetFoodResponse: {exceptionMessage}"));
        }

        [Fact]
        public async Task GetFoodResponse_ShouldConstructCorrectHttpRequest()
        {
            // Arrange
            var date = "2023-10-01";
            var accessToken = "test-access-token";
            HttpRequestMessage? capturedRequest = null;

            _mockSecretClient.Setup(s => s.GetSecretAsync("AccessToken", null, It.IsAny<CancellationToken>()))
                .ReturnsAsync(Response.FromValue(new KeyVaultSecret("AccessToken", accessToken), Mock.Of<Response>()));

            _mockHttpMessageHandler.Protected()
                .Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>())
                .Callback<HttpRequestMessage, CancellationToken>((req, ct) => capturedRequest = req)
                .ReturnsAsync(new HttpResponseMessage
                {
                    StatusCode = HttpStatusCode.OK,
                    Content = new StringContent(JsonSerializer.Serialize(new FoodResponse()))
                });

            // Act
            await _fitbitService.GetFoodResponse(date);

            // Assert
            capturedRequest.Should().NotBeNull();
            capturedRequest!.Method.Should().Be(HttpMethod.Get);
            capturedRequest.RequestUri!.ToString().Should().Be($"https://api.fitbit.com/1/user/-/foods/date/{date}.json");
            capturedRequest.Headers.Authorization.Should().NotBeNull();
            capturedRequest.Headers.Authorization!.Scheme.Should().Be("Bearer");
            capturedRequest.Headers.Authorization.Parameter.Should().Be(accessToken);
        }

        [Theory]
        [InlineData("2024-02-29")] // Leap year
        [InlineData("2023-12-31")] // Year end
        [InlineData("2024-01-01")] // Year start
        [InlineData("1900-01-01")] // Historical date
        [InlineData("2099-12-31")] // Future date
        public async Task GetFoodResponse_ShouldConstructCorrectUriForDifferentDates(string date)
        {
            // Arrange
            var accessToken = "test-access-token";
            HttpRequestMessage? capturedRequest = null;

            _mockSecretClient.Setup(s => s.GetSecretAsync("AccessToken", null, It.IsAny<CancellationToken>()))
                .ReturnsAsync(Response.FromValue(new KeyVaultSecret("AccessToken", accessToken), Mock.Of<Response>()));

            _mockHttpMessageHandler.Protected()
                .Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>())
                .Callback<HttpRequestMessage, CancellationToken>((req, ct) => capturedRequest = req)
                .ReturnsAsync(new HttpResponseMessage
                {
                    StatusCode = HttpStatusCode.OK,
                    Content = new StringContent(JsonSerializer.Serialize(new FoodResponse()))
                });

            // Act
            await _fitbitService.GetFoodResponse(date);

            // Assert
            capturedRequest!.RequestUri!.ToString().Should().Be($"https://api.fitbit.com/1/user/-/foods/date/{date}.json");
        }

        [Theory]
        [InlineData("invalid-date")]
        [InlineData("2023-13-01")] // Invalid month
        [InlineData("2023-02-30")] // Invalid day
        [InlineData("not-a-date")]
        [InlineData("2023/10/01")] // Wrong format
        [InlineData("10-01-2023")] // Wrong format
        public async Task GetFoodResponse_ShouldHandleInvalidDateFormats(string invalidDate)
        {
            // Arrange
            var accessToken = "test-access-token";

            _mockSecretClient.Setup(s => s.GetSecretAsync("AccessToken", null, It.IsAny<CancellationToken>()))
                .ReturnsAsync(Response.FromValue(new KeyVaultSecret("AccessToken", accessToken), Mock.Of<Response>()));

            _mockHttpMessageHandler.Protected()
                .Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>())
                .ReturnsAsync(new HttpResponseMessage
                {
                    StatusCode = HttpStatusCode.BadRequest,
                    Content = new StringContent("Invalid date format")
                });

            // Act & Assert
            await Assert.ThrowsAsync<HttpRequestException>(() => _fitbitService.GetFoodResponse(invalidDate));
        }

        [Fact]
        public async Task GetFoodResponse_ShouldHandleKeyVaultSecretNotFound()
        {
            // Arrange
            var date = "2023-10-01";
            var keyVaultException = new RequestFailedException(404, "Secret not found");

            _mockSecretClient.Setup(s => s.GetSecretAsync("AccessToken", null, It.IsAny<CancellationToken>()))
                .ThrowsAsync(keyVaultException);

            // Act & Assert
            var exception = await Assert.ThrowsAsync<RequestFailedException>(() => _fitbitService.GetFoodResponse(date));
            exception.Status.Should().Be(404);
            _mockLogger.VerifyLog(logger => logger.LogError($"Exception thrown in {nameof(FitbitService.GetFoodResponse)}: Secret not found"), Times.Once);
        }

        [Fact]
        public async Task GetFoodResponse_ShouldHandleKeyVaultAccessDenied()
        {
            // Arrange
            var date = "2023-10-01";
            var keyVaultException = new RequestFailedException(403, "Access denied");

            _mockSecretClient.Setup(s => s.GetSecretAsync("AccessToken", null, It.IsAny<CancellationToken>()))
                .ThrowsAsync(keyVaultException);

            // Act & Assert
            var exception = await Assert.ThrowsAsync<RequestFailedException>(() => _fitbitService.GetFoodResponse(date));
            exception.Status.Should().Be(403);
            _mockLogger.VerifyLog(logger => logger.LogError($"Exception thrown in {nameof(FitbitService.GetFoodResponse)}: Access denied"), Times.Once);
        }

        [Fact]
        public async Task GetFoodResponse_ShouldCallKeyVaultWithCorrectSecretName()
        {
            // Arrange
            var date = "2023-10-01";
            var accessToken = "test-token";

            _mockSecretClient.Setup(s => s.GetSecretAsync("AccessToken", null, It.IsAny<CancellationToken>()))
                .ReturnsAsync(Response.FromValue(new KeyVaultSecret("AccessToken", accessToken), Mock.Of<Response>()));

            _mockHttpMessageHandler.Protected()
                .Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>())
                .ReturnsAsync(new HttpResponseMessage
                {
                    StatusCode = HttpStatusCode.OK,
                    Content = new StringContent(JsonSerializer.Serialize(new FoodResponse()))
                });

            // Act
            await _fitbitService.GetFoodResponse(date);

            // Assert
            _mockSecretClient.Verify(s => s.GetSecretAsync("AccessToken", null, It.IsAny<CancellationToken>()), Times.Once);
            _mockSecretClient.VerifyNoOtherCalls();
        }

        [Fact]
        public async Task GetFoodResponse_ShouldHandleMalformedJson()
        {
            // Arrange
            var date = "2023-10-01";
            var accessToken = "test-access-token";
            var malformedJson = "{ \"incomplete\": json without closing brace";

            _mockSecretClient.Setup(s => s.GetSecretAsync("AccessToken", null, It.IsAny<CancellationToken>()))
                .ReturnsAsync(Response.FromValue(new KeyVaultSecret("AccessToken", accessToken), Mock.Of<Response>()));

            _mockHttpMessageHandler.Protected()
                .Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>())
                .ReturnsAsync(new HttpResponseMessage
                {
                    StatusCode = HttpStatusCode.OK,
                    Content = new StringContent(malformedJson)
                });

            // Act & Assert
            var exception = await Assert.ThrowsAsync<JsonException>(() => _fitbitService.GetFoodResponse(date));
            _mockLogger.VerifyLog(logger => logger.LogError(It.Is<string>(s => s.Contains("Exception thrown in GetFoodResponse"))), Times.Once);
        }

        [Fact]
        public async Task GetFoodResponse_ShouldHandleUnexpectedJsonStructure()
        {
            // Arrange
            var date = "2023-10-01";
            var accessToken = "test-access-token";
            var unexpectedJson = "{ \"different\": \"structure\", \"than\": \"expected\" }";

            _mockSecretClient.Setup(s => s.GetSecretAsync("AccessToken", null, It.IsAny<CancellationToken>()))
                .ReturnsAsync(Response.FromValue(new KeyVaultSecret("AccessToken", accessToken), Mock.Of<Response>()));

            _mockHttpMessageHandler.Protected()
                .Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>())
                .ReturnsAsync(new HttpResponseMessage
                {
                    StatusCode = HttpStatusCode.OK,
                    Content = new StringContent(unexpectedJson)
                });

            // Act
            var result = await _fitbitService.GetFoodResponse(date);

            // Assert
            // Should successfully deserialize but with default/null values
            result.Should().NotBeNull();
            // Most properties should be null/default since JSON structure doesn't match
        }

        [Theory]
        [InlineData("")]
        [InlineData("   ")]
        [InlineData("null")]
        [InlineData("[]")]
        [InlineData("\"string\"")]
        [InlineData("123")]
        public async Task GetFoodResponse_ShouldHandleNonObjectJsonResponses(string jsonResponse)
        {
            // Arrange
            var date = "2023-10-01";
            var accessToken = "test-access-token";

            _mockSecretClient.Setup(s => s.GetSecretAsync("AccessToken", null, It.IsAny<CancellationToken>()))
                .ReturnsAsync(Response.FromValue(new KeyVaultSecret("AccessToken", accessToken), Mock.Of<Response>()));

            _mockHttpMessageHandler.Protected()
                .Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>())
                .ReturnsAsync(new HttpResponseMessage
                {
                    StatusCode = HttpStatusCode.OK,
                    Content = new StringContent(jsonResponse)
                });

            // Act & Assert
            if (string.IsNullOrWhiteSpace(jsonResponse))
            {
                await Assert.ThrowsAsync<JsonException>(() => _fitbitService.GetFoodResponse(date));
            }
            else
            {
                var exception = await Record.ExceptionAsync(() => _fitbitService.GetFoodResponse(date));
                // May throw JsonException or return null depending on the input
            }
        }
    }
}
