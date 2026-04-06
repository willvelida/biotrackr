using Bunit;
using Biotrackr.UI.Components.Shared;
using Biotrackr.UI.Models;
using Biotrackr.UI.UnitTests.Helpers;
using FluentAssertions;
using Radzen;

namespace Biotrackr.UI.UnitTests.Components.Shared
{
    public class SummaryCardShould : BunitContext
    {
        public SummaryCardShould()
        {
            Services.AddRadzenComponents();
            JSInterop.Mode = JSRuntimeMode.Loose;
            JSInterop.SetupRadzenChartInterop();
        }

        [Fact]
        public void RenderTitleAndValue()
        {
            var cut = Render<SummaryCard>(parameters => parameters
                .Add(p => p.Title, "Steps")
                .Add(p => p.Value, "10,000"));

            cut.Markup.Should().Contain("Steps");
            cut.Markup.Should().Contain("10,000");
        }

        [Fact]
        public void RenderSubtitle_WhenProvided()
        {
            var cut = Render<SummaryCard>(parameters => parameters
                .Add(p => p.Title, "Calories")
                .Add(p => p.Value, "2,500")
                .Add(p => p.Subtitle, "Goal: 3,000"));

            cut.Markup.Should().Contain("Goal: 3,000");
        }

        [Fact]
        public void NotRenderSubtitle_WhenNull()
        {
            var cut = Render<SummaryCard>(parameters => parameters
                .Add(p => p.Title, "Weight")
                .Add(p => p.Value, "80.5 kg"));

            cut.Markup.Should().NotContain("rz-text-caption");
        }

        [Fact]
        public void ApplyCardClass_WhenProvided()
        {
            var cut = Render<SummaryCard>(parameters => parameters
                .Add(p => p.Title, "Steps")
                .Add(p => p.Value, "5,000")
                .Add(p => p.CardClass, "card-activity"));

            cut.Find(".summary-card").ClassList.Should().Contain("card-activity");
        }

        [Fact]
        public void RenderDefaultValue_WhenNotSet()
        {
            var cut = Render<SummaryCard>(parameters => parameters
                .Add(p => p.Title, "Steps"));

            cut.Markup.Should().Contain("--");
        }

        [Fact]
        public void RenderIconContent_WhenProvided()
        {
            var cut = Render<SummaryCard>(parameters => parameters
                .Add(p => p.Title, "Steps")
                .Add(p => p.Value, "1,000")
                .Add(p => p.IconContent, "<span class=\"test-icon\">icon</span>"));

            cut.Find(".card-icon").InnerHtml.Should().Contain("test-icon");
        }

        [Fact]
        public void RenderSparkline_WhenTrendDataProvided()
        {
            var trendData = new List<TrendDataPoint>
            {
                new() { Date = "2026-03-01", Value = 8000 },
                new() { Date = "2026-03-02", Value = 9500 },
                new() { Date = "2026-03-03", Value = 7200 }
            };

            var cut = Render<SummaryCard>(parameters => parameters
                .Add(p => p.Title, "Steps")
                .Add(p => p.Value, "9,500")
                .Add(p => p.TrendData, trendData));

            cut.Markup.Should().Contain("rz-chart");
        }

        [Fact]
        public void RenderNoSparkline_WhenTrendDataIsNull()
        {
            var cut = Render<SummaryCard>(parameters => parameters
                .Add(p => p.Title, "Steps")
                .Add(p => p.Value, "10,000")
                .Add(p => p.TrendData, (IEnumerable<TrendDataPoint>?)null));

            cut.Markup.Should().NotContain("rz-sparkline");
        }

        [Fact]
        public void RenderNoSparkline_WhenTrendDataIsEmpty()
        {
            var cut = Render<SummaryCard>(parameters => parameters
                .Add(p => p.Title, "Steps")
                .Add(p => p.Value, "10,000")
                .Add(p => p.TrendData, new List<TrendDataPoint>()));

            cut.Markup.Should().NotContain("rz-sparkline");
        }

        [Fact]
        public void RenderSubtitleFromTrend_WhenProvided()
        {
            var cut = Render<SummaryCard>(parameters => parameters
                .Add(p => p.Title, "Calories")
                .Add(p => p.Value, "2,500")
                .Add(p => p.Subtitle, "Goal: 3,000"));

            cut.Markup.Should().Contain("Goal: 3,000");
        }

        [Fact]
        public void RenderNoSubtitleFromTrend_WhenNotProvided()
        {
            var cut = Render<SummaryCard>(parameters => parameters
                .Add(p => p.Title, "Steps")
                .Add(p => p.Value, "10,000"));

            cut.Markup.Should().NotContain("rz-text-caption");
        }

        [Fact]
        public void RenderIconContentFromTrend_WhenProvided()
        {
            var cut = Render<SummaryCard>(parameters => parameters
                .Add(p => p.Title, "Steps")
                .Add(p => p.Value, "1,000")
                .Add(p => p.IconContent, "<span class=\"test-icon\">icon</span>"));

            cut.Find(".card-icon").InnerHtml.Should().Contain("test-icon");
        }

        [Fact]
        public void RenderGauge_WhenGaugeMaxGreaterThanZero()
        {
            var cut = Render<SummaryCard>(parameters => parameters
                .Add(p => p.Title, "Steps")
                .Add(p => p.Value, "8,000")
                .Add(p => p.GaugeValue, 8000)
                .Add(p => p.GaugeMax, 10000)
                .Add(p => p.GaugeZoneLow, 5000)
                .Add(p => p.GaugeZoneHigh, 8000));

            cut.Markup.Should().Contain("rz-linear-gauge");
        }

        [Fact]
        public void NotRenderGauge_WhenGaugeMaxIsZero()
        {
            var cut = Render<SummaryCard>(parameters => parameters
                .Add(p => p.Title, "Steps")
                .Add(p => p.Value, "8,000")
                .Add(p => p.GaugeValue, 8000)
                .Add(p => p.GaugeMax, 0));

            cut.Markup.Should().NotContain("rz-linear-gauge");
        }
    }
}
