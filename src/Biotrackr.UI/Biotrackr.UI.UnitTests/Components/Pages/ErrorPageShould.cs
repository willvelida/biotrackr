using Bunit;
using Biotrackr.UI.Components.Pages;
using FluentAssertions;

namespace Biotrackr.UI.UnitTests.Components.Pages
{
    public class ErrorPageShould : BunitContext
    {
        [Fact]
        public void RenderErrorHeading()
        {
            var cut = Render<Error>();

            cut.Find("h2.text-danger").TextContent.Should().Be("Something went wrong");
        }

        [Fact]
        public void RenderErrorMessage()
        {
            var cut = Render<Error>();

            cut.Markup.Should().Contain("An unexpected error occurred");
        }

        [Fact]
        public void RenderBackToDashboardLink()
        {
            var cut = Render<Error>();

            var link = cut.Find("a.btn-primary");
            link.GetAttribute("href").Should().Be("/");
            link.TextContent.Should().Contain("Back to Dashboard");
        }

        [Fact]
        public void NotShowRequestId_WhenNoCascadingHttpContext()
        {
            var cut = Render<Error>();

            // Without HttpContext or Activity.Current, RequestId is null
            cut.Markup.Should().NotContain("Request ID:");
        }
    }
}
