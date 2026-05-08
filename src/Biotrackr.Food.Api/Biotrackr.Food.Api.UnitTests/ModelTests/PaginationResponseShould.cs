using Biotrackr.Food.Api.Models;
using FluentAssertions;

namespace Biotrackr.Food.Api.UnitTests.ModelTests
{
    public class PaginationResponseShould
    {
        [Theory]
        [InlineData(50, 20, 3)]
        [InlineData(100, 20, 5)]
        [InlineData(19, 20, 1)]
        [InlineData(0, 20, 0)]
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
        [InlineData(1, 20, 100, false)]
        [InlineData(2, 20, 100, true)]
        [InlineData(5, 20, 100, true)]
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
        [InlineData(1, 20, 100, true)]
        [InlineData(4, 20, 100, true)]
        [InlineData(5, 20, 100, false)]
        [InlineData(1, 20, 15, false)]
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

        [Fact]
        public void InitializeWithEmptyItemsList()
        {
            // Act
            var response = new PaginationResponse<FoodDocument>();

            // Assert
            response.Items.Should().NotBeNull();
            response.Items.Should().BeEmpty();
            response.TotalCount.Should().Be(0);
            response.PageNumber.Should().Be(0);
            response.PageSize.Should().Be(0);
        }
    }
}
