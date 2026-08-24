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
            // Act
            var cut = Render<Login>();

            // Assert
            cut.Markup.Should().Contain("Biotrackr");
        }

        [Fact]
        public void RenderSignInDescription()
        {
            // Act
            var cut = Render<Login>();

            // Assert
            cut.Markup.Should().Contain("Sign in to view your health data dashboard");
        }

        [Fact]
        public void RenderSignInWithMicrosoftButton()
        {
            // Act
            var cut = Render<Login>();

            // Assert
            cut.Markup.Should().Contain("Sign in with Microsoft");
        }

        [Fact]
        public void RenderBiotrackrIcon()
        {
            // Act
            var cut = Render<Login>();

            // Assert
            cut.Find("svg").Should().NotBeNull();
        }

        [Fact]
        public void RenderCardLayout()
        {
            // Act
            var cut = Render<Login>();

            // Assert
            cut.Markup.Should().Contain("rz-card");
        }

        [Fact]
        public void UseLoginLayout()
        {
            // The Login page renders within the LoginLayout

            // Act
            var cut = Render<Login>();

            // Assert
            cut.Markup.Should().NotBeEmpty();
        }

        [Fact]
        public void HaveCorrectEasyAuthLoginUrl()
        {
            // Act
            var cut = Render<Login>();

            // Assert
            var link = cut.Find("a[href='/.auth/login/aad?post_login_redirect_uri=/']");
            link.Should().NotBeNull();
            link.TextContent.Should().Contain("Sign in with Microsoft");
        }
    }
}
