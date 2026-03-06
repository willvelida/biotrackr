using Bunit;
using Biotrackr.UI.Components.Shared;
using FluentAssertions;

namespace Biotrackr.UI.UnitTests.Components.Shared
{
    public class SummaryCardShould : BunitContext
    {
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

            cut.FindAll("small.text-muted").Should().BeEmpty();
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

            // IconContent render fragment is rendered inside card-icon div
            cut.Find(".card-icon").InnerHtml.Should().Contain("test-icon");
        }
    }
}
