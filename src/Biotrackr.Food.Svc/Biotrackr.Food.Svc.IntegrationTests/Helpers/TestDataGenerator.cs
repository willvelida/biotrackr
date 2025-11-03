using AutoFixture;
using Biotrackr.Food.Svc.Models;
using Biotrackr.Food.Svc.Models.FitbitEntities;

namespace Biotrackr.Food.Svc.IntegrationTests.Helpers;

/// <summary>
/// Helper class to generate test data for Food Service integration tests.
/// </summary>
public static class TestDataGenerator
{
    private static readonly Fixture _fixture = new();

    /// <summary>
    /// Generates a valid FoodDocument with realistic test data.
    /// </summary>
    public static FoodDocument GenerateFoodDocument(string? date = null)
    {
        return new FoodDocument
        {
            Id = Guid.NewGuid().ToString(),
            Date = date ?? DateTime.UtcNow.ToString("yyyy-MM-dd"),
            DocumentType = "food",
            Food = GenerateFoodResponse(date)
        };
    }

    /// <summary>
    /// Generates a valid FoodResponse from Fitbit API.
    /// </summary>
    public static FoodResponse GenerateFoodResponse(string? date = null)
    {
        return new FoodResponse
        {
            foods = GenerateFoodList(),
            goals = GenerateGoals(),
            summary = GenerateSummary()
        };
    }

    /// <summary>
    /// Generates nutritional goals.
    /// </summary>
    public static Goals GenerateGoals()
    {
        return new Goals
        {
            calories = 2000
        };
    }

    /// <summary>
    /// Generates daily nutritional summary.
    /// </summary>
    public static Summary GenerateSummary()
    {
        return new Summary
        {
            calories = 1800,
            carbs = 220,
            fat = 60,
            fiber = 22,
            protein = 70,
            sodium = 2100,
            water = 2000
        };
    }

    /// <summary>
    /// Generates a list of logged foods.
    /// </summary>
    public static List<Models.FitbitEntities.Food> GenerateFoodList(int count = 3)
    {
        var foods = new List<Models.FitbitEntities.Food>();
        for (int i = 0; i < count; i++)
        {
            foods.Add(new Models.FitbitEntities.Food
            {
                isFavorite = i == 0,
                logDate = DateTime.UtcNow.ToString("yyyy-MM-dd"),
                logId = _fixture.Create<long>(),
                loggedFood = GenerateLoggedFood(),
                nutritionalValues = GenerateNutritionalValues()
            });
        }
        return foods;
    }

    /// <summary>
    /// Generates logged food details.
    /// </summary>
    public static LoggedFood GenerateLoggedFood()
    {
        return new LoggedFood
        {
            accessLevel = "PUBLIC",
            amount = 1,
            brand = "Test Brand",
            calories = 250,
            foodId = _fixture.Create<int>(),
            locale = "en_US",
            mealTypeId = 1,
            name = "Test Food Item",
            unit = GenerateUnit(),
            units = new List<int> { 1, 2, 3 }
        };
    }

    /// <summary>
    /// Generates nutritional values.
    /// </summary>
    public static NutritionalValues GenerateNutritionalValues()
    {
        return new NutritionalValues
        {
            calories = 250,
            carbs = 30,
            fat = 10,
            fiber = 5,
            protein = 15,
            sodium = 400
        };
    }

    /// <summary>
    /// Generates unit information.
    /// </summary>
    public static Unit GenerateUnit()
    {
        return new Unit
        {
            id = _fixture.Create<int>(),
            name = "serving",
            plural = "servings"
        };
    }
}
