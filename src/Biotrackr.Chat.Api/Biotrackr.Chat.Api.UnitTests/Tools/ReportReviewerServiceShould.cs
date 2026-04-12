using Biotrackr.Chat.Api.Configuration;
using Biotrackr.Chat.Api.Tools;
using FluentAssertions;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Moq.Protected;
using System.Net;
using System.Text;
using System.Text.Json;

namespace Biotrackr.Chat.Api.UnitTests.Tools
{
    public class ReportReviewerServiceShould
    {
        private readonly Mock<ILogger<ReportReviewerService>> _logger;

        public ReportReviewerServiceShould()
        {
            _logger = new Mock<ILogger<ReportReviewerService>>();
        }

        [Fact]
        public async Task SkipReviewWhenSystemPromptIsEmpty()
        {
            // Arrange
            var sut = CreateService(reviewerSystemPrompt: "");

            // Act
            var result = await sut.ReviewReportAsync("Test summary", new { }, "weekly_summary");

            // Assert
            result.Approved.Should().BeTrue();
            result.ReviewCompleted.Should().BeFalse();
            result.ReviewSkipReason.Should().Contain("not configured");
            result.Concerns.Should().NotBeEmpty();
            result.ValidatedSummary.Should().Contain("Test summary");
            result.ValidatedSummary.Should().Contain("not been independently reviewed");
        }

        [Fact]
        public async Task SkipReviewWhenSystemPromptIsNull()
        {
            // Arrange
            var sut = CreateService(reviewerSystemPrompt: null!);

            // Act
            var result = await sut.ReviewReportAsync("Test summary", new { }, "diet_analysis");

            // Assert
            result.Approved.Should().BeTrue();
            result.ReviewCompleted.Should().BeFalse();
            result.ReviewSkipReason.Should().NotBeNullOrEmpty();
            result.ValidatedSummary.Should().Contain("Test summary");
        }

        [Fact]
        public async Task SkipReviewWhenSystemPromptIsWhitespace()
        {
            // Arrange
            var sut = CreateService(reviewerSystemPrompt: "   ");

            // Act
            var result = await sut.ReviewReportAsync("Test summary", null, "trend_analysis");

            // Assert
            result.Approved.Should().BeTrue();
            result.ReviewCompleted.Should().BeFalse();
            result.ValidatedSummary.Should().Contain("Test summary");
        }

        [Fact]
        public void ReviewResultShouldDefaultToApproved()
        {
            // Arrange & Act
            var result = new ReviewResult();

            // Assert
            result.Approved.Should().BeTrue();
            result.ReviewCompleted.Should().BeFalse();
            result.Concerns.Should().BeEmpty();
            result.ValidatedSummary.Should().BeEmpty();
            result.ReviewSkipReason.Should().BeNull();
        }

        [Fact]
        public async Task PopulateConcernsWhenPromptIsEmpty()
        {
            // Arrange
            var sut = CreateService(reviewerSystemPrompt: "");

            // Act
            var result = await sut.ReviewReportAsync("Test summary", new { }, "weekly_summary");

            // Assert
            result.Concerns.Should().ContainSingle()
                .Which.Should().Contain("not configured");
        }

        [Fact]
        public async Task SetReviewSkipReasonWhenPromptIsEmpty()
        {
            // Arrange
            var sut = CreateService(reviewerSystemPrompt: "");

            // Act
            var result = await sut.ReviewReportAsync("Test summary", new { }, "weekly_summary");

            // Assert
            result.ReviewSkipReason.Should().Be("Reviewer system prompt not configured");
        }

        [Fact]
        public async Task NotCacheSkippedReviews()
        {
            // Arrange
            var cache = new MemoryCache(new MemoryCacheOptions());
            var sut = CreateService(reviewerSystemPrompt: "", cache: cache);

            // Act
            await sut.ReviewReportAsync("Test summary", new { }, "weekly_summary");

            // Assert
            cache.Count.Should().Be(0);
        }

        [Fact]
        public async Task ReturnFailClosedResultWhenClaudeApiThrows()
        {
            // Arrange
            var handler = new Mock<HttpMessageHandler>();
            handler.Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>())
                .ThrowsAsync(new HttpRequestException("Connection refused"));
            var sut = CreateServiceWithHandler(handler);

            // Act
            var result = await sut.ReviewReportAsync("Test summary", new { steps = 5000 }, "weekly_summary");

