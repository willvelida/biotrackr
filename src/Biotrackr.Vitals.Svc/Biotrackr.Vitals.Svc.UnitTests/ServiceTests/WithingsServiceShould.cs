using Azure.Security.KeyVault.Secrets;
using Azure;
using Biotrackr.Vitals.Svc.Models.WithingsEntities;
using Biotrackr.Vitals.Svc.Services;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using Moq.Protected;
using System.Net;
using System.Text.Json;

namespace Biotrackr.Vitals.Svc.UnitTests.ServiceTests
{
    public class WithingsServiceShould
    {
        private readonly Mock<SecretClient> _mockSecretClient;
        private readonly Mock<HttpMessageHandler> _mockHttpMessageHandler;
        private readonly Mock<ILogger<WithingsService>> _mockLogger;
        private readonly WithingsService _sut;

        public WithingsServiceShould()
        {
            _mockSecretClient = new Mock<SecretClient>();
            _mockHttpMessageHandler = new Mock<HttpMessageHandler>();
            _mockLogger = new Mock<ILogger<WithingsService>>();

            var httpClient = new HttpClient(_mockHttpMessageHandler.Object);
            _sut = new WithingsService(_mockSecretClient.Object, httpClient, _mockLogger.Object);
        }

        [Fact]
        public async Task GetMeasurements_ShouldReturnMeasureResponse_WhenSuccessful()
        {
            // Arrange
            SetupSecretClient("test-access-token");
            var expectedResponse = CreateSuccessfulResponse(1);
            SetupHttpResponse(expectedResponse);

            // Act
            var result = await _sut.GetMeasurements("2026-02-04", "2026-04-02");

            // Assert
            result.Should().NotBeNull();
            result.Status.Should().Be(0);
            result.Body.Should().NotBeNull();
            result.Body!.MeasureGroups.Should().HaveCount(1);
        }

        [Fact]
        public async Task GetMeasurements_ShouldHandlePagination()
        {
            // Arrange
            SetupSecretClient("test-access-token");

            var page1 = new WithingsMeasureResponse
            {
                Status = 0,
                Body = new WithingsMeasureBody
                {
                    Timezone = "Australia/Melbourne",
                    MeasureGroups = [new MeasureGroup { GrpId = 1, Date = 1711929600, Measures = [new Measure { Value = 80250, Type = 1, Unit = -3 }] }],
                    More = 1,
                    Offset = 50
                }
            };

            var page2 = new WithingsMeasureResponse
            {
                Status = 0,
                Body = new WithingsMeasureBody
                {
                    Timezone = "Australia/Melbourne",
                    MeasureGroups = [new MeasureGroup { GrpId = 2, Date = 1711929700, Measures = [new Measure { Value = 81000, Type = 1, Unit = -3 }] }],
                    More = 0,
                    Offset = 0
                }
            };

            var callCount = 0;
            _mockHttpMessageHandler.Protected()
                .Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>())
                .ReturnsAsync(() =>
                {
                    callCount++;
                    var response = callCount == 1 ? page1 : page2;
                    return new HttpResponseMessage(HttpStatusCode.OK)
                    {
                        Content = new StringContent(JsonSerializer.Serialize(response))
                    };
                });

            // Act
            var result = await _sut.GetMeasurements("2026-02-04", "2026-04-02");

            // Assert
            result.Body!.MeasureGroups.Should().HaveCount(2);
            callCount.Should().Be(2);
        }

