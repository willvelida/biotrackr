using FluentAssertions;
using System.Text.Json;
using Biotrackr.UI.Models.Food;

namespace Biotrackr.UI.UnitTests.Models.Food;

public class FoodModelsShould
{
    [Fact]
    public void FoodEntry_ShouldRoundTrip()
    {
        // Arrange
        var entry = new FoodEntry
        {
            IsFavorite = true,
            LogDate = "2024-02-20",
            LogId = 54321,
            LoggedFood = new LoggedFood { Name = "Apple", Calories = 95 },
            NutritionalValues = new NutritionalValues { Calories = 95, Fiber = 4.4 }
        };

        // Act
        var json = JsonSerializer.Serialize(entry);
        var result = JsonSerializer.Deserialize<FoodEntry>(json);

        // Assert
        result.Should().NotBeNull();
        result!.IsFavorite.Should().BeTrue();
        result.LogDate.Should().Be("2024-02-20");
        result.LogId.Should().Be(54321);
        result.LoggedFood.Name.Should().Be("Apple");
        result.NutritionalValues.Fiber.Should().Be(4.4);
    }

    [Fact]
    public void FoodUnit_ShouldRoundTrip()
    {
        // Arrange
        var unit = new FoodUnit
        {
            Id = 5,
            Name = "tablespoon",
            Plural = "tablespoons"
        };

        // Act
        var json = JsonSerializer.Serialize(unit);
        var result = JsonSerializer.Deserialize<FoodUnit>(json);

        // Assert
        result.Should().NotBeNull();
        result!.Id.Should().Be(5);
        result.Name.Should().Be("tablespoon");
        result.Plural.Should().Be("tablespoons");
    }

    [Fact]
    public void LoggedFood_ShouldRoundTrip()
    {
        // Arrange
        var food = new LoggedFood
        {
            AccessLevel = "PRIVATE",
            Amount = 1,
            Brand = "Generic",
            Calories = 120,
            FoodId = 42,
            Locale = "en_US",
            MealTypeId = 3,
            Name = "Yogurt",
            Unit = new FoodUnit { Name = "container" },
            Units = [1, 5, 10]
        };

        // Act
        var json = JsonSerializer.Serialize(food);
        var result = JsonSerializer.Deserialize<LoggedFood>(json);

        // Assert
        result.Should().NotBeNull();
        result!.AccessLevel.Should().Be("PRIVATE");
        result.Amount.Should().Be(1);
        result.Brand.Should().Be("Generic");
        result.Calories.Should().Be(120);
        result.FoodId.Should().Be(42);
        result.Locale.Should().Be("en_US");
        result.MealTypeId.Should().Be(3);
        result.Name.Should().Be("Yogurt");
        result.Unit.Name.Should().Be("container");
        result.Units.Should().BeEquivalentTo(new[] { 1, 5, 10 });
    }

    [Fact]
    public void NutritionalValues_ShouldRoundTrip()
    {
        // Arrange
        var values = new NutritionalValues
        {
            Calories = 200,
            Carbs = 25,
            Fat = 8,
            Fiber = 2,
            Protein = 10,
            Sodium = 300
        };

        // Act
        var json = JsonSerializer.Serialize(values);
        var result = JsonSerializer.Deserialize<NutritionalValues>(json);

        // Assert
        result.Should().NotBeNull();
        result!.Calories.Should().Be(200);
        result.Carbs.Should().Be(25);
        result.Fat.Should().Be(8);
        result.Fiber.Should().Be(2);
        result.Protein.Should().Be(10);
        result.Sodium.Should().Be(300);
    }
}
