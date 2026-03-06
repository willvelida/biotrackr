using Bunit;
using Biotrackr.UI.Components.Pages;
using FluentAssertions;

namespace Biotrackr.UI.UnitTests.Components.Pages
{
    public class NotFoundPageShould : BunitContext
    {
        [Fact]
        public void Render404Heading()
        {
            var cut = Render<NotFound>();

            cut.Find("h1.text-muted").TextContent.Should().Be("404");
        }

        [Fact]
        public void RenderNotFoundMessage()
        {
            var cut = Render<NotFound>();

            cut.Find("h2").TextContent.Should().Be("Page not found");
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

            var link = cut.Find("a.btn-primary");
            link.GetAttribute("href").Should().Be("/");
            link.TextContent.Should().Contain("Back to Dashboard");
        }
    }
}
