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
            // Act
            var cut = Render<SummaryCard>(parameters => parameters
                .Add(p => p.Title, "Steps")
                .Add(p => p.Value, "10,000"));

            // Assert
            cut.Markup.Should().Contain("Steps");
            cut.Markup.Should().Contain("10,000");
        }

        [Fact]
        public void RenderSubtitle_WhenProvided()
        {
            // Act
            var cut = Render<SummaryCard>(parameters => parameters
                .Add(p => p.Title, "Calories")
                .Add(p => p.Value, "2,500")
                .Add(p => p.Subtitle, "Goal: 3,000"));

            // Assert
            cut.Markup.Should().Contain("Goal: 3,000");
        }

        [Fact]
        public void NotRenderSubtitle_WhenNull()
        {
            // Act
            var cut = Render<SummaryCard>(parameters => parameters
                .Add(p => p.Title, "Weight")
                .Add(p => p.Value, "80.5 kg"));

            // Assert
            cut.Markup.Should().NotContain("rz-text-caption");
        }

        [Fact]
        public void ApplyCardClass_WhenProvided()
        {
            // Act
            var cut = Render<SummaryCard>(parameters => parameters
                .Add(p => p.Title, "Steps")
                .Add(p => p.Value, "5,000")
                .Add(p => p.CardClass, "card-activity"));

            // Assert
            cut.Find(".summary-card").ClassList.Should().Contain("card-activity");
        }

        [Fact]
        public void RenderDefaultValue_WhenNotSet()
        {
            // Act
            var cut = Render<SummaryCard>(parameters => parameters
                .Add(p => p.Title, "Steps"));

            // Assert
            cut.Markup.Should().Contain("--");
        }

        [Fact]
        public void RenderIconContent_WhenProvided()
        {
            // Act
            var cut = Render<SummaryCard>(parameters => parameters
                .Add(p => p.Title, "Steps")
                .Add(p => p.Value, "1,000")
                .Add(p => p.IconContent, "<span class=\"test-icon\">icon</span>"));

            // Assert
            cut.Find(".card-icon").InnerHtml.Should().Contain("test-icon");
        }

        [Fact]
        public void RenderSparkline_WhenTrendDataProvided()
        {
            // Arrange
            var trendData = new List<TrendDataPoint>
            {
                new() { Date = "2026-03-01", Value = 8000 },
                new() { Date = "2026-03-02", Value = 9500 },
                new() { Date = "2026-03-03", Value = 7200 }
            };

            // Act
            var cut = Render<SummaryCard>(parameters => parameters
                .Add(p => p.Title, "Steps")
                .Add(p => p.Value, "9,500")
                .Add(p => p.TrendData, trendData));

            // Assert
            cut.Markup.Should().Contain("rz-chart");
        }

        [Fact]
        public void RenderNoSparkline_WhenTrendDataIsNull()
        {
            // Act
            var cut = Render<SummaryCard>(parameters => parameters
                .Add(p => p.Title, "Steps")
                .Add(p => p.Value, "10,000")
                .Add(p => p.TrendData, (IEnumerable<TrendDataPoint>?)null));

            // Assert
            cut.Markup.Should().NotContain("rz-sparkline");
        }

        [Fact]
        public void RenderNoSparkline_WhenTrendDataIsEmpty()
        {
            // Act
            var cut = Render<SummaryCard>(parameters => parameters
                .Add(p => p.Title, "Steps")
                .Add(p => p.Value, "10,000")
                .Add(p => p.TrendData, new List<TrendDataPoint>()));

            // Assert
            cut.Markup.Should().NotContain("rz-sparkline");
        }

        [Fact]
        public void RenderSubtitleFromTrend_WhenProvided()
        {
            // Act
            var cut = Render<SummaryCard>(parameters => parameters
                .Add(p => p.Title, "Calories")
                .Add(p => p.Value, "2,500")
                .Add(p => p.Subtitle, "Goal: 3,000"));

            // Assert
            cut.Markup.Should().Contain("Goal: 3,000");
        }

        [Fact]
        public void RenderNoSubtitleFromTrend_WhenNotProvided()
        {
            // Act
            var cut = Render<SummaryCard>(parameters => parameters
                .Add(p => p.Title, "Steps")
                .Add(p => p.Value, "10,000"));

            // Assert
            cut.Markup.Should().NotContain("rz-text-caption");
        }

        [Fact]
        public void RenderIconContentFromTrend_WhenProvided()
        {
            // Act
            var cut = Render<SummaryCard>(parameters => parameters
                .Add(p => p.Title, "Steps")
                .Add(p => p.Value, "1,000")
                .Add(p => p.IconContent, "<span class=\"test-icon\">icon</span>"));

            // Assert
            cut.Find(".card-icon").InnerHtml.Should().Contain("test-icon");
        }

        [Fact]
        public void RenderGauge_WhenGaugeMaxGreaterThanZero()
        {
            // Act
            var cut = Render<SummaryCard>(parameters => parameters
                .Add(p => p.Title, "Steps")
                .Add(p => p.Value, "8,000")
                .Add(p => p.GaugeValue, 8000)
                .Add(p => p.GaugeMax, 10000)
                .Add(p => p.GaugeZoneLow, 5000)
                .Add(p => p.GaugeZoneHigh, 8000));

            // Assert
            cut.Markup.Should().Contain("rz-linear-gauge");
        }

        [Fact]
        public void NotRenderGauge_WhenGaugeMaxIsZero()
        {
            // Act
            var cut = Render<SummaryCard>(parameters => parameters
                .Add(p => p.Title, "Steps")
                .Add(p => p.Value, "8,000")
                .Add(p => p.GaugeValue, 8000)
                .Add(p => p.GaugeMax, 0));

            // Assert
            cut.Markup.Should().NotContain("rz-linear-gauge");
        }
    }
}
