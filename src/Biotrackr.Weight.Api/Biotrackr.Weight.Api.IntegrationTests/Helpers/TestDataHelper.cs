using AutoFixture;
using Biotrackr.Weight.Api.Models;
using FitbitWeight = Biotrackr.Weight.Api.Models.FitbitEntities.Weight;

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
        double? weightValue = null)
    {
        var testDate = date ?? DateTime.UtcNow.ToString("yyyy-MM-dd");
        
        return new WeightDocument
        {
            Id = id ?? Guid.NewGuid().ToString(),
            Date = testDate,
            DocumentType = "weight",
            Weight = new FitbitWeight
            {
                Date = testDate,
                weight = weightValue ?? _fixture.CreateDouble(50, 150),
                Bmi = 22.5,
                Fat = 15.0,
                Time = DateTime.UtcNow.ToString("HH:mm:ss"),
                Source = "API",
                LogId = null!
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
                Weight = new FitbitWeight { weight = 70.0 }
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
