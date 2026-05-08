using FluentAssertions;
using System.Text.Json;
using Biotrackr.UI.Models.Sleep;

namespace Biotrackr.UI.UnitTests.Models.Sleep;

public class SleepModelsShould
{
    [Fact]
    public void SleepDetails_ShouldRoundTrip()
    {
        // Arrange
        var details = new SleepDetails
        {
            Count = 4,
            Minutes = 95,
            ThirtyDayAvgMinutes = 88
        };

        // Act
        var json = JsonSerializer.Serialize(details);
        var result = JsonSerializer.Deserialize<SleepDetails>(json);

        // Assert
        result.Should().NotBeNull();
        result!.Count.Should().Be(4);
        result.Minutes.Should().Be(95);
        result.ThirtyDayAvgMinutes.Should().Be(88);
    }

    [Fact]
    public void SleepLevelData_ShouldRoundTrip()
    {
        // Arrange
        var data = new SleepLevelData
        {
            DateTime = new DateTime(2024, 3, 10, 2, 15, 0),
            Level = "rem",
            Seconds = 2400
        };

        // Act
        var json = JsonSerializer.Serialize(data);
        var result = JsonSerializer.Deserialize<SleepLevelData>(json);

        // Assert
        result.Should().NotBeNull();
        result!.Level.Should().Be("rem");
        result.Seconds.Should().Be(2400);
        result.DateTime.Should().Be(new DateTime(2024, 3, 10, 2, 15, 0));
    }
}
