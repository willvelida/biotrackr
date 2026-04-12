using System.Net;
using System.Text.Json;
using Biotrackr.Chat.Api.Configuration;
using Biotrackr.Chat.Api.Tools;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Moq.Protected;

namespace Biotrackr.Chat.Api.UnitTests.Tools
{
    public class GetReportStatusToolShould
    {
        private readonly Mock<ILogger<GetReportStatusTool>> _logger;
        private readonly Mock<IHttpClientFactory> _httpClientFactory;
        private readonly Mock<IReportReviewerService> _reviewerServiceMock;

        public GetReportStatusToolShould()
        {
            _logger = new Mock<ILogger<GetReportStatusTool>>();
            _httpClientFactory = new Mock<IHttpClientFactory>();

            _reviewerServiceMock = new Mock<IReportReviewerService>();
            _reviewerServiceMock.Setup(r => r.ReviewReportAsync(It.IsAny<string>(), It.IsAny<object?>(), It.IsAny<string>()))
                .ReturnsAsync(new ReviewResult
                {
                    Approved = true,
                    ReviewCompleted = true,
                    ValidatedSummary = "Validated summary"
                });
        }

        [Fact]
        public async Task ReturnNotFoundMessageForMissingJob()
        {
            var handler = CreateMockHandler(HttpStatusCode.NotFound, "");
            var sut = CreateTool(handler);

            var result = await sut.GetReportStatus("nonexistent-job");

            result.Should().Contain("couldn't find that report");
            result.Should().NotContain("nonexistent-job");
        }

        [Fact]
        public async Task ReturnErrorMessageOnServerError()
        {
            var handler = CreateMockHandler(HttpStatusCode.InternalServerError, "Server error");
            var sut = CreateTool(handler);

            var result = await sut.GetReportStatus("job-123");

            result.Should().Contain("unable to check your report status");
            result.Should().NotContain("InternalServerError");
            result.Should().NotContain("Server error");
        }

        [Fact]
        public async Task ReturnGeneratingMessageWhenStillInProgress()
        {
            var body = CreateStatusResponse("generating");
            var handler = CreateMockHandler(HttpStatusCode.OK, body);
            var sut = CreateTool(handler);

            var result = await sut.GetReportStatus("job-123");

            result.Should().Contain("still being generated");
        }

        [Fact]
        public async Task ReturnFailedMessageWithoutExposingInternalError()
        {
            var body = CreateStatusResponse("failed", error: "Timeout occurred");
            var handler = CreateMockHandler(HttpStatusCode.OK, body);
            var sut = CreateTool(handler);

            var result = await sut.GetReportStatus("job-123");

            result.Should().Contain("couldn't be completed");
            result.Should().NotContain("Timeout occurred");
        }

        [Fact]
        public void ExposeAsAIFunction()
        {
            var handler = CreateMockHandler(HttpStatusCode.OK, "{}");
            var sut = CreateTool(handler);

            var aiFunction = sut.AsAIFunction();

            aiFunction.Should().NotBeNull();
            aiFunction.Name.Should().Be("GetReportStatus");
        }

        private GetReportStatusTool CreateTool(Mock<HttpMessageHandler> handler)
        {
            var httpClient = new HttpClient(handler.Object)
            {
                BaseAddress = new Uri("http://localhost:5000")
            };

            _httpClientFactory.Setup(f => f.CreateClient("ReportingApi")).Returns(httpClient);

            var settings = Options.Create(new Settings
            {
                ReportingApiUrl = "http://localhost:5000",
                AnthropicApiKey = "test-key",
                ChatAgentModel = "claude-sonnet-4-20250514"
            });

            return new GetReportStatusTool(settings, _httpClientFactory.Object, _reviewerServiceMock.Object, _logger.Object);
        }

        private static string CreateStatusResponse(string status, string? summary = null, string? error = null)
        {
            var metadata = new
            {
                jobId = "job-123",
                status,
                reportType = "weekly_summary",
                summary = summary ?? "Test summary",
                error,
                sourceDataSnapshot = new { },
                artifacts = new List<string>()
            };

            return JsonSerializer.Serialize(new
            {
                metadata,
                artifactUrls = new Dictionary<string, string>()
            });
        }

        private static Mock<HttpMessageHandler> CreateMockHandler(HttpStatusCode statusCode, string responseBody)
        {
            var handler = new Mock<HttpMessageHandler>();
            handler.Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>())
                .ReturnsAsync(new HttpResponseMessage
                {
                    StatusCode = statusCode,
                    Content = new StringContent(responseBody, System.Text.Encoding.UTF8, "application/json")
                });
            return handler;
        }
    }
}
