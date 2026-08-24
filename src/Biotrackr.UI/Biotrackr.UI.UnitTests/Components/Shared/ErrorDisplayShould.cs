using Bunit;
using Biotrackr.UI.Components.Shared;
using FluentAssertions;
using Radzen;

namespace Biotrackr.UI.UnitTests.Components.Shared
{
    public class ErrorDisplayShould : BunitContext
    {
        public ErrorDisplayShould()
        {
            Services.AddRadzenComponents();
        }

        [Fact]
        public void RenderAlert_WhenMessageIsProvided()
        {
            // Act
            var cut = Render<ErrorDisplay>(parameters => parameters
                .Add(p => p.Message, "Something went wrong"));

            // Assert
            cut.Find(".rz-alert").Should().NotBeNull();
            cut.Markup.Should().Contain("Something went wrong");
        }

        [Fact]
        public void RenderNothing_WhenMessageIsNull()
        {
            // Act
            var cut = Render<ErrorDisplay>(parameters => parameters
                .Add(p => p.Message, null));

            // Assert
            cut.Markup.Trim().Should().BeEmpty();
        }

        [Fact]
        public void RenderNothing_WhenMessageIsEmpty()
        {
            // Act
            var cut = Render<ErrorDisplay>(parameters => parameters
                .Add(p => p.Message, ""));

            // Assert
            cut.Markup.Trim().Should().BeEmpty();
        }
    }
}
