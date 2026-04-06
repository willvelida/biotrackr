using System.Net;
using System.Net.Http.Json;
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
    public class RequestReportToolShould
    {
        private const string SampleSnapshot = """{"steps":[{"date":"2026-03-01","count":8500}]}""";

        private readonly Mock<ILogger<RequestReportTool>> _logger;
        private readonly Mock<IHttpClientFactory> _httpClientFactory;

        public RequestReportToolShould()
        {
            _logger = new Mock<ILogger<RequestReportTool>>();
            _httpClientFactory = new Mock<IHttpClientFactory>();
        }

        [Fact]
        public async Task ReturnJobIdOnSuccessfulRequest()
        {
            var responseBody = JsonSerializer.Serialize(new { jobId = "abc-123", status = "generating", message = "Started" });
            var handler = CreateMockHandler(HttpStatusCode.Accepted, responseBody);
            var sut = CreateTool(handler);

            var result = await sut.RequestReport("weekly_summary", "2026-03-01", "2026-03-07", "Generate a report", SampleSnapshot);

            result.Should().Contain("abc-123");
            result.Should().ContainEquivalentOf("started");
        }

        [Fact]
        public async Task ReturnStructuredJsonOnSuccessfulRequest()
        {
            var responseBody = JsonSerializer.Serialize(new { jobId = "job-456", status = "generating", message = "Started" });
            var handler = CreateMockHandler(HttpStatusCode.Accepted, responseBody);
            var sut = CreateTool(handler);

            var result = await sut.RequestReport("weekly_summary", "2026-03-01", "2026-03-07", "Generate a report", SampleSnapshot);

            using var doc = JsonDocument.Parse(result);
            doc.RootElement.GetProperty("jobId").GetString().Should().Be("job-456");
            doc.RootElement.GetProperty("status").GetString().Should().Be("generating");
            doc.RootElement.GetProperty("message").GetString().Should().Contain("Job ID: job-456");
        }

        [Fact]
        public async Task ReturnErrorMessageOnFailedRequest()
        {
            var handler = CreateMockHandler(HttpStatusCode.BadRequest, "{\"error\":\"Invalid report type\"}");
            var sut = CreateTool(handler);

            var result = await sut.RequestReport("invalid", "2026-03-01", "2026-03-07", "Generate a report", SampleSnapshot);

            result.Should().Contain("wasn't able to start your report");
            result.Should().NotContain("BadRequest");
        }

        [Fact]
        public async Task ReturnErrorMessageOnServerError()
        {
            var handler = CreateMockHandler(HttpStatusCode.InternalServerError, "");
            var sut = CreateTool(handler);

            var result = await sut.RequestReport("weekly_summary", "2026-03-01", "2026-03-07", "Generate a report", SampleSnapshot);

            result.Should().Contain("wasn't able to start your report");
            result.Should().NotContain("InternalServerError");
        }

        [Fact]
        public async Task SendCorrectRequestPayload()
        {
            string? capturedBody = null;
            var responseBody = JsonSerializer.Serialize(new { jobId = "test-job", status = "generating", message = "Started" });
            var handler = CreateMockHandler(HttpStatusCode.Accepted, responseBody, request =>
            {
                capturedBody = request.Content?.ReadAsStringAsync().Result;
            });
            var sut = CreateTool(handler);

            await sut.RequestReport("diet_analysis", "2026-01-01", "2026-03-01", "Analyze diet patterns", SampleSnapshot);

            capturedBody.Should().NotBeNull();
            capturedBody.Should().Contain("diet_analysis");
            capturedBody.Should().Contain("2026-01-01");
            capturedBody.Should().Contain("2026-03-01");
            capturedBody.Should().Contain("Analyze diet patterns");
            capturedBody.Should().Contain("steps");
        }

        [Fact]
        public void ExposeAsAIFunction()
        {
            var handler = CreateMockHandler(HttpStatusCode.OK, "{}");
            var sut = CreateTool(handler);

            var aiFunction = sut.AsAIFunction();

            aiFunction.Should().NotBeNull();
            aiFunction.Name.Should().Be("RequestReport");
        }

        private RequestReportTool CreateTool(Mock<HttpMessageHandler> handler)
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

            return new RequestReportTool(settings, _httpClientFactory.Object, _logger.Object);
        }

        private static Mock<HttpMessageHandler> CreateMockHandler(
            HttpStatusCode statusCode, string responseBody, Action<HttpRequestMessage>? onRequest = null)
        {
            var handler = new Mock<HttpMessageHandler>();
            handler.Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>())
                .Callback<HttpRequestMessage, CancellationToken>((req, _) => onRequest?.Invoke(req))
                .ReturnsAsync(new HttpResponseMessage
                {
                    StatusCode = statusCode,
                    Content = new StringContent(responseBody, System.Text.Encoding.UTF8, "application/json")
                });
            return handler;
        }
    }
}
