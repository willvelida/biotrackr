using Bunit;
using Biotrackr.UI.Components.Shared;
using FluentAssertions;

namespace Biotrackr.UI.UnitTests.Components.Shared
{
    public class DatePickerShould : BunitContext
    {
        [Fact]
        public void RenderInSingleDateMode_ByDefault()
        {
            var cut = Render<DatePicker>();

            cut.Find("#singleDate").Should().NotBeNull();
            cut.FindAll("#startDate").Should().BeEmpty();
        }

        [Fact]
        public void RenderDateRangeInputs_WhenModeIsRange()
        {
            var cut = Render<DatePicker>(parameters => parameters
                .Add(p => p.Mode, "range"));

            cut.Find("#startDate").Should().NotBeNull();
            cut.Find("#endDate").Should().NotBeNull();
            cut.FindAll("#singleDate").Should().BeEmpty();
        }

        [Fact]
        public void RenderYesterdayButton()
        {
            var cut = Render<DatePicker>();

            cut.Find("button.btn-outline-secondary").TextContent.Trim().Should().Be("Yesterday");
        }

        [Fact]
        public void InvokeOnSingleDateSelected_WhenDateChanged()
        {
            string? selectedDate = null;
            var cut = Render<DatePicker>(parameters => parameters
                .Add(p => p.OnSingleDateSelected, Microsoft.AspNetCore.Components.EventCallback.Factory.Create<string>(this, d => selectedDate = d)));

            cut.Find("#singleDate").Change("2026-03-01");

            selectedDate.Should().Be("2026-03-01");
        }

        [Fact]
        public void InvokeOnDateRangeSelected_WhenApplyClicked()
        {
            (string Start, string End)? selectedRange = null;
            var cut = Render<DatePicker>(parameters => parameters
                .Add(p => p.Mode, "range")
                .Add(p => p.StartDate, "2026-02-01")
                .Add(p => p.EndDate, "2026-02-28")
                .Add(p => p.OnDateRangeSelected, Microsoft.AspNetCore.Components.EventCallback.Factory.Create<(string, string)>(this, r => selectedRange = r)));

            cut.Find("button.btn-primary").Click();

            selectedRange.Should().NotBeNull();
            selectedRange!.Value.Start.Should().Be("2026-02-01");
            selectedRange!.Value.End.Should().Be("2026-02-28");
        }
    }
}
