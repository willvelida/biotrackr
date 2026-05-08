using System.Text.Json;
using Biotrackr.Sleep.Api.Models.FitbitEntities;
using FluentAssertions;

namespace Biotrackr.Sleep.Api.UnitTests.ModelTests.FitbitEntityTests;

public class SleepDataShould
{
    [Fact]
    public void Initialize_ShouldHaveDefaultValues_WhenCreatedWithDefaultConstructor()
    {
        // Arrange & Act
        var sleepData = new SleepData();

        // Assert
        sleepData.DateTime.Should().Be(default);
        sleepData.Level.Should().BeNull();
        sleepData.Seconds.Should().Be(0);
    }

    [Fact]
    public void AllowPropertyAssignment_ShouldRetainValues_WhenPropertiesAreSet()
    {
        // Arrange
        var dateTime = new DateTime(2026, 5, 7, 22, 30, 0);

        // Act
        var sleepData = new SleepData
        {
            DateTime = dateTime,
            Level = "deep",
            Seconds = 3600
        };

        // Assert
        sleepData.DateTime.Should().Be(dateTime);
        sleepData.Level.Should().Be("deep");
        sleepData.Seconds.Should().Be(3600);
    }

    [Fact]
    public void Deserialize_ShouldMapJsonPropertyNames_WhenDeserializedFromJson()
    {
        // Arrange
        var json = """{"dateTime": "2026-05-07T22:30:00", "level": "deep", "seconds": 3600}""";

        // Act
        var sleepData = JsonSerializer.Deserialize<SleepData>(json);

        // Assert
        sleepData.Should().NotBeNull();
        sleepData!.Level.Should().Be("deep");
        sleepData.Seconds.Should().Be(3600);
    }
}
