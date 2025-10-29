using FluentAssertions;
using Xunit;
using FitbitActivity = Biotrackr.Activity.Api.Models.FitbitEntities.Activity;

namespace Biotrackr.Activity.Api.UnitTests.ModelTests.FitbitEntityTests;

/// <summary>
/// Unit tests for Activity Fitbit entity
/// T028-T031: Activity entity tests including edge cases
/// </summary>
public class ActivityShould
{
    [Fact]
    public void Create_Activity_With_All_Required_Properties()
    {
        // Arrange & Act
        var activity = new FitbitActivity
        {
            activityId = 90013,
            activityParentId = 90013,
            activityParentName = "Walk",
            calories = 300,
            description = "Morning walk",
            duration = 3600000,
            hasActiveZoneMinutes = true,
            hasStartTime = true,
            isFavorite = false,
            lastModified = DateTime.UtcNow,
            logId = 123456789,
            name = "Walk",
            startDate = "2025-10-29",
            startTime = "07:00:00",
            steps = 7500
        };

        // Assert
        activity.Should().NotBeNull();
        activity.activityId.Should().Be(90013);
        activity.name.Should().Be("Walk");
        activity.calories.Should().Be(300);
    }

    [Fact]
    public void Allow_Null_Distance_Property()
    {
        // Arrange & Act
        var activity = new FitbitActivity
        {
            activityId = 90013,
            activityParentName = "Weights",
            description = "Weight lifting",
            duration = 1800000,
            name = "Weights",
            startDate = "2025-10-29",
            startTime = "08:00:00",
            distance = null // No distance for weight training
        };

        // Assert
        activity.distance.Should().BeNull();
    }

    [Fact]
    public void Support_Activities_With_Distance()
    {
        // Arrange & Act
        var activity = new FitbitActivity
        {
            activityId = 90013,
            activityParentName = "Run",
            description = "Morning run",
            duration = 1800000,
            name = "Run",
            startDate = "2025-10-29",
            startTime = "06:00:00",
            distance = 5.5,
            steps = 7000
        };

        // Assert
        activity.distance.Should().Be(5.5);
    }

    [Fact]
    public void Handle_Zero_Duration_Activities()
    {
        // Arrange & Act
        var activity = new FitbitActivity
        {
            activityId = 1234,
            activityParentName = "Test",
            description = "Quick test",
            duration = 0,
            name = "Test",
            startDate = "2025-10-29",
            startTime = "12:00:00"
        };

        // Assert
        activity.duration.Should().Be(0);
    }

    [Fact]
    public void Support_Activities_With_Zero_Calories()
    {
        // Arrange & Act
        var activity = new FitbitActivity
        {
            activityId = 90013,
            activityParentName = "Stretch",
            description = "Light stretching",
            duration = 600000,
            calories = 0,
            name = "Stretch",
            startDate = "2025-10-29",
            startTime = "09:00:00"
        };

        // Assert
        activity.calories.Should().Be(0);
    }

    [Fact]
    public void Support_Activities_With_Zero_Steps()
    {
        // Arrange & Act
        var activity = new FitbitActivity
        {
            activityId = 90013,
            activityParentName = "Swim",
            description = "Swimming",
            duration = 1800000,
            steps = 0, // Swimming doesn't count steps
            name = "Swim",
            startDate = "2025-10-29",
            startTime = "10:00:00"
        };

        // Assert
        activity.steps.Should().Be(0);
    }

    [Fact]
    public void SupportFavoriteActivity()
    {
        // Arrange & Act
        var activity = new FitbitActivity
        {
            activityId = 90013,
            activityParentName = "Yoga",
            description = "Favorite yoga session",
            duration = 3600000,
            isFavorite = true,
            name = "Yoga",
            startDate = "2025-10-29",
            startTime = "07:00:00"
        };

        // Assert
        activity.isFavorite.Should().BeTrue();
    }

    [Fact]
    public void Support_Activities_Without_Active_Zone_Minutes()
    {
        // Arrange & Act
        var activity = new FitbitActivity
        {
            activityId = 90013,
            activityParentName = "Walk",
            description = "Casual walk",
            duration = 1800000,
            hasActiveZoneMinutes = false,
            name = "Walk",
            startDate = "2025-10-29",
            startTime = "11:00:00"
        };

        // Assert
        activity.hasActiveZoneMinutes.Should().BeFalse();
    }

    [Fact]
    public void Support_Activities_Without_Start_Time()
    {
        // Arrange & Act
        var activity = new FitbitActivity
        {
            activityId = 90013,
            activityParentName = "Exercise",
            description = "General exercise",
            duration = 1800000,
            hasStartTime = false,
            name = "Exercise",
            startDate = "2025-10-29",
            startTime = string.Empty
        };

        // Assert
        activity.hasStartTime.Should().BeFalse();
    }
}
