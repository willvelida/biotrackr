using Bunit;
using Biotrackr.UI.Components.Shared;
using FluentAssertions;

namespace Biotrackr.UI.UnitTests.Components.Shared
{
    public class LoadingSpinnerShould : BunitContext
    {
        [Fact]
        public void RenderSpinner_WhenIsLoadingIsTrue()
        {
            var cut = Render<LoadingSpinner>(parameters => parameters
                .Add(p => p.IsLoading, true));

            cut.Find(".spinner-border").Should().NotBeNull();
        }

        [Fact]
        public void RenderMessage_WhenIsLoadingAndMessageProvided()
        {
            var cut = Render<LoadingSpinner>(parameters => parameters
                .Add(p => p.IsLoading, true)
                .Add(p => p.Message, "Loading data..."));

            cut.Markup.Should().Contain("Loading data...");
        }

        [Fact]
        public void RenderNothing_WhenIsLoadingIsFalse()
        {
            var cut = Render<LoadingSpinner>(parameters => parameters
                .Add(p => p.IsLoading, false));

            cut.Markup.Trim().Should().BeEmpty();
        }

        [Fact]
        public void NotRenderMessage_WhenIsLoadingButMessageIsNull()
        {
            var cut = Render<LoadingSpinner>(parameters => parameters
                .Add(p => p.IsLoading, true));

            cut.FindAll("span.ms-3").Should().BeEmpty();
        }
    }
}
