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
    public class A2AReportToolShould
    {
        private const string SampleSnapshot = """{"steps":[{"date":"2026-03-01","count":8500}]}""";

        private readonly Mock<ILogger<A2AReportTool>> _logger;
        private readonly Mock<IHttpClientFactory> _httpClientFactory;
        private readonly Mock<IReportReviewerService> _reviewerServiceMock;

        public A2AReportToolShould()
        {
            _logger = new Mock<ILogger<A2AReportTool>>();
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
        public async Task ReturnGracefulMessageWhenSnapshotIsNull()
        {
            var sut = CreateTool(CreateMockHandler(HttpStatusCode.OK, "{}"));

            var result = await sut.GenerateReport("weekly_summary", "2026-03-01", "2026-03-07", "Generate report", "");

            result.Should().Contain("couldn't process the health data");
        }

        [Fact]
        public async Task ReturnGracefulMessageWhenSnapshotIsInvalidJson()
        {
            var sut = CreateTool(CreateMockHandler(HttpStatusCode.OK, "{}"));

            var result = await sut.GenerateReport("weekly_summary", "2026-03-01", "2026-03-07", "Generate report", "not json");

            result.Should().Contain("couldn't process the health data");
        }

        [Fact]
        public async Task ReturnGracefulMessageOnHttpRequestException()
        {
            var handler = new Mock<HttpMessageHandler>();
            handler.Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>())
                .ThrowsAsync(new HttpRequestException("Connection refused"));

            var sut = CreateTool(handler);

            var result = await sut.GenerateReport("weekly_summary", "2026-03-01", "2026-03-07", "Generate report", SampleSnapshot);

            result.Should().Contain("unable to reach the report generation service");
        }

        [Fact]
        public async Task ReturnGracefulMessageOnTimeoutException()
        {
            var handler = new Mock<HttpMessageHandler>();
            handler.Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>())
                .ThrowsAsync(new TaskCanceledException("Timed out", new TimeoutException()));

            var sut = CreateTool(handler);

            var result = await sut.GenerateReport("weekly_summary", "2026-03-01", "2026-03-07", "Generate report", SampleSnapshot);

            result.Should().Contain("unexpected error");
        }

        [Fact]
        public async Task ReturnGracefulMessageOnUnexpectedException()
        {
            var handler = new Mock<HttpMessageHandler>();
            handler.Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>())
                .ThrowsAsync(new InvalidOperationException("Something broke"));

            var sut = CreateTool(handler);

            var result = await sut.GenerateReport("weekly_summary", "2026-03-01", "2026-03-07", "Generate report", SampleSnapshot);

            result.Should().Contain("unexpected error");
        }

        [Fact]
        public void ExposeGenerateReportAsAIFunction()
        {
            var sut = CreateTool(CreateMockHandler(HttpStatusCode.OK, "{}"));

            var aiFunction = sut.AsGenerateReportFunction();

            aiFunction.Should().NotBeNull();
            aiFunction.Name.Should().Be(nameof(A2AReportTool.GenerateReport));
        }

        [Fact]
        public void ExposeCheckReportStatusAsAIFunction()
        {
            var sut = CreateTool(CreateMockHandler(HttpStatusCode.OK, "{}"));

            var aiFunction = sut.AsCheckReportStatusFunction();

            aiFunction.Should().NotBeNull();
            aiFunction.Name.Should().Be(nameof(A2AReportTool.CheckReportStatus));
        }

        [Fact]
        public void ExposeGenerateReportFunctionWithDescription()
        {
            var sut = CreateTool(CreateMockHandler(HttpStatusCode.OK, "{}"));

            var aiFunction = sut.AsGenerateReportFunction();

            aiFunction.Description.Should().Contain("report");
            aiFunction.Description.Should().Contain("weekly_summary");
        }

        [Fact]
        public async Task LogInformationOnMethodEntry()
        {
            var handler = new Mock<HttpMessageHandler>();
            handler.Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>())
                .ThrowsAsync(new HttpRequestException("Connection refused"));

            var sut = CreateTool(handler);

            await sut.GenerateReport("weekly_summary", "2026-03-01", "2026-03-07", "Generate report", SampleSnapshot);

            _logger.Verify(
                x => x.Log(
                    LogLevel.Information,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("GenerateReport called")),
                    It.IsAny<Exception?>(),
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
                Times.Once);
        }

        [Fact]
        public async Task LogErrorOnHttpRequestException()
        {
            var handler = new Mock<HttpMessageHandler>();
            handler.Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>())
                .ThrowsAsync(new HttpRequestException("Connection refused"));

            var sut = CreateTool(handler);

            await sut.GenerateReport("weekly_summary", "2026-03-01", "2026-03-07", "Generate report", SampleSnapshot);

            _logger.Verify(
                x => x.Log(
                    LogLevel.Error,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Connection failure")),
                    It.IsAny<Exception?>(),
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
                Times.Once);
        }

        [Fact]
        public async Task PresentReviewStatusWhenReviewNotCompleted()
        {
            // Arrange
            _reviewerServiceMock.Setup(r => r.ReviewReportAsync(It.IsAny<string>(), It.IsAny<object?>(), It.IsAny<string>()))
                .ReturnsAsync(new ReviewResult
                {
                    Approved = true,
                    ReviewCompleted = false,
                    ReviewSkipReason = "Reviewer agent failed due to a service error",
                    ValidatedSummary = "Test summary\n\n⚠️ Note: review failed",
                    Concerns = ["Review could not be completed: the reviewer service encountered an error."]
                });
            var statusBody = CreateStatusResponseWithArtifacts("generated", "Test summary");
            var handler = CreateMockHandler(HttpStatusCode.OK, statusBody);
            var sut = CreateTool(handler);

            // Act
            var result = await sut.CheckReportStatus("job-123");

            // Assert
            result.Should().Contain("Review Status");
            result.Should().Contain("independent review did not complete");
            result.Should().Contain("reviewer service encountered an error");
            result.Should().NotContain("regenerate");
        }

        [Fact]
        public async Task IncludeArtifactsWhenReviewNotCompleted()
        {
            // Arrange
            _reviewerServiceMock.Setup(r => r.ReviewReportAsync(It.IsAny<string>(), It.IsAny<object?>(), It.IsAny<string>()))
                .ReturnsAsync(new ReviewResult
                {
                    Approved = true,
                    ReviewCompleted = false,
                    ReviewSkipReason = "Reviewer system prompt not configured",
                    ValidatedSummary = "Test summary",
                    Concerns = ["Review was skipped."]
                });
            var statusBody = CreateStatusResponseWithArtifacts("generated", "Test summary",
                artifacts: new Dictionary<string, string> { { "report.pdf", "https://example.com/report.pdf" } });
            var handler = CreateMockHandler(HttpStatusCode.OK, statusBody);
            var sut = CreateTool(handler);

            // Act
            var result = await sut.CheckReportStatus("job-123");

            // Assert
            result.Should().Contain("report.pdf");
            result.Should().Contain("Review Status");
        }

        private A2AReportTool CreateTool(Mock<HttpMessageHandler> handler)
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

            return new A2AReportTool(
                settings,
                _httpClientFactory.Object,
                _reviewerServiceMock.Object,
                _logger.Object);
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

        private static string CreateStatusResponseWithArtifacts(
            string status, string summary,
            Dictionary<string, string>? artifacts = null)
        {
            var metadata = new
            {
                jobId = "job-123",
                status,
                reportType = "weekly_summary",
                summary,
                error = (string?)null,
                sourceDataSnapshot = new { },
                artifacts = (artifacts?.Keys.ToList()) ?? new List<string>()
            };

            return JsonSerializer.Serialize(new
            {
                metadata,
                artifactUrls = artifacts ?? new Dictionary<string, string>()
            });
        }
    }
}
