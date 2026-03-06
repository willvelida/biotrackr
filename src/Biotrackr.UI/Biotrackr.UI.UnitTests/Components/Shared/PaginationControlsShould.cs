using Bunit;
using Biotrackr.UI.Components.Shared;
using FluentAssertions;

namespace Biotrackr.UI.UnitTests.Components.Shared
{
    public class PaginationControlsShould : BunitContext
    {
        [Fact]
        public void RenderNothing_WhenTotalPagesIsOne()
        {
            var cut = Render<PaginationControls>(parameters => parameters
                .Add(p => p.PageNumber, 1)
                .Add(p => p.TotalPages, 1)
                .Add(p => p.TotalCount, 5));

            cut.Markup.Trim().Should().BeEmpty();
        }

        [Fact]
        public void RenderPagination_WhenTotalPagesGreaterThanOne()
        {
            var cut = Render<PaginationControls>(parameters => parameters
                .Add(p => p.PageNumber, 1)
                .Add(p => p.TotalPages, 3)
                .Add(p => p.TotalCount, 60)
                .Add(p => p.HasPreviousPage, false)
                .Add(p => p.HasNextPage, true));

            cut.Find("nav").Should().NotBeNull();
            cut.Markup.Should().Contain("Page 1 of 3 (60 total records)");
        }

        [Fact]
        public void DisablePreviousButton_WhenNoPreviousPage()
        {
            var cut = Render<PaginationControls>(parameters => parameters
                .Add(p => p.PageNumber, 1)
                .Add(p => p.TotalPages, 3)
                .Add(p => p.TotalCount, 60)
                .Add(p => p.HasPreviousPage, false)
                .Add(p => p.HasNextPage, true));

            var prevItem = cut.Find("li.page-item");
            prevItem.ClassList.Should().Contain("disabled");
        }

        [Fact]
        public void DisableNextButton_WhenNoNextPage()
        {
            var cut = Render<PaginationControls>(parameters => parameters
                .Add(p => p.PageNumber, 3)
                .Add(p => p.TotalPages, 3)
                .Add(p => p.TotalCount, 60)
                .Add(p => p.HasPreviousPage, true)
                .Add(p => p.HasNextPage, false));

            var items = cut.FindAll("li.page-item");
            items.Last().ClassList.Should().Contain("disabled");
        }

        [Fact]
        public void HighlightCurrentPage()
        {
            var cut = Render<PaginationControls>(parameters => parameters
                .Add(p => p.PageNumber, 2)
                .Add(p => p.TotalPages, 3)
                .Add(p => p.TotalCount, 60)
                .Add(p => p.HasPreviousPage, true)
                .Add(p => p.HasNextPage, true));

            cut.FindAll("li.page-item.active").Should().HaveCount(1);
        }

        [Fact]
        public void InvokeOnPageChanged_WhenNextClicked()
        {
            int? changedPage = null;
            var cut = Render<PaginationControls>(parameters => parameters
                .Add(p => p.PageNumber, 1)
                .Add(p => p.TotalPages, 3)
                .Add(p => p.TotalCount, 60)
                .Add(p => p.HasPreviousPage, false)
                .Add(p => p.HasNextPage, true)
                .Add(p => p.OnPageChanged, Microsoft.AspNetCore.Components.EventCallback.Factory.Create<int>(this, p => changedPage = p)));

            var nextButton = cut.FindAll("button.page-link").Last();
            nextButton.Click();

            changedPage.Should().Be(2);
        }
    }
}
