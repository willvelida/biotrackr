using Biotrackr.Weight.Api.Models;
using FluentAssertions;

namespace Biotrackr.Weight.Api.UnitTests.ModelTests
{
    public class PaginationResponseShould
    {
        [Theory]
        [InlineData(50, 20, 3)]  // 50 total, 20 per page = 3 pages
        [InlineData(100, 20, 5)] // 100 total, 20 per page = 5 pages
        [InlineData(19, 20, 1)]  // 19 total, 20 per page = 1 page
        [InlineData(0, 20, 0)]   // 0 total = 0 pages
        public void CalculateCorrectTotalPages(int totalCount, int pageSize, int expectedTotalPages)
        {
            // Act
            var response = new PaginationResponse<string>
            {
                TotalCount = totalCount,
                PageSize = pageSize
            };

            // Assert
            response.TotalPages.Should().Be(expectedTotalPages);
        }

        [Theory]
        [InlineData(1, 20, 100, false)] // First page
        [InlineData(2, 20, 100, true)]  // Middle page
        [InlineData(5, 20, 100, true)]  // Last page
        public void DetermineHasPreviousPageCorrectly(int pageNumber, int pageSize, int totalCount, bool expectedHasPrevious)
        {
            // Act
            var response = new PaginationResponse<string>
            {
                PageNumber = pageNumber,
                PageSize = pageSize,
                TotalCount = totalCount
            };

            // Assert
            response.HasPreviousPage.Should().Be(expectedHasPrevious);
        }

        [Theory]
        [InlineData(1, 20, 100, true)]  // First page, more pages available
        [InlineData(4, 20, 100, true)]  // Middle page, more pages available
        [InlineData(5, 20, 100, false)] // Last page, no more pages
        [InlineData(1, 20, 15, false)]  // Only page
        public void DetermineHasNextPageCorrectly(int pageNumber, int pageSize, int totalCount, bool expectedHasNext)
        {
            // Act
            var response = new PaginationResponse<string>
            {
                PageNumber = pageNumber,
                PageSize = pageSize,
                TotalCount = totalCount
            };

            // Assert
            response.HasNextPage.Should().Be(expectedHasNext);
        }
    }
}
