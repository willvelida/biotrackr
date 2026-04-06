using Biotrackr.UI.Helpers;
using FluentAssertions;

namespace Biotrackr.UI.UnitTests.Helpers
{
    public class FormattingHelpersShould
    {
        [Theory]
        [InlineData(0, "--")]
        [InlineData(30, "30m")]
        [InlineData(60, "1h 0m")]
        [InlineData(90, "1h 30m")]
        [InlineData(150, "2h 30m")]
        public void FormatMinutesCorrectly(int minutes, string expected)
        {
            FormattingHelpers.FormatMinutes(minutes).Should().Be(expected);
        }

        [Theory]
        [InlineData(null, "--")]
        [InlineData(0, "0")]
        [InlineData(1234, "1,234")]
        [InlineData(10000, "10,000")]
        public void FormatNumberCorrectly(int? value, string expected)
        {
            FormattingHelpers.FormatNumber(value).Should().Be(expected);
        }

        [Theory]
        [InlineData(0, "0m")]
        [InlineData(60000, "1m")]
        [InlineData(3600000, "1h 0m")]
        [InlineData(5400000, "1h 30m")]
        public void FormatDurationCorrectly(long milliseconds, string expected)
        {
            FormattingHelpers.FormatDuration(milliseconds).Should().Be(expected);
        }

        [Theory]
        [InlineData(0, "0s")]
        [InlineData(1, "1s")]
        [InlineData(45, "45s")]
        [InlineData(59, "59s")]
        [InlineData(60, "1:00")]
        [InlineData(61, "1:01")]
        [InlineData(83, "1:23")]
        [InlineData(600, "10:00")]
        public void FormatElapsedTimeCorrectly(int totalSeconds, string expected)
        {
            FormattingHelpers.FormatElapsedTime(totalSeconds).Should().Be(expected);
        }
    }
}
