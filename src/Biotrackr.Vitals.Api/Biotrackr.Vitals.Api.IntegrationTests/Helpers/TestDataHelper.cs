using AutoFixture;
using Biotrackr.Vitals.Api.Models;

namespace Biotrackr.Vitals.Api.IntegrationTests;

/// <summary>
/// Helper class for generating test data
/// </summary>
public static class TestDataHelper
{
    private static readonly Fixture _fixture = new();

    /// <summary>
    /// Creates a valid vitals document for testing
    /// </summary>
    public static VitalsDocument CreateValidVitalsDocument(
        string? id = null,
        string? date = null,
        double? weightValue = null,
        string? provider = null)
    {
        var testDate = date ?? DateTime.UtcNow.ToString("yyyy-MM-dd");
        
        return new VitalsDocument
        {
            Id = id ?? Guid.NewGuid().ToString(),
            Date = testDate,
            DocumentType = "Vitals",
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
    /// Creates a legacy Fitbit vitals document (no body comp fields) for backward compatibility testing
    /// </summary>
    public static VitalsDocument CreateLegacyFitbitVitalsDocument(
        string? id = null,
        string? date = null)
    {
        var testDate = date ?? DateTime.UtcNow.ToString("yyyy-MM-dd");
        
        return new VitalsDocument
        {
            Id = id ?? Guid.NewGuid().ToString(),
            Date = testDate,
            DocumentType = "Vitals",
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
    /// Creates a collection of vitals documents for testing
    /// </summary>
    public static List<VitalsDocument> CreateVitalsDocuments(int count, string? userId = null)
    {
        var documents = new List<VitalsDocument>();

        for (int i = 0; i < count; i++)
        {
            documents.Add(CreateValidVitalsDocument(
                date: DateTime.UtcNow.AddDays(-i).ToString("yyyy-MM-dd")));
        }

        return documents;
    }

    /// <summary>
    /// Creates a vitals document with invalid data for negative testing
    /// </summary>
    public static VitalsDocument CreateInvalidVitalsDocument(string? invalidReason = null)
    {
        return invalidReason switch
        {
            "negative-weight" => CreateValidVitalsDocument(weightValue: -10.0),
            "empty-id" => new VitalsDocument
            {
                Id = string.Empty,
                Date = DateTime.UtcNow.ToString("yyyy-MM-dd"),
                DocumentType = "Vitals",
                Weight = new WeightMeasurement { WeightKg = 70.0 }
            },
            "future-date" => CreateValidVitalsDocument(
                date: DateTime.UtcNow.AddDays(1).ToString("yyyy-MM-dd")),
            _ => new VitalsDocument
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
