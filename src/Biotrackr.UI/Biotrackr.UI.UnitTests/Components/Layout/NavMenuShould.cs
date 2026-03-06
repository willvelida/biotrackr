using Bunit;
using Biotrackr.UI.Components.Layout;
using FluentAssertions;

namespace Biotrackr.UI.UnitTests.Components.Layout
{
    public class NavMenuShould : BunitContext
    {
        [Fact]
        public void RenderBrandName()
        {
            var cut = Render<NavMenu>();

            cut.Find("a.navbar-brand").TextContent.Should().Be("Biotrackr");
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
        public void RenderSignOutLink()
        {
            var cut = Render<NavMenu>();

            var signOutLink = cut.Find("a[href='/.auth/logout?post_logout_redirect_uri=/login']");
            signOutLink.TextContent.Should().Contain("Sign Out");
        }

        [Fact]
        public void RenderSignOutWithLogoutIcon()
        {
            var cut = Render<NavMenu>();

            cut.Find("span.bi-logout-nav-menu").Should().NotBeNull();
        }

        [Fact]
        public void RenderSeparatorBeforeSignOut()
        {
            var cut = Render<NavMenu>();

            cut.Find("hr").Should().NotBeNull();
        }
    }
}
