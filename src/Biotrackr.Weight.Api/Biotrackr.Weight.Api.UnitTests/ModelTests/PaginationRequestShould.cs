using Biotrackr.Weight.Api.Models;
using FluentAssertions;

namespace Biotrackr.Weight.Api.UnitTests.ModelTests
{
    public class PaginationRequestShould
    {
        [Fact]
        public void SetDefaultValues_WhenCreated()
        {
            // Act
            var request = new PaginationRequest();

            // Assert
            request.PageNumber.Should().Be(1);
            request.PageSize.Should().Be(20);
            request.Skip.Should().Be(0);
        }

        [Theory]
        [InlineData(0, 1)]
        [InlineData(-1, 1)]
        [InlineData(-10, 1)]
        public void SetPageNumberToOne_WhenInvalidValueProvided(int invalidPageNumber, int expectedPageNumber)
        {
            // Act
            var request = new PaginationRequest { PageNumber = invalidPageNumber };

            // Assert
            request.PageNumber.Should().Be(expectedPageNumber);
        }

        [Theory]
        [InlineData(0, 20)]
        [InlineData(-1, 20)]
        [InlineData(101, 100)]
        [InlineData(200, 100)]
        public void ClampPageSize_WhenInvalidValueProvided(int invalidPageSize, int expectedPageSize)
        {
            // Act
            var request = new PaginationRequest { PageSize = invalidPageSize };

            // Assert
            request.PageSize.Should().Be(expectedPageSize);
        }

        [Theory]
        [InlineData(1, 20, 0)]
        [InlineData(2, 20, 20)]
        [InlineData(3, 15, 30)]
        [InlineData(5, 10, 40)]
        public void CalculateCorrectSkipValue(int pageNumber, int pageSize, int expectedSkip)
        {
            // Act
            var request = new PaginationRequest
            {
                PageNumber = pageNumber,
                PageSize = pageSize
            };

            // Assert
            request.Skip.Should().Be(expectedSkip);
        }
    }
}
