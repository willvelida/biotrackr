using Bunit;
using Biotrackr.UI.Components.Shared;
using FluentAssertions;

namespace Biotrackr.UI.UnitTests.Components.Shared
{
    public class ErrorDisplayShould : BunitContext
    {
        [Fact]
        public void RenderAlert_WhenMessageIsProvided()
        {
            var cut = Render<ErrorDisplay>(parameters => parameters
                .Add(p => p.Message, "Something went wrong"));

            cut.Find(".alert-danger").Should().NotBeNull();
            cut.Markup.Should().Contain("Something went wrong");
        }

        [Fact]
        public void RenderNothing_WhenMessageIsNull()
        {
            var cut = Render<ErrorDisplay>(parameters => parameters
                .Add(p => p.Message, null));

            cut.Markup.Trim().Should().BeEmpty();
        }

        [Fact]
        public void RenderNothing_WhenMessageIsEmpty()
        {
            var cut = Render<ErrorDisplay>(parameters => parameters
                .Add(p => p.Message, ""));

            cut.Markup.Trim().Should().BeEmpty();
        }
    }
}
