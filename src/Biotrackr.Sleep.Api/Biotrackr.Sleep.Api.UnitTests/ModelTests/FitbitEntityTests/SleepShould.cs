using System.Text.Json;
using FluentAssertions;
using SleepModel = Biotrackr.Sleep.Api.Models.FitbitEntities.Sleep;

namespace Biotrackr.Sleep.Api.UnitTests.ModelTests.FitbitEntityTests;

public class SleepShould
{
    [Fact]
    public void Initialize_ShouldHaveDefaultValues_WhenCreatedWithDefaultConstructor()
    {
        // Arrange & Act
        var sleep = new SleepModel();

        // Assert
        sleep.DateOfSleep.Should().BeNull();
        sleep.Duration.Should().Be(0);
        sleep.Efficiency.Should().Be(0);
        sleep.InfoCode.Should().Be(0);
        sleep.IsMainSleep.Should().BeFalse();
        sleep.Levels.Should().BeNull();
        sleep.LogId.Should().Be(0);
        sleep.MinutesAfterWakeup.Should().Be(0);
        sleep.MinutesAsleep.Should().Be(0);
        sleep.MinutesAwake.Should().Be(0);
        sleep.MinutesToFallAsleep.Should().Be(0);
        sleep.LogType.Should().BeNull();
        sleep.TimeInBed.Should().Be(0);
        sleep.Type.Should().BeNull();
    }

    [Fact]
    public void AllowPropertyAssignment_ShouldRetainValues_WhenPropertiesAreSet()
    {
        // Arrange
        var startTime = new DateTime(2026, 5, 7, 22, 30, 0);
        var endTime = new DateTime(2026, 5, 8, 6, 30, 0);

        // Act
        var sleep = new SleepModel
        {
            DateOfSleep = "2026-05-07",
            Duration = 28800000,
            Efficiency = 95,
            EndTime = endTime,
            InfoCode = 0,
            IsMainSleep = true,
            LogId = 12345678L,
            MinutesAfterWakeup = 5,
            MinutesAsleep = 450,
            MinutesAwake = 30,
            MinutesToFallAsleep = 10,
            LogType = "auto_detected",
            StartTime = startTime,
            TimeInBed = 480,
            Type = "stages"
        };

        // Assert
        sleep.DateOfSleep.Should().Be("2026-05-07");
        sleep.Duration.Should().Be(28800000);
        sleep.Efficiency.Should().Be(95);
        sleep.EndTime.Should().Be(endTime);
        sleep.IsMainSleep.Should().BeTrue();
        sleep.LogId.Should().Be(12345678L);
        sleep.MinutesAsleep.Should().Be(450);
        sleep.MinutesAwake.Should().Be(30);
        sleep.MinutesToFallAsleep.Should().Be(10);
        sleep.LogType.Should().Be("auto_detected");
        sleep.StartTime.Should().Be(startTime);
        sleep.TimeInBed.Should().Be(480);
        sleep.Type.Should().Be("stages");
    }

    [Fact]
    public void Serialize_ShouldUseJsonPropertyNames_WhenSerializedToJson()
    {
        // Arrange
        var sleep = new SleepModel
        {
            DateOfSleep = "2026-05-07",
            Duration = 28800000,
            Efficiency = 95,
            IsMainSleep = true,
            LogId = 12345678L
        };

        // Act
        var json = JsonSerializer.Serialize(sleep);

        // Assert
        json.Should().Contain("\"dateOfSleep\"");
        json.Should().Contain("\"duration\"");
        json.Should().Contain("\"efficiency\"");
        json.Should().Contain("\"isMainSleep\"");
        json.Should().Contain("\"logId\"");
    }

    [Fact]
    public void Deserialize_ShouldMapJsonPropertyNames_WhenDeserializedFromJson()
    {
        // Arrange
        var json = """
        {
            "dateOfSleep": "2026-05-07",
            "duration": 28800000,
            "efficiency": 95,
            "isMainSleep": true,
            "logId": 12345678,
            "minutesAsleep": 450,
            "minutesAwake": 30,
            "logType": "auto_detected",
            "type": "stages"
        }
        """;

        // Act
        var sleep = JsonSerializer.Deserialize<SleepModel>(json);

        // Assert
        sleep.Should().NotBeNull();
        sleep!.DateOfSleep.Should().Be("2026-05-07");
        sleep.Duration.Should().Be(28800000);
        sleep.Efficiency.Should().Be(95);
        sleep.IsMainSleep.Should().BeTrue();
        sleep.LogId.Should().Be(12345678L);
        sleep.MinutesAsleep.Should().Be(450);
        sleep.LogType.Should().Be("auto_detected");
    }
}