            // Assert
            result.ReviewCompleted.Should().BeFalse();
            result.Approved.Should().BeTrue();
            result.ReviewSkipReason.Should().Contain("service error");
            result.Concerns.Should().NotBeEmpty();
            result.ValidatedSummary.Should().Contain("service error");
        }

        [Fact]
        public async Task UseSanitizedReviewSkipReasonWhenApiCallFails()
        {
            // Arrange
            var handler = new Mock<HttpMessageHandler>();
            handler.Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>())
                .ThrowsAsync(new HttpRequestException("API rate limited"));
            var sut = CreateServiceWithHandler(handler);

            // Act
            var result = await sut.ReviewReportAsync("Test summary", new { }, "diet_analysis");

            // Assert
            result.ReviewSkipReason.Should().Be("Reviewer agent failed due to a service error");
            result.ReviewSkipReason.Should().NotContain("API rate limited");
        }

        [Fact]
        public async Task NotCacheResultWhenApiCallFails()
        {
            // Arrange
            var cache = new MemoryCache(new MemoryCacheOptions());
            var handler = new Mock<HttpMessageHandler>();
            handler.Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>())
                .ThrowsAsync(new HttpRequestException("Service unavailable"));
            var sut = CreateServiceWithHandler(handler, cache: cache);

            // Act
            await sut.ReviewReportAsync("Test summary", new { }, "weekly_summary");

            // Assert
            cache.Count.Should().Be(0);
        }

        [Fact]
        public async Task ReturnFailClosedResultWhenClaudeReturnsNonJsonResponse()
        {
            // Arrange — response text has no JSON braces so ParseReviewResult returns fail-closed
            var handler = CreateAnthropicMockHandler("I cannot review this report at this time");
            var sut = CreateServiceWithHandler(handler);

            // Act
            var result = await sut.ReviewReportAsync("Test summary", new { }, "weekly_summary");

            // Assert
            result.ReviewCompleted.Should().BeFalse();
            result.ReviewSkipReason.Should().Contain("parse");
            result.Concerns.Should().NotBeEmpty();
            result.ValidatedSummary.Should().Contain("Test summary");
        }

        private ReportReviewerService CreateService(string reviewerSystemPrompt = "Review this report", IMemoryCache? cache = null)
        {
            var settings = Options.Create(new Settings
            {
                AnthropicApiKey = "test-key",
                ChatAgentModel = "claude-sonnet-4-20250514",
                ReviewerSystemPrompt = reviewerSystemPrompt
            });

            var httpClientFactory = new Mock<IHttpClientFactory>();
            httpClientFactory.Setup(f => f.CreateClient("Anthropic")).Returns(new HttpClient());

            return new ReportReviewerService(settings, _logger.Object, httpClientFactory.Object, cache ?? new MemoryCache(new MemoryCacheOptions()));
        }

        private ReportReviewerService CreateServiceWithHandler(
            Mock<HttpMessageHandler> handler,
            string reviewerSystemPrompt = "Review this report",
            IMemoryCache? cache = null)
        {
            var settings = Options.Create(new Settings
            {
                AnthropicApiKey = "test-key",
                ChatAgentModel = "claude-sonnet-4-20250514",
                ReviewerSystemPrompt = reviewerSystemPrompt
            });

            var httpClient = new HttpClient(handler.Object);
            var httpClientFactory = new Mock<IHttpClientFactory>();
            httpClientFactory.Setup(f => f.CreateClient("Anthropic")).Returns(httpClient);

            return new ReportReviewerService(settings, _logger.Object, httpClientFactory.Object, cache ?? new MemoryCache(new MemoryCacheOptions()));
        }

        private static Mock<HttpMessageHandler> CreateAnthropicMockHandler(string textContent)
        {
            var response = new
            {
                id = "msg_test123",
                type = "message",
                role = "assistant",
                content = new[] { new { type = "text", text = textContent } },
                model = "claude-sonnet-4-20250514",
                stop_reason = "end_turn",
                stop_sequence = (string?)null,
                usage = new
                {
                    input_tokens = 100,
                    output_tokens = 50,
                    cache_read_input_tokens = 0,
                    cache_creation_input_tokens = 0
                }
            };
            var body = JsonSerializer.Serialize(response);

            var handler = new Mock<HttpMessageHandler>();
            handler.Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>())
                .ReturnsAsync(new HttpResponseMessage
                {
                    StatusCode = HttpStatusCode.OK,
                    Content = new StringContent(body, Encoding.UTF8, "application/json")
                });
            return handler;
        }
    }
}
