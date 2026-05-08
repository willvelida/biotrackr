using System.Text.Json;
using Biotrackr.Sleep.Api.Models.FitbitEntities;
using FluentAssertions;
using SleepModel = Biotrackr.Sleep.Api.Models.FitbitEntities.Sleep;

namespace Biotrackr.Sleep.Api.UnitTests.ModelTests.FitbitEntityTests;

public class SleepResponseShould
{
    [Fact]
    public void Initialize_ShouldHaveDefaultValues_WhenCreatedWithDefaultConstructor()
    {
        // Arrange & Act
        var response = new SleepResponse();

        // Assert
        response.Sleep.Should().BeNull();
        response.Summary.Should().BeNull();
    }

    [Fact]
    public void AllowPropertyAssignment_ShouldRetainValues_WhenPropertiesAreSet()
    {
        // Arrange
        var sleepList = new List<SleepModel>
        {
            new() { DateOfSleep = "2026-05-07", Duration = 28800000, IsMainSleep = true }
        };
        var summary = new Summary { TotalMinutesAsleep = 450, TotalSleepRecords = 1, TotalTimeInBed = 480 };

        // Act
        var response = new SleepResponse
        {
            Sleep = sleepList,
            Summary = summary
        };

        // Assert
        response.Sleep.Should().HaveCount(1);
        response.Sleep[0].DateOfSleep.Should().Be("2026-05-07");
        response.Summary.TotalMinutesAsleep.Should().Be(450);
    }

    [Fact]
    public void Deserialize_ShouldMapJsonPropertyNames_WhenDeserializedFromJson()
    {
        // Arrange
        var json = """
        {
            "sleep": [{"dateOfSleep": "2026-05-07", "duration": 28800000, "isMainSleep": true, "efficiency": 95, "infoCode": 0, "logId": 12345, "minutesAfterWakeup": 5, "minutesAsleep": 450, "minutesAwake": 30, "minutesToFallAsleep": 10, "logType": "auto_detected", "timeInBed": 480, "type": "stages"}],
            "summary": {"totalMinutesAsleep": 450, "totalSleepRecords": 1, "totalTimeInBed": 480}
        }
        """;

        // Act
        var response = JsonSerializer.Deserialize<SleepResponse>(json);

        // Assert
        response.Should().NotBeNull();
        response!.Sleep.Should().HaveCount(1);
        response.Sleep[0].DateOfSleep.Should().Be("2026-05-07");
        response.Summary.Should().NotBeNull();
    }
}
