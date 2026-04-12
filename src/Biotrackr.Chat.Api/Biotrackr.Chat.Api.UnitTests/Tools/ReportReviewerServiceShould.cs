using Biotrackr.Chat.Api.Configuration;
using Biotrackr.Chat.Api.Tools;
using FluentAssertions;
using Microsoft.Extensions.Caching.Memory;
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
        public void DefaultReviewCompletedToFalse()
        {
            // Arrange & Act
            var result = new ReviewResult();

            // Assert
            result.ReviewCompleted.Should().BeFalse();
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
        public async Task NotCacheFailedReviews()
        {
            // Arrange
            var cache = new MemoryCache(new MemoryCacheOptions());
            var sut = CreateService(reviewerSystemPrompt: "", cache: cache);

            // Act
            await sut.ReviewReportAsync("Test summary", new { }, "weekly_summary");

            // Assert
            cache.Count.Should().Be(0);
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
    }
}
