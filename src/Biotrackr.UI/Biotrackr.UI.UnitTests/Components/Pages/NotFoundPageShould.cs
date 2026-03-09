using Bunit;
using Radzen;
using Biotrackr.UI.Components.Pages;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;

namespace Biotrackr.UI.UnitTests.Components.Pages
{
    public class NotFoundPageShould : BunitContext
    {
        public NotFoundPageShould()
        {
            Services.AddRadzenComponents();
            JSInterop.Mode = JSRuntimeMode.Loose;
        }

        [Fact]
        public void Render404Heading()
        {
            var cut = Render<NotFound>();

            cut.Markup.Should().Contain("404");
        }

        [Fact]
        public void RenderNotFoundMessage()
        {
            var cut = Render<NotFound>();

            cut.Markup.Should().Contain("Page not found");
        }

        [Fact]
        public void RenderDescriptionText()
        {
            var cut = Render<NotFound>();

            cut.Markup.Should().Contain("The page you're looking for doesn't exist.");
        }

        [Fact]
        public void RenderBackToDashboardLink()
        {
            var cut = Render<NotFound>();

            cut.Markup.Should().Contain("Back to Dashboard");
        }
    }
}
