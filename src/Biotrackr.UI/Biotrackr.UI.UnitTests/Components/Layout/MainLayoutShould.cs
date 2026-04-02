using Bunit;
using Biotrackr.UI.Components.Layout;
using Biotrackr.UI.Models;
using Biotrackr.UI.Services;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Radzen;

namespace Biotrackr.UI.UnitTests.Components.Layout
{
    public class MainLayoutShould : BunitContext
    {
        private readonly Mock<IUserInfoService> _mockUserInfoService;
        private readonly Mock<IHttpContextAccessor> _mockHttpContextAccessor;

        public MainLayoutShould()
        {
            _mockUserInfoService = new Mock<IUserInfoService>();
            _mockHttpContextAccessor = new Mock<IHttpContextAccessor>();

            var httpContext = new DefaultHttpContext();
            _mockHttpContextAccessor.Setup(x => x.HttpContext).Returns(httpContext);
            _mockUserInfoService.Setup(x => x.GetUserInfo(It.IsAny<HttpContext>()))
                .Returns(new UserInfo { DisplayName = "Test User", Email = "test@example.com" });

            Services.AddSingleton(_mockUserInfoService.Object);
            Services.AddSingleton<IHttpContextAccessor>(_mockHttpContextAccessor.Object);
            Services.AddRadzenComponents();
            JSInterop.Mode = JSRuntimeMode.Loose;
        }

        [Fact]
        public void RenderBiotrackrTitle()
        {
            var cut = Render<MainLayout>(parameters => parameters
                .Add(p => p.Body, builder => builder.AddContent(0, "Test content")));

            cut.Markup.Should().Contain("Biotrackr");
        }

        [Fact]
        public void RenderUserDisplayName()
        {
            var cut = Render<MainLayout>(parameters => parameters
                .Add(p => p.Body, builder => builder.AddContent(0, "Test content")));

            cut.Markup.Should().Contain("Test User");
        }

        [Fact]
        public void RenderProfileMenu()
        {
            var cut = Render<MainLayout>(parameters => parameters
                .Add(p => p.Body, builder => builder.AddContent(0, "Test content")));

            cut.Markup.Should().Contain("Sign Out");
            cut.Markup.Should().Contain("Profile");
        }

        [Fact]
        public void CallUserInfoService_OnInitialized()
        {
            var cut = Render<MainLayout>(parameters => parameters
                .Add(p => p.Body, builder => builder.AddContent(0, "Test content")));

            _mockUserInfoService.Verify(x => x.GetUserInfo(It.IsAny<HttpContext>()), Times.Once);
        }

        [Fact]
        public void RenderBodyContent()
        {
            var cut = Render<MainLayout>(parameters => parameters
                .Add(p => p.Body, builder => builder.AddContent(0, "Hello from body")));

            cut.Markup.Should().Contain("Hello from body");
        }
    }
}
