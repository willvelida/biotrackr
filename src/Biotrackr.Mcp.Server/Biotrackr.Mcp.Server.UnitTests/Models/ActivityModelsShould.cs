using AutoFixture;
using FluentAssertions;
using System.Text.Json;
using Biotrackr.Mcp.Server.Models.Activity;

namespace Biotrackr.Mcp.Server.UnitTests.Models;

public class ActivityModelsShould
{
    private readonly Fixture _fixture = new();

    [Fact]
    public void ActivityLog_ShouldSerializeAndDeserialize()
    {
        // Arrange
        var log = _fixture.Create<ActivityLog>();

        // Act
        var json = JsonSerializer.Serialize(log);
        var deserialized = JsonSerializer.Deserialize<ActivityLog>(json);

        // Assert
        deserialized.Should().NotBeNull();
        deserialized!.ActivityParentName.Should().Be(log.ActivityParentName);
        deserialized.Calories.Should().Be(log.Calories);
        deserialized.Description.Should().Be(log.Description);
        deserialized.Duration.Should().Be(log.Duration);
        deserialized.Name.Should().Be(log.Name);
        deserialized.StartDate.Should().Be(log.StartDate);
        deserialized.StartTime.Should().Be(log.StartTime);
        deserialized.Steps.Should().Be(log.Steps);
        deserialized.Distance.Should().Be(log.Distance);
    }

    [Fact]
    public void ActivityResponse_ShouldSerializeAndDeserialize()
    {
        // Arrange
        var response = new ActivityResponse
        {
            Items = [new ActivityItem()],
            TotalCount = 10,
            PageNumber = 1,
            PageSize = 5,
            TotalPages = 2,
            HasPreviousPage = false,
            HasNextPage = true
        };

        // Act
        var json = JsonSerializer.Serialize(response);
        var deserialized = JsonSerializer.Deserialize<ActivityResponse>(json);

        // Assert
        deserialized.Should().NotBeNull();
        deserialized!.TotalCount.Should().Be(10);
        deserialized.PageNumber.Should().Be(1);
        deserialized.PageSize.Should().Be(5);
        deserialized.TotalPages.Should().Be(2);
        deserialized.HasPreviousPage.Should().BeFalse();
        deserialized.HasNextPage.Should().BeTrue();
        deserialized.Items.Should().HaveCount(1);
    }

    [Fact]
    public void DistanceData_ShouldSerializeAndDeserialize()
    {
        // Arrange
        var distance = new DistanceData
        {
            Activity = "running",
            Distance = 5.2
        };

        // Act
        var json = JsonSerializer.Serialize(distance);
        var deserialized = JsonSerializer.Deserialize<DistanceData>(json);

        // Assert
        deserialized.Should().NotBeNull();
        deserialized!.Activity.Should().Be("running");
        deserialized.Distance.Should().Be(5.2);
    }

    [Fact]
    public void HeartRateZone_ShouldSerializeAndDeserialize()
    {
        // Arrange
        var zone = new HeartRateZone
        {
            CaloriesOut = 150.5,
            Max = 170,
            Min = 130,
            Minutes = 25,
            Name = "Cardio"
        };

        // Act
        var json = JsonSerializer.Serialize(zone);
        var deserialized = JsonSerializer.Deserialize<HeartRateZone>(json);

        // Assert
        deserialized.Should().NotBeNull();
        deserialized!.CaloriesOut.Should().Be(150.5);
        deserialized.Max.Should().Be(170);
        deserialized.Min.Should().Be(130);
        deserialized.Minutes.Should().Be(25);
        deserialized.Name.Should().Be("Cardio");
    }
}
