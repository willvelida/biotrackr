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
            var cut = Render<NavMenu>();

            cut.Markup.Should().Contain("Dashboard");
        }

        [Fact]
        public void RenderActivityLink()
        {
            var cut = Render<NavMenu>();

            cut.Markup.Should().Contain("Activity");
        }

        [Fact]
        public void RenderFoodLink()
        {
            var cut = Render<NavMenu>();

            cut.Markup.Should().Contain("Food");
        }

        [Fact]
        public void RenderSleepLink()
        {
            var cut = Render<NavMenu>();

            cut.Markup.Should().Contain("Sleep");
        }

        [Fact]
        public void RenderWeightLink()
        {
            var cut = Render<NavMenu>();

            cut.Markup.Should().Contain("Weight");
        }

        [Fact]
        public void NotRenderSignOutLink_WhenMovedToProfileDropdown()
        {
            var cut = Render<NavMenu>();

            cut.Markup.Should().NotContain("Sign Out");
        }
    }
}
