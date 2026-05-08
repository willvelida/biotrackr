using FluentAssertions;
using System.Text.Json;
using Biotrackr.Mcp.Server.Models.Sleep;

namespace Biotrackr.Mcp.Server.UnitTests.Models;

public class SleepModelsShould
{
    [Fact]
    public void SleepRecord_ShouldSerializeAndDeserialize()
    {
        // Arrange
        var record = new SleepRecord
        {
            DateOfSleep = "2024-01-15",
            Duration = 28800000,
            Efficiency = 92,
            EndTime = new DateTime(2024, 1, 15, 7, 0, 0),
            InfoCode = 0,
            IsMainSleep = true,
            Levels = new SleepLevels(),
            LogId = 12345L,
            MinutesAfterWakeup = 5,
            MinutesAsleep = 420,
            MinutesAwake = 60,
            MinutesToFallAsleep = 15,
            LogType = "classic",
            StartTime = new DateTime(2024, 1, 14, 23, 0, 0),
            TimeInBed = 480,
            Type = "stages"
        };

        // Act
        var json = JsonSerializer.Serialize(record);
        var deserialized = JsonSerializer.Deserialize<SleepRecord>(json);

        // Assert
        deserialized.Should().NotBeNull();
        deserialized!.DateOfSleep.Should().Be("2024-01-15");
        deserialized.Duration.Should().Be(28800000);
        deserialized.Efficiency.Should().Be(92);
        deserialized.IsMainSleep.Should().BeTrue();
        deserialized.MinutesAfterWakeup.Should().Be(5);
        deserialized.MinutesAsleep.Should().Be(420);
        deserialized.MinutesAwake.Should().Be(60);
        deserialized.MinutesToFallAsleep.Should().Be(15);
        deserialized.TimeInBed.Should().Be(480);
    }

    [Fact]
    public void SleepLevelData_ShouldSerializeAndDeserialize()
    {
        // Arrange
        var data = new SleepLevelData
        {
            DateTime = new DateTime(2024, 1, 15, 1, 30, 0),
            Level = "deep",
            Seconds = 1800
        };

        // Act
        var json = JsonSerializer.Serialize(data);
        var deserialized = JsonSerializer.Deserialize<SleepLevelData>(json);

        // Assert
        deserialized.Should().NotBeNull();
        deserialized!.Level.Should().Be("deep");
        deserialized.Seconds.Should().Be(1800);
    }

    [Fact]
    public void SleepDetails_ShouldSerializeAndDeserialize()
    {
        // Arrange
        var details = new SleepDetails
        {
            Count = 3,
            Minutes = 90,
            ThirtyDayAvgMinutes = 85
        };

        // Act
        var json = JsonSerializer.Serialize(details);
        var deserialized = JsonSerializer.Deserialize<SleepDetails>(json);

        // Assert
        deserialized.Should().NotBeNull();
        deserialized!.Count.Should().Be(3);
        deserialized.Minutes.Should().Be(90);
        deserialized.ThirtyDayAvgMinutes.Should().Be(85);
    }

    [Fact]
    public void SleepLevels_ShouldSerializeAndDeserialize()
    {
        // Arrange
        var levels = new SleepLevels
        {
            Data = [new SleepLevelData { Level = "light", Seconds = 600 }],
            ShortData = [new SleepLevelData { Level = "wake", Seconds = 120 }],
            Summary = new SleepSummary()
        };

        // Act
        var json = JsonSerializer.Serialize(levels);
        var deserialized = JsonSerializer.Deserialize<SleepLevels>(json);

        // Assert
        deserialized.Should().NotBeNull();
        deserialized!.Summary.Should().NotBeNull();
    }
}
