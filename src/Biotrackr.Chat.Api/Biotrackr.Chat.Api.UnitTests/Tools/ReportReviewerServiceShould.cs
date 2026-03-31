using Biotrackr.Chat.Api.Configuration;
using Biotrackr.Chat.Api.Tools;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;

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
            var sut = CreateService(reviewerSystemPrompt: "");

            var result = await sut.ReviewReportAsync("Test summary", new { }, "weekly_summary");

            result.Approved.Should().BeTrue();
            result.ValidatedSummary.Should().Be("Test summary");
            result.Concerns.Should().BeEmpty();
        }

        [Fact]
        public async Task SkipReviewWhenSystemPromptIsNull()
        {
            var sut = CreateService(reviewerSystemPrompt: null!);

            var result = await sut.ReviewReportAsync("Test summary", new { }, "diet_analysis");

            result.Approved.Should().BeTrue();
            result.ValidatedSummary.Should().Be("Test summary");
        }

        [Fact]
        public async Task SkipReviewWhenSystemPromptIsWhitespace()
        {
            var sut = CreateService(reviewerSystemPrompt: "   ");

            var result = await sut.ReviewReportAsync("Test summary", null, "trend_analysis");

            result.Approved.Should().BeTrue();
            result.ValidatedSummary.Should().Be("Test summary");
        }

        [Fact]
        public void ReviewResultShouldDefaultToApproved()
        {
            var result = new ReviewResult();

            result.Approved.Should().BeTrue();
            result.Concerns.Should().BeEmpty();
            result.ValidatedSummary.Should().BeEmpty();
        }

        private ReportReviewerService CreateService(string reviewerSystemPrompt = "Review this report")
        {
            var settings = Options.Create(new Settings
            {
                AnthropicApiKey = "test-key",
                ChatAgentModel = "claude-sonnet-4-20250514",
                ReviewerSystemPrompt = reviewerSystemPrompt
            });

            var httpClientFactory = new Mock<IHttpClientFactory>();
            httpClientFactory.Setup(f => f.CreateClient("Anthropic")).Returns(new HttpClient());

            return new ReportReviewerService(settings, _logger.Object, httpClientFactory.Object);
        }
    }
}
