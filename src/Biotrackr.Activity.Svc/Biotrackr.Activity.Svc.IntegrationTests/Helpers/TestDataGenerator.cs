using AutoFixture;
using Biotrackr.Activity.Svc.Models;
using Biotrackr.Activity.Svc.Models.FitbitEntities;

namespace Biotrackr.Activity.Svc.IntegrationTests.Helpers;

public static class TestDataGenerator
{
    private static readonly Fixture _fixture = new();

    public static ActivityDocument CreateActivityDocument(string? date = null, string? id = null)
    {
        return new ActivityDocument
        {
            Id = id ?? Guid.NewGuid().ToString(),
            Date = date ?? DateTime.Now.AddDays(-1).ToString("yyyy-MM-dd"),
            DocumentType = "Activity",
            Activity = CreateActivityResponse()
        };
    }

    public static ActivityResponse CreateActivityResponse()
    {
        return new ActivityResponse
        {
            summary = CreateSummary(),
            activities = new List<Models.FitbitEntities.Activity>()
        };
    }

    private static Summary CreateSummary()
    {
        return new Summary
        {
            activeScore = _fixture.Create<int>(),
            activityCalories = _fixture.Create<int>(),
            caloriesBMR = _fixture.Create<int>(),
            caloriesOut = _fixture.Create<int>(),
            distances = CreateDistances(),
            elevation = _fixture.Create<double>(),
            fairlyActiveMinutes = _fixture.Create<int>(),
            floors = _fixture.Create<int>(),
            heartRateZones = CreateHeartRateZones(),
            lightlyActiveMinutes = _fixture.Create<int>(),
            marginalCalories = _fixture.Create<int>(),
            restingHeartRate = _fixture.Create<int>(),
            sedentaryMinutes = _fixture.Create<int>(),
            steps = _fixture.Create<int>(),
            veryActiveMinutes = _fixture.Create<int>()
        };
    }

    private static List<Distance> CreateDistances()
    {
        return new List<Distance>
        {
            new Distance
            {
                activity = "total",
                distance = _fixture.Create<double>()
            }
        };
    }

    private static List<HeartRateZone> CreateHeartRateZones()
    {
        return new List<HeartRateZone>
        {
            new HeartRateZone
            {
                caloriesOut = _fixture.Create<double>(),
                max = 93,
                min = 30,
                minutes = _fixture.Create<int>(),
                name = "Out of Range"
            },
            new HeartRateZone
            {
                caloriesOut = _fixture.Create<double>(),
                max = 130,
                min = 94,
                minutes = _fixture.Create<int>(),
                name = "Fat Burn"
            }
        };
    }
}
