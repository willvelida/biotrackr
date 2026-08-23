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
            // Act
            var cut = Render<Error>();

            // Assert
            cut.Markup.Should().Contain("Something went wrong");
        }

        [Fact]
        public void RenderErrorMessage()
        {
            // Act
            var cut = Render<Error>();

            // Assert
            cut.Markup.Should().Contain("An unexpected error occurred");
        }

        [Fact]
        public void RenderBackToDashboardLink()
        {
            // Act
            var cut = Render<Error>();

            // Assert
            cut.Markup.Should().Contain("Back to Dashboard");
        }

        [Fact]
        public void NotShowRequestId_WhenNoCascadingHttpContext()
        {
            // Act
            var cut = Render<Error>();

            // Without HttpContext or Activity.Current, RequestId is null

            // Assert
            cut.Markup.Should().NotContain("Request ID:");
        }
    }
}
