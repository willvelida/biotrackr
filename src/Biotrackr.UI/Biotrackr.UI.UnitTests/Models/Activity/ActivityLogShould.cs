using FluentAssertions;
using System.Text.Json;
using Biotrackr.UI.Models.Activity;

namespace Biotrackr.UI.UnitTests.Models.Activity;

public class ActivityLogShould
{
    [Fact]
    public void ShouldSerializeAndDeserialize()
    {
        // Arrange
        var log = new ActivityLog
        {
            ActivityId = 1,
            ActivityParentId = 2,
            ActivityParentName = "Running",
            Calories = 350,
            Description = "Morning run",
            Duration = 1800000,
            HasActiveZoneMinutes = true,
            HasStartTime = true,
            IsFavorite = false,
            LastModified = new DateTime(2024, 1, 15, 10, 0, 0),
            LogId = 99999,
            Name = "Run",
            StartDate = "2024-01-15",
            StartTime = "07:30",
            Steps = 5000,
            Distance = 4.2
        };

        // Act
        var json = JsonSerializer.Serialize(log);
        var result = JsonSerializer.Deserialize<ActivityLog>(json);

        // Assert
        result.Should().NotBeNull();
        result!.ActivityId.Should().Be(1);
        result.ActivityParentId.Should().Be(2);
        result.ActivityParentName.Should().Be("Running");
        result.Calories.Should().Be(350);
        result.Description.Should().Be("Morning run");
        result.Duration.Should().Be(1800000);
        result.HasActiveZoneMinutes.Should().BeTrue();
        result.HasStartTime.Should().BeTrue();
        result.IsFavorite.Should().BeFalse();
        result.LogId.Should().Be(99999);
        result.Name.Should().Be("Run");
        result.StartDate.Should().Be("2024-01-15");
        result.StartTime.Should().Be("07:30");
        result.Steps.Should().Be(5000);
        result.Distance.Should().Be(4.2);
    }
}
