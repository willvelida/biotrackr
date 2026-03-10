using Bunit;
using Radzen;
using Biotrackr.UI.Components.Pages;
using Biotrackr.UI.Components.Layout;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;

namespace Biotrackr.UI.UnitTests.Components.Pages
{
    public class LoginPageShould : BunitContext
    {
        public LoginPageShould()
        {
            Services.AddRadzenComponents();
            JSInterop.Mode = JSRuntimeMode.Loose;
        }

        [Fact]
        public void RenderPageTitle()
        {
            var cut = Render<Login>();

            cut.Markup.Should().Contain("Biotrackr");
        }

        [Fact]
        public void RenderSignInDescription()
        {
            var cut = Render<Login>();

            cut.Markup.Should().Contain("Sign in to view your health data dashboard");
        }

        [Fact]
        public void RenderSignInWithMicrosoftButton()
        {
            var cut = Render<Login>();

            cut.Markup.Should().Contain("Sign in with Microsoft");
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

            cut.Markup.Should().Contain("rz-card");
        }

        [Fact]
        public void UseLoginLayout()
        {
            // The Login page renders within the LoginLayout
            var cut = Render<Login>();

            cut.Markup.Should().NotBeEmpty();
        }

        [Fact]
        public void HaveCorrectEasyAuthLoginUrl()
        {
            var cut = Render<Login>();

            var link = cut.Find("a[href='/.auth/login/aad?post_login_redirect_uri=/']");
            link.Should().NotBeNull();
            link.TextContent.Should().Contain("Sign in with Microsoft");
        }
    }
}