        [Fact]
        public async Task GetMeasurements_ShouldThrowWhenHttpRequestFails()
        {
            // Arrange
            SetupSecretClient("test-access-token");

            _mockHttpMessageHandler.Protected()
                .Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>())
                .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.Unauthorized) { Content = new StringContent("Unauthorized") });

            // Act
            Func<Task> act = async () => await _sut.GetMeasurements("2026-02-04", "2026-04-02");

            // Assert
            await act.Should().ThrowAsync<HttpRequestException>();
        }

        [Fact]
        public async Task GetMeasurements_ShouldThrowWhenWithingsStatusIsNonZero()
        {
            // Arrange
            SetupSecretClient("test-access-token");
            var errorResponse = new WithingsMeasureResponse { Status = 601, Body = null };
            SetupHttpResponse(errorResponse);

            // Act
            Func<Task> act = async () => await _sut.GetMeasurements("2026-02-04", "2026-04-02");

            // Assert
            await act.Should().ThrowAsync<InvalidOperationException>().WithMessage("*Status: 601*");
        }

        [Fact]
        public async Task GetMeasurements_ShouldHandleEmptyMeasureGroups()
        {
            // Arrange
            SetupSecretClient("test-access-token");
            var emptyResponse = CreateSuccessfulResponse(0);
            SetupHttpResponse(emptyResponse);

            // Act
            var result = await _sut.GetMeasurements("2026-02-04", "2026-04-02");

            // Assert
            result.Body!.MeasureGroups.Should().BeEmpty();
        }

        [Fact]
        public async Task GetMeasurements_ShouldLogInformationOnSuccess()
        {
            // Arrange
            SetupSecretClient("test-access-token");
            var response = CreateSuccessfulResponse(2);
            SetupHttpResponse(response);

            // Act
            await _sut.GetMeasurements("2026-02-04", "2026-04-02");

            // Assert
            _mockLogger.Verify(
                x => x.Log(
                    LogLevel.Information,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("2 measure group(s)")),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
                Times.Once);
        }

        [Fact]
        public async Task GetMeasurements_ShouldLogErrorOnException()
        {
            // Arrange
            _mockSecretClient.Setup(x => x.GetSecretAsync("WithingsAccessToken", null, It.IsAny<CancellationToken>()))
                .ThrowsAsync(new RequestFailedException(404, "Secret not found"));

            // Act
            Func<Task> act = async () => await _sut.GetMeasurements("2026-02-04", "2026-04-02");

            // Assert
            await act.Should().ThrowAsync<RequestFailedException>();
            _mockLogger.Verify(
                x => x.Log(
                    LogLevel.Error,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Exception thrown in GetMeasurements")),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
                Times.Once);
        }

        [Fact]
        public async Task GetMeasurements_ShouldRequestVisceralFatMeasureType()
        {
            // Arrange
            SetupSecretClient("test-access-token");
            string? capturedBody = null;

            _mockHttpMessageHandler.Protected()
                .Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>())
                .ReturnsAsync((HttpRequestMessage req, CancellationToken _) =>
                {
                    capturedBody = req.Content!.ReadAsStringAsync().Result;
                    var response = CreateSuccessfulResponse(1);
                    return new HttpResponseMessage(HttpStatusCode.OK)
                    {
                        Content = new StringContent(JsonSerializer.Serialize(response))
                    };
                });

            // Act
            await _sut.GetMeasurements("2026-02-04", "2026-04-02");

            // Assert
            capturedBody.Should().NotBeNull();
            capturedBody.Should().Contain("meastypes=");

            // Extract the meastypes value from URL-encoded form body
            var parsed = System.Web.HttpUtility.ParseQueryString(capturedBody!);
            var meastypes = parsed["meastypes"]!;

            // Type 170 = Visceral Fat (correct)
            meastypes.Split(',').Should().Contain("170");
            // Type 123 = VO2 Max (should NOT be requested as visceral fat)
            meastypes.Split(',').Should().NotContain("123");
        }

        [Fact]
        public async Task GetMeasurements_ShouldPreserveTimezoneFromResponse()
        {
            // Arrange
            SetupSecretClient("test-access-token");
            var expectedResponse = CreateSuccessfulResponse(1);
            SetupHttpResponse(expectedResponse);

            // Act
            var result = await _sut.GetMeasurements("2026-02-04", "2026-04-02");

            // Assert
            result.Body!.Timezone.Should().Be("Australia/Melbourne");
        }

        [Fact]
        public async Task GetMeasurements_ShouldPreserveTimezoneFromFirstPage_WhenPaginated()
        {
            // Arrange
            SetupSecretClient("test-access-token");

            var page1 = new WithingsMeasureResponse
            {
                Status = 0,
                Body = new WithingsMeasureBody
                {
                    Timezone = "Australia/Melbourne",
                    MeasureGroups = [new MeasureGroup { GrpId = 1, Date = 1711929600, Measures = [new Measure { Value = 80250, Type = 1, Unit = -3 }] }],
                    More = 1,
                    Offset = 50
                }
            };

            var page2 = new WithingsMeasureResponse
            {
                Status = 0,
                Body = new WithingsMeasureBody
                {
                    Timezone = "Europe/Paris",
                    MeasureGroups = [new MeasureGroup { GrpId = 2, Date = 1711929700, Measures = [new Measure { Value = 81000, Type = 1, Unit = -3 }] }],
                    More = 0,
                    Offset = 0
                }
            };

            var callCount = 0;
            _mockHttpMessageHandler.Protected()
                .Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>())
                .ReturnsAsync(() =>
                {
                    callCount++;
                    var response = callCount == 1 ? page1 : page2;
                    return new HttpResponseMessage(HttpStatusCode.OK)
                    {
                        Content = new StringContent(JsonSerializer.Serialize(response))
                    };
                });

            // Act
            var result = await _sut.GetMeasurements("2026-02-04", "2026-04-02");

            // Assert
            result.Body!.Timezone.Should().Be("Australia/Melbourne");
            result.Body.MeasureGroups.Should().HaveCount(2);
        }

        private void SetupSecretClient(string accessToken)
        {
            _mockSecretClient.Setup(x => x.GetSecretAsync("WithingsAccessToken", null, It.IsAny<CancellationToken>()))
                .ReturnsAsync(Response.FromValue(new KeyVaultSecret("WithingsAccessToken", accessToken), Mock.Of<Response>()));
        }

        private void SetupHttpResponse(WithingsMeasureResponse response)
        {
            _mockHttpMessageHandler.Protected()
                .Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>())
                .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent(JsonSerializer.Serialize(response))
                });
        }

        private static WithingsMeasureResponse CreateSuccessfulResponse(int groupCount)
        {
            var groups = Enumerable.Range(0, groupCount).Select(i => new MeasureGroup
            {
                GrpId = 100000 + i,
                Date = 1711929600 + (i * 86400),
                Measures = [new Measure { Value = 80250, Type = 1, Unit = -3 }]
            }).ToList();

            return new WithingsMeasureResponse
            {
                Status = 0,
                Body = new WithingsMeasureBody
                {
                    Timezone = "Australia/Melbourne",
                    MeasureGroups = groups,
                    More = 0,
                    Offset = 0
                }
            };
        }
    }
}
