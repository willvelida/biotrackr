using Biotrackr.UI.Helpers;
using Biotrackr.UI.Models.Chat;
using FluentAssertions;

namespace Biotrackr.UI.UnitTests.Helpers
{
    public class ReportStatusHelpersShould
    {
        [Theory]
        [InlineData(0, "Generating report...")]
        [InlineData(30, "Generating report...")]
        [InlineData(59, "Generating report...")]
        [InlineData(60, "Still working on your report...")]
        [InlineData(120, "Still working on your report...")]
        [InlineData(300, "Still working on your report...")]
        public void GetStatusText_ReturnCorrectLabel(int elapsedSeconds, string expected)
        {
            ReportStatusHelpers.GetStatusText(elapsedSeconds).Should().Be(expected);
        }

        [Fact]
        public void GetTerminalStatusText_ReturnReportNotFound_WhenNull()
        {
            ReportStatusHelpers.GetTerminalStatusText(null).Should().Be("Report not found");
        }

        [Theory]
        [InlineData("generated", "Report ready!")]
        [InlineData("reviewed", "Report ready!")]
        [InlineData("failed", "Report generation failed")]
        public void GetTerminalStatusText_ReturnCorrectText_ForTerminalStatuses(string status, string expected)
        {
            var response = new ReportStatusResponse { JobId = "job-1", Status = status };
            ReportStatusHelpers.GetTerminalStatusText(response).Should().Be(expected);
        }

        [Theory]
        [InlineData("generating")]
        [InlineData("unknown")]
        public void GetTerminalStatusText_ReturnNull_ForNonTerminalStatuses(string status)
        {
            var response = new ReportStatusResponse { JobId = "job-1", Status = status };
            ReportStatusHelpers.GetTerminalStatusText(response).Should().BeNull();
        }

        [Fact]
        public void IsTerminalStatus_ReturnTrue_WhenNull()
        {
            ReportStatusHelpers.IsTerminalStatus(null).Should().BeTrue();
        }

        [Theory]
        [InlineData("generated", true)]
        [InlineData("reviewed", true)]
        [InlineData("failed", true)]
        [InlineData("generating", false)]
        [InlineData("unknown", false)]
        public void IsTerminalStatus_ReturnCorrectResult(string status, bool expected)
        {
            var response = new ReportStatusResponse { JobId = "job-1", Status = status };
            ReportStatusHelpers.IsTerminalStatus(response).Should().Be(expected);
        }

        [Theory]
        [InlineData("generated", true)]
        [InlineData("reviewed", true)]
        [InlineData("failed", false)]
        [InlineData("generating", false)]
        public void IsCompletedSuccessfully_ReturnCorrectResult(string status, bool expected)
        {
            var response = new ReportStatusResponse { JobId = "job-1", Status = status };
            ReportStatusHelpers.IsCompletedSuccessfully(response).Should().Be(expected);
        }

        [Fact]
        public void IsCompletedSuccessfully_ReturnFalse_WhenNull()
        {
            ReportStatusHelpers.IsCompletedSuccessfully(null).Should().BeFalse();
        }

        [Fact]
        public void TryExtractJobId_ReturnJobId_FromStructuredJson()
        {
            var json = """{"jobId":"abc-123","status":"generating","message":"Report started."}""";
            ReportStatusHelpers.TryExtractJobId(json).Should().Be("abc-123");
        }

        [Fact]
        public void TryExtractJobId_ReturnNull_WhenContentIsNull()
        {
            ReportStatusHelpers.TryExtractJobId(null).Should().BeNull();
        }

        [Fact]
        public void TryExtractJobId_ReturnNull_WhenContentIsEmpty()
        {
            ReportStatusHelpers.TryExtractJobId("").Should().BeNull();
        }

        [Fact]
        public void TryExtractJobId_ReturnNull_WhenContentIsInvalidJson()
        {
            ReportStatusHelpers.TryExtractJobId("not json at all").Should().BeNull();
        }

        [Fact]
        public void TryExtractJobId_ReturnNull_WhenJsonHasNoJobId()
        {
            ReportStatusHelpers.TryExtractJobId("""{"status":"generating"}""").Should().BeNull();
        }

        [Fact]
        public void TryExtractJobId_ReturnJobId_CaseInsensitive()
        {
            var json = """{"JobId":"xyz-789","Status":"generating"}""";
            ReportStatusHelpers.TryExtractJobId(json).Should().Be("xyz-789");
        }

        [Theory]
        [InlineData(0, false)]
        [InlineData(599, false)]
        [InlineData(600, false)]
        [InlineData(601, true)]
        [InlineData(1000, true)]
        public void IsTimedOut_ReturnCorrectResult_WithDefaultMax(int elapsed, bool expected)
        {
            ReportStatusHelpers.IsTimedOut(elapsed).Should().Be(expected);
        }

        [Theory]
        [InlineData(100, 120, false)]
        [InlineData(121, 120, true)]
        public void IsTimedOut_ReturnCorrectResult_WithCustomMax(int elapsed, int max, bool expected)
        {
            ReportStatusHelpers.IsTimedOut(elapsed, max).Should().Be(expected);
        }
    }
}
