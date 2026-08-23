using Bunit;
using Radzen;
using Biotrackr.UI.Components.Layout;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;

namespace Biotrackr.UI.UnitTests.Components.Layout
{
    public class NavMenuShould : BunitContext
    {
        public NavMenuShould()
        {
            Services.AddRadzenComponents();
            JSInterop.Mode = JSRuntimeMode.Loose;
        }

        [Fact]
        public void RenderDashboardLink()
        {
            // Act
            var cut = Render<NavMenu>();

            // Assert
            cut.Markup.Should().Contain("Dashboard");
        }

        [Fact]
        public void RenderActivityLink()
        {
            // Act
            var cut = Render<NavMenu>();

            // Assert
            cut.Markup.Should().Contain("Activity");
        }

        [Fact]
        public void RenderFoodLink()
        {
            // Act
            var cut = Render<NavMenu>();

            // Assert
            cut.Markup.Should().Contain("Food");
        }

        [Fact]
        public void RenderSleepLink()
        {
            // Act
            var cut = Render<NavMenu>();

            // Assert
            cut.Markup.Should().Contain("Sleep");
        }

        [Fact]
        public void RenderVitalsLink()
        {
            // Act
            var cut = Render<NavMenu>();

            // Assert
            cut.Markup.Should().Contain("Vitals");
        }

        [Fact]
        public void NotRenderSignOutLink_WhenMovedToProfileDropdown()
        {
            // Act
            var cut = Render<NavMenu>();

            // Assert
            cut.Markup.Should().NotContain("Sign Out");
        }
    }
}
