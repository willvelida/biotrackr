using Biotrackr.Sleep.Api.Models.FitbitEntities;
using FluentAssertions;
using Xunit;

namespace Biotrackr.Sleep.Api.UnitTests.ModelTests;

public class SleepDetailsShould
{
    [Fact]
    public void Initialize_WithDefaultValues()
    {
        // Arrange & Act
        var sleepDetails = new SleepDetails();

        // Assert
        sleepDetails.Count.Should().Be(0);
        sleepDetails.Minutes.Should().Be(0);
        sleepDetails.ThirtyDayAvgMinutes.Should().Be(0);
    }

    [Fact]
    public void AllowPropertyAssignment()
    {
        // Arrange
        var sleepDetails = new SleepDetails();

        // Act
        sleepDetails.Count = 5;
        sleepDetails.Minutes = 480;
        sleepDetails.ThirtyDayAvgMinutes = 450;

        // Assert
        sleepDetails.Count.Should().Be(5);
        sleepDetails.Minutes.Should().Be(480);
        sleepDetails.ThirtyDayAvgMinutes.Should().Be(450);
    }

    [Fact]
    public void SupportPropertyInitialization()
    {
        // Arrange & Act
        var sleepDetails = new SleepDetails
        {
            Count = 10,
            Minutes = 540,
            ThirtyDayAvgMinutes = 500
        };

        // Assert
        sleepDetails.Count.Should().Be(10);
        sleepDetails.Minutes.Should().Be(540);
        sleepDetails.ThirtyDayAvgMinutes.Should().Be(500);
    }
}
