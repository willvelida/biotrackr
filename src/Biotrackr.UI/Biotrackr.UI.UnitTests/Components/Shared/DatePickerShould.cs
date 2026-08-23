using Bunit;
using Biotrackr.UI.Components.Shared;
using FluentAssertions;
using Radzen;

namespace Biotrackr.UI.UnitTests.Components.Shared
{
    public class DatePickerShould : BunitContext
    {
        public DatePickerShould()
        {
            Services.AddRadzenComponents();
            JSInterop.Mode = JSRuntimeMode.Loose;
        }

        [Fact]
        public void RenderInSingleDateMode_ByDefault()
        {
            // Act
            var cut = Render<DatePicker>();

            // Assert
            cut.Find(".rz-datepicker").Should().NotBeNull();
            cut.Markup.Should().NotContain(">Start<");
            cut.Markup.Should().NotContain(">End<");
        }

        [Fact]
        public void RenderDateRangeInputs_WhenModeIsRange()
        {
            // Act
            var cut = Render<DatePicker>(parameters => parameters
                .Add(p => p.Mode, "range"));

            // Assert
            cut.Markup.Should().Contain("Start");
            cut.Markup.Should().Contain("End");
        }

        [Fact]
        public void RenderYesterdayButton()
        {
            // Act
            var cut = Render<DatePicker>();

            // Assert
            cut.Markup.Should().Contain("Yesterday");
        }

        [Fact]
        public void RenderSingleDateMode_WithCorrectParameters()
        {
            // Act
            var cut = Render<DatePicker>(parameters => parameters
                .Add(p => p.OnSingleDateSelected, Microsoft.AspNetCore.Components.EventCallback.Factory.Create<string>(this, _ => { })));

            // Assert
            cut.Find(".rz-datepicker").Should().NotBeNull();
            cut.Markup.Should().Contain("Single Date");
        }

        [Fact]
        public void RenderRangeMode_WithApplyButton()
        {
            // Act
            var cut = Render<DatePicker>(parameters => parameters
                .Add(p => p.Mode, "range")
                .Add(p => p.StartDate, "2026-02-01")
                .Add(p => p.EndDate, "2026-02-28")
                .Add(p => p.OnDateRangeSelected, Microsoft.AspNetCore.Components.EventCallback.Factory.Create<(string, string)>(this, _ => { })));

            // Assert
            cut.Markup.Should().Contain("Apply");
            cut.Markup.Should().Contain("Start");
            cut.Markup.Should().Contain("End");
        }

        [Fact]
        public void RenderModeToggle_WithBothOptions()
        {
            // Act
            var cut = Render<DatePicker>();

            // Assert
            cut.Markup.Should().Contain("Single Date");
            cut.Markup.Should().Contain("Date Range");
        }

        [Fact]
        public void RenderWithCustomSelectedDate()
        {
            // Act
            var cut = Render<DatePicker>(parameters => parameters
                .Add(p => p.SelectedDate, "2026-01-15"));

            // Assert
            cut.Find(".rz-datepicker").Should().NotBeNull();
        }

        [Fact]
        public void RenderRangeMode_WithCustomDates()
        {
            // Act
            var cut = Render<DatePicker>(parameters => parameters
                .Add(p => p.Mode, "range")
                .Add(p => p.StartDate, "2026-01-01")
                .Add(p => p.EndDate, "2026-01-31"));

            // Assert
            cut.Markup.Should().Contain("Start");
            cut.Markup.Should().Contain("End");
            cut.Find(".rz-datepicker").Should().NotBeNull();
        }
    }
}
