using System.Text.Json;
using Biotrackr.Sleep.Api.Models.FitbitEntities;
using FluentAssertions;

namespace Biotrackr.Sleep.Api.UnitTests.ModelTests.FitbitEntityTests;

public class SummaryShould
{
    [Fact]
    public void Initialize_ShouldHaveDefaultValues_WhenCreatedWithDefaultConstructor()
    {
        // Arrange & Act
        var summary = new Summary();

        // Assert
        summary.Stages.Should().BeNull();
        summary.TotalMinutesAsleep.Should().Be(0);
        summary.TotalSleepRecords.Should().Be(0);
        summary.TotalTimeInBed.Should().Be(0);
    }

    [Fact]
    public void AllowPropertyAssignment_ShouldRetainValues_WhenPropertiesAreSet()
    {
        // Arrange & Act
        var summary = new Summary
        {
            Stages = new Stages { Deep = 90, Light = 200, Rem = 120, Wake = 30 },
            TotalMinutesAsleep = 450,
            TotalSleepRecords = 1,
            TotalTimeInBed = 480
        };

        // Assert
        summary.Stages.Should().NotBeNull();
        summary.Stages.Deep.Should().Be(90);
        summary.TotalMinutesAsleep.Should().Be(450);
        summary.TotalSleepRecords.Should().Be(1);
        summary.TotalTimeInBed.Should().Be(480);
    }

    [Fact]
    public void Deserialize_ShouldMapJsonPropertyNames_WhenDeserializedFromJson()
    {
        // Arrange
        var json = """
        {
            "stages": {"deep": 90, "light": 200, "rem": 120, "wake": 30},
            "totalMinutesAsleep": 450,
            "totalSleepRecords": 1,
            "totalTimeInBed": 480
        }
        """;

        // Act
        var summary = JsonSerializer.Deserialize<Summary>(json);

        // Assert
        summary.Should().NotBeNull();
        summary!.Stages.Should().NotBeNull();
        summary.Stages.Deep.Should().Be(90);
        summary.TotalMinutesAsleep.Should().Be(450);
        summary.TotalSleepRecords.Should().Be(1);
        summary.TotalTimeInBed.Should().Be(480);
    }
}
