using AutoFixture;
using Biotrackr.Activity.Api.Models;
using FitbitActivity = Biotrackr.Activity.Api.Models.FitbitEntities.Activity;
using Biotrackr.Activity.Api.Models.FitbitEntities;

namespace Biotrackr.Activity.Api.IntegrationTests.Helpers;

/// <summary>
/// Helper class for generating test data for Activity API integration tests
/// Per decision-record 2025-10-28-integration-test-project-structure.md
/// </summary>
public static class TestDataHelper
{
    private static readonly Fixture _fixture = new();

    /// <summary>
    /// Creates a valid activity document for testing
    /// </summary>
    public static ActivityDocument CreateValidActivityDocument(
        string? id = null,
        string? date = null)
    {
        var testDate = date ?? DateTime.UtcNow.ToString("yyyy-MM-dd");
        
        return new ActivityDocument
        {
            Id = id ?? Guid.NewGuid().ToString(),
            Date = testDate,
            DocumentType = "activity",
            Activity = new ActivityResponse
            {
                activities = new List<FitbitActivity>
                {
                    new FitbitActivity
                    {
                        activityId = 90013,
                        activityParentId = 90013,
                        activityParentName = "Walk",
                        calories = 300,
                        description = "Morning walk",
                        distance = 5.0,
                        duration = 3600000, // 1 hour in milliseconds
                        hasActiveZoneMinutes = true,
                        hasStartTime = true,
                        isFavorite = false,
                        lastModified = DateTime.UtcNow,
                        logId = 123456789,
                        name = "Walk",
                        startDate = testDate,
                        startTime = "07:00:00",
                        steps = 7500
                    }
                },
                goals = new Goals
                {
                    activeMinutes = 30,
                    caloriesOut = 2500,
                    distance = 8.0,
                    floors = 10,
                    steps = 10000
                },
                summary = new Summary
                {
                    activeScore = -1,
                    activityCalories = 300,
                    caloriesBMR = 1500,
                    caloriesOut = 2500,
                    distances = new List<Distance>
                    {
                        new Distance
                        {
                            activity = "Walk",
                            distance = 5.0
                        }
                    },
                    fairlyActiveMinutes = 30,
                    floors = 5,
                    lightlyActiveMinutes = 60,
                    marginalCalories = 100,
                    sedentaryMinutes = 600,
                    steps = 7500,
                    veryActiveMinutes = 15,
                    heartRateZones = new List<HeartRateZone>
                    {
                        new HeartRateZone
                        {
                            caloriesOut = 100.0,
                            max = 100,
                            min = 30,
                            minutes = 60,
                            name = "Out of Range"
                        }
                    }
                }
            }
        };
    }

    /// <summary>
    /// Creates a collection of activity documents for testing
    /// </summary>
    public static List<ActivityDocument> CreateActivityDocuments(int count)
    {
        var documents = new List<ActivityDocument>();

        for (int i = 0; i < count; i++)
        {
            documents.Add(CreateValidActivityDocument(
                date: DateTime.UtcNow.AddDays(-i).ToString("yyyy-MM-dd")));
        }

        return documents;
    }

    /// <summary>
    /// Creates an activity document with invalid data for negative testing
    /// </summary>
    public static ActivityDocument CreateInvalidActivityDocument(string? invalidReason = null)
    {
        return invalidReason switch
        {
            "empty-id" => new ActivityDocument
            {
                Id = string.Empty,
                Date = DateTime.UtcNow.ToString("yyyy-MM-dd"),
                DocumentType = "activity",
                Activity = new ActivityResponse
                {
                    activities = new List<FitbitActivity>(),
                    goals = null!,
                    summary = null!
                }
            },
            "future-date" => CreateValidActivityDocument(
                date: DateTime.UtcNow.AddDays(1).ToString("yyyy-MM-dd")),
            _ => new ActivityDocument
            {
                Id = string.Empty,
                Date = string.Empty,
                DocumentType = string.Empty,
                Activity = null!
            }
        };
    }
}
