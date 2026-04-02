using AutoFixture;
using Biotrackr.Weight.Api.Models;

namespace Biotrackr.Weight.Api.IntegrationTests;

/// <summary>
/// Helper class for generating test data
/// </summary>
public static class TestDataHelper
{
    private static readonly Fixture _fixture = new();

    /// <summary>
    /// Creates a valid weight document for testing
    /// </summary>
    public static WeightDocument CreateValidWeightDocument(
        string? id = null,
        string? date = null,
        double? weightValue = null,
        string? provider = null)
    {
        var testDate = date ?? DateTime.UtcNow.ToString("yyyy-MM-dd");
        
        return new WeightDocument
        {
            Id = id ?? Guid.NewGuid().ToString(),
            Date = testDate,
            DocumentType = "weight",
            Provider = provider ?? "Withings",
            Weight = new WeightMeasurement
            {
                Date = testDate,
                WeightKg = weightValue ?? _fixture.CreateDouble(50, 150),
                Bmi = 22.5,
                Fat = 15.0,
                Time = DateTime.UtcNow.ToString("HH:mm:ss"),
                Source = provider ?? "Withings",
                LogId = _fixture.Create<long>(),
                FatMassKg = 15.23,
                FatFreeMassKg = 65.02,
                MuscleMassKg = 45.2,
                BoneMassKg = 3.1,
                WaterMassKg = 48.9,
                VisceralFatIndex = 10
            }
        };
    }

    /// <summary>
    /// Creates a legacy Fitbit weight document (no body comp fields) for backward compatibility testing
    /// </summary>
    public static WeightDocument CreateLegacyFitbitWeightDocument(
        string? id = null,
        string? date = null)
    {
        var testDate = date ?? DateTime.UtcNow.ToString("yyyy-MM-dd");
        
        return new WeightDocument
        {
            Id = id ?? Guid.NewGuid().ToString(),
            Date = testDate,
            DocumentType = "weight",
            Provider = "Fitbit",
            Weight = new WeightMeasurement
            {
                Date = testDate,
                WeightKg = _fixture.CreateDouble(50, 150),
                Bmi = 24.1,
                Fat = 18.5,
                Time = DateTime.UtcNow.ToString("HH:mm:ss"),
                Source = "Aria",
                LogId = _fixture.Create<long>()
            }
        };
    }

    /// <summary>
    /// Creates a collection of weight documents for testing
    /// </summary>
    public static List<WeightDocument> CreateWeightDocuments(int count, string? userId = null)
    {
        var documents = new List<WeightDocument>();

        for (int i = 0; i < count; i++)
        {
            documents.Add(CreateValidWeightDocument(
                date: DateTime.UtcNow.AddDays(-i).ToString("yyyy-MM-dd")));
        }

        return documents;
    }

    /// <summary>
    /// Creates a weight document with invalid data for negative testing
    /// </summary>
    public static WeightDocument CreateInvalidWeightDocument(string? invalidReason = null)
    {
        return invalidReason switch
        {
            "negative-weight" => CreateValidWeightDocument(weightValue: -10.0),
            "empty-id" => new WeightDocument
            {
                Id = string.Empty,
                Date = DateTime.UtcNow.ToString("yyyy-MM-dd"),
                DocumentType = "weight",
                Weight = new WeightMeasurement { WeightKg = 70.0 }
            },
            "future-date" => CreateValidWeightDocument(
                date: DateTime.UtcNow.AddDays(1).ToString("yyyy-MM-dd")),
            _ => new WeightDocument
            {
                Id = string.Empty,
                Date = string.Empty,
                DocumentType = string.Empty,
                Weight = null!
            }
        };
    }

    private static double CreateDouble(this Fixture fixture, double min, double max)
    {
        var random = new Random();
        return Math.Round(min + (random.NextDouble() * (max - min)), 2);
    }
}
