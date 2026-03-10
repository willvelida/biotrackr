using Bunit;
using Radzen;
using Biotrackr.UI.Components.Pages;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;

namespace Biotrackr.UI.UnitTests.Components.Pages
{
    public class ErrorPageShould : BunitContext
    {
        public ErrorPageShould()
        {
            Services.AddRadzenComponents();
            JSInterop.Mode = JSRuntimeMode.Loose;
        }

        [Fact]
        public void RenderErrorHeading()
        {
            var cut = Render<Error>();

            cut.Markup.Should().Contain("Something went wrong");
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

            cut.Markup.Should().Contain("Back to Dashboard");
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
