using Bunit;
using Biotrackr.UI.Components.Pages;
using Biotrackr.UI.Components.Layout;
using FluentAssertions;

namespace Biotrackr.UI.UnitTests.Components.Pages
{
    public class LoginPageShould : BunitContext
    {
        [Fact]
        public void RenderPageTitle()
        {
            var cut = Render<Login>();

            cut.Find("h2").TextContent.Should().Be("Biotrackr");
        }

        [Fact]
        public void RenderSignInDescription()
        {
            var cut = Render<Login>();

            cut.Find("p.text-muted").TextContent.Should().Be("Sign in to view your health data dashboard");
        }

        [Fact]
        public void RenderSignInWithMicrosoftButton()
        {
            var cut = Render<Login>();

            var link = cut.Find("a.btn-primary");
            link.TextContent.Should().Contain("Sign in with Microsoft");
        }

        [Fact]
        public void HaveCorrectEasyAuthLoginUrl()
        {
            var cut = Render<Login>();

            var link = cut.Find("a.btn-primary");
            link.GetAttribute("href").Should().Be("/.auth/login/aad?post_login_redirect_uri=/");
        }

        [Fact]
        public void RenderBiotrackrIcon()
        {
            var cut = Render<Login>();

            cut.Find("svg").Should().NotBeNull();
        }

        [Fact]
        public void RenderCardLayout()
        {
            var cut = Render<Login>();

            cut.Find("div.card").Should().NotBeNull();
            cut.Find("div.card-body").Should().NotBeNull();
        }

        [Fact]
        public void UseLoginLayout()
        {
            // The Login page uses @layout LoginLayout - verify the component renders
            // within the expected structure (card inside a centered flex container)
            var cut = Render<Login>();

            cut.Find("div.d-flex.justify-content-center.align-items-center.vh-100").Should().NotBeNull();
        }
    }
}
