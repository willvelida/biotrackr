using System.Text.Json;
using Biotrackr.Sleep.Api.Models.FitbitEntities;
using FluentAssertions;

namespace Biotrackr.Sleep.Api.UnitTests.ModelTests.FitbitEntityTests;

public class LevelsShould
{
    [Fact]
    public void Initialize_ShouldHaveDefaultValues_WhenCreatedWithDefaultConstructor()
    {
        // Arrange & Act
        var levels = new Levels();

        // Assert
        levels.Data.Should().BeNull();
        levels.ShortData.Should().BeNull();
        levels.Summary.Should().BeNull();
    }

    [Fact]
    public void AllowPropertyAssignment_ShouldRetainValues_WhenPropertiesAreSet()
    {
        // Arrange
        var data = new List<SleepData> { new() { Level = "deep", Seconds = 3600 } };
        var shortData = new List<SleepData> { new() { Level = "wake", Seconds = 60 } };
        var summary = new Summary { TotalMinutesAsleep = 450 };

        // Act
        var levels = new Levels
        {
            Data = data,
            ShortData = shortData,
            Summary = summary
        };

        // Assert
        levels.Data.Should().HaveCount(1);
        levels.Data[0].Level.Should().Be("deep");
        levels.ShortData.Should().HaveCount(1);
        levels.Summary.TotalMinutesAsleep.Should().Be(450);
    }

    [Fact]
    public void Deserialize_ShouldMapJsonPropertyNames_WhenDeserializedFromJson()
    {
        // Arrange
        var json = """
        {
            "data": [{"dateTime": "2026-05-07T22:30:00", "level": "deep", "seconds": 3600}],
            "shortData": [{"dateTime": "2026-05-08T01:00:00", "level": "wake", "seconds": 60}],
            "summary": {"totalMinutesAsleep": 450, "totalSleepRecords": 1, "totalTimeInBed": 480}
        }
        """;

        // Act
        var levels = JsonSerializer.Deserialize<Levels>(json);

        // Assert
        levels.Should().NotBeNull();
        levels!.Data.Should().HaveCount(1);
        levels.Data[0].Level.Should().Be("deep");
        levels.ShortData.Should().HaveCount(1);
        levels.Summary.Should().NotBeNull();
        levels.Summary.TotalMinutesAsleep.Should().Be(450);
    }
}
