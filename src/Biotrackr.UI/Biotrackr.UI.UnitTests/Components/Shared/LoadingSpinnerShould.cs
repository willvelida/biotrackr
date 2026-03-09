using Bunit;
using Biotrackr.UI.Components.Shared;
using FluentAssertions;
using Radzen;

namespace Biotrackr.UI.UnitTests.Components.Shared
{
    public class LoadingSpinnerShould : BunitContext
    {
        public LoadingSpinnerShould()
        {
            Services.AddRadzenComponents();
        }

        [Fact]
        public void RenderSpinner_WhenIsLoadingIsTrue()
        {
            var cut = Render<LoadingSpinner>(parameters => parameters
                .Add(p => p.IsLoading, true));

            cut.Find(".rz-progressbar-circular").Should().NotBeNull();
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

            cut.Markup.Should().NotContain("rz-text-body2");
        }
    }
}
