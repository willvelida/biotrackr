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
            var cut = Render<DatePicker>();

            cut.Find(".rz-datepicker").Should().NotBeNull();
            cut.Markup.Should().NotContain(">Start<");
            cut.Markup.Should().NotContain(">End<");
        }

        [Fact]
        public void RenderDateRangeInputs_WhenModeIsRange()
        {
            var cut = Render<DatePicker>(parameters => parameters
                .Add(p => p.Mode, "range"));

            cut.Markup.Should().Contain("Start");
            cut.Markup.Should().Contain("End");
        }

        [Fact]
        public void RenderYesterdayButton()
        {
            var cut = Render<DatePicker>();

            cut.Markup.Should().Contain("Yesterday");
        }

        [Fact]
        public void RenderSingleDateMode_WithCorrectParameters()
        {
            var cut = Render<DatePicker>(parameters => parameters
                .Add(p => p.OnSingleDateSelected, Microsoft.AspNetCore.Components.EventCallback.Factory.Create<string>(this, _ => { })));

            cut.Find(".rz-datepicker").Should().NotBeNull();
            cut.Markup.Should().Contain("Single Date");
        }

        [Fact]
        public void RenderRangeMode_WithApplyButton()
        {
            var cut = Render<DatePicker>(parameters => parameters
                .Add(p => p.Mode, "range")
                .Add(p => p.StartDate, "2026-02-01")
                .Add(p => p.EndDate, "2026-02-28")
                .Add(p => p.OnDateRangeSelected, Microsoft.AspNetCore.Components.EventCallback.Factory.Create<(string, string)>(this, _ => { })));

            cut.Markup.Should().Contain("Apply");
            cut.Markup.Should().Contain("Start");
            cut.Markup.Should().Contain("End");
        }
    }
}
