using Biotrackr.Reporting.Api.Models;
using FluentAssertions;

namespace Biotrackr.Reporting.Api.UnitTests.Models
{
    public class ReportMetadataShould
    {
        [Fact]
        public void InitializeWithDefaultValues()
        {
            var metadata = new ReportMetadata();

            metadata.JobId.Should().BeEmpty();
            metadata.Status.Should().Be(ReportStatus.Generating);
            metadata.ReportType.Should().BeEmpty();
            metadata.DateRange.Should().NotBeNull();
            metadata.Artifacts.Should().BeEmpty();
            metadata.GeneratedAt.Should().BeNull();
            metadata.Summary.Should().BeNull();
            metadata.Error.Should().BeNull();
        }

        [Fact]
        public void ReportJobResultShouldInitializeWithDefaults()
        {
            var result = new ReportJobResult();

            result.JobId.Should().BeEmpty();
            result.Status.Should().BeEmpty();
            result.Message.Should().BeEmpty();
        }

        [Fact]
        public void ReportStatusShouldDefineExpectedValues()
        {
            ReportStatus.Generating.Should().Be("generating");
            ReportStatus.Generated.Should().Be("generated");
            ReportStatus.Failed.Should().Be("failed");
            ReportStatus.Reviewed.Should().Be("reviewed");
        }

        [Theory]
        [InlineData("weekly_summary", true)]
        [InlineData("monthly_summary", true)]
        [InlineData("trend_analysis", true)]
        [InlineData("diet_analysis", true)]
        [InlineData("correlation_report", true)]
        [InlineData("invalid", false)]
        [InlineData("", false)]
        [InlineData("Weekly_Summary", false)]
        public void ValidateReportTypes(string reportType, bool expected)
        {
            ReportType.IsValid(reportType).Should().Be(expected);
        }

        [Fact]
        public void ReportDateRangeShouldInitializeWithDefaults()
        {
            var range = new ReportDateRange();

            range.Start.Should().BeEmpty();
            range.End.Should().BeEmpty();
        }

        [Fact]
        public void HaveNullReviewedAtByDefault()
        {
            // Arrange & Act
            var metadata = new ReportMetadata();

            // Assert
            metadata.ReviewedAt.Should().BeNull();
        }

        [Fact]
        public void HaveNullReviewApprovedByDefault()
        {
            // Arrange & Act
            var metadata = new ReportMetadata();

            // Assert
            metadata.ReviewApproved.Should().BeNull();
        }

        [Fact]
        public void HaveNullReviewConcernsByDefault()
        {
            // Arrange & Act
            var metadata = new ReportMetadata();

            // Assert
            metadata.ReviewConcerns.Should().BeNull();
        }

        [Fact]
        public void HaveNullReviewValidatedSummaryByDefault()
        {
            // Arrange & Act
            var metadata = new ReportMetadata();

            // Assert
            metadata.ReviewValidatedSummary.Should().BeNull();
        }
    }
}
