using FluentAssertions;
using System.Text.Json;
using Biotrackr.Mcp.Server.Models.Food;

namespace Biotrackr.Mcp.Server.UnitTests.Models;

public class FoodModelsShould
{
    [Fact]
    public void FoodEntry_ShouldSerializeAndDeserialize()
    {
        // Arrange
        var entry = new FoodEntry
        {
            IsFavorite = true,
            LogDate = "2024-01-15",
            LogId = 12345,
            LoggedFood = new LoggedFood { Name = "Banana", Calories = 105 },
            NutritionalValues = new NutritionalValues { Calories = 105, Protein = 1.3 }
        };

        // Act
        var json = JsonSerializer.Serialize(entry);
        var deserialized = JsonSerializer.Deserialize<FoodEntry>(json);

        // Assert
        deserialized.Should().NotBeNull();
        deserialized!.LogDate.Should().Be("2024-01-15");
        deserialized.LoggedFood.Name.Should().Be("Banana");
        deserialized.NutritionalValues.Calories.Should().Be(105);
    }

    [Fact]
    public void FoodUnit_ShouldSerializeAndDeserialize()
    {
        // Arrange
        var unit = new FoodUnit
        {
            Id = 1,
            Name = "cup",
            Plural = "cups"
        };

        // Act
        var json = JsonSerializer.Serialize(unit);
        var deserialized = JsonSerializer.Deserialize<FoodUnit>(json);

        // Assert
        deserialized.Should().NotBeNull();
        deserialized!.Name.Should().Be("cup");
    }

    [Fact]
    public void LoggedFood_ShouldSerializeAndDeserialize()
    {
        // Arrange
        var food = new LoggedFood
        {
            AccessLevel = "PUBLIC",
            Amount = 2,
            Brand = "Dole",
            Calories = 210,
            FoodId = 99,
            Locale = "en_US",
            MealTypeId = 1,
            Name = "Banana",
            Unit = new FoodUnit { Name = "medium" },
            Units = [1, 2, 3]
        };

        // Act
        var json = JsonSerializer.Serialize(food);
        var deserialized = JsonSerializer.Deserialize<LoggedFood>(json);

        // Assert
        deserialized.Should().NotBeNull();
        deserialized!.Amount.Should().Be(2);
        deserialized.Brand.Should().Be("Dole");
        deserialized.Calories.Should().Be(210);
        deserialized.Name.Should().Be("Banana");
        deserialized.Unit.Name.Should().Be("medium");
    }

    [Fact]
    public void NutritionalValues_ShouldSerializeAndDeserialize()
    {
        // Arrange
        var values = new NutritionalValues
        {
            Calories = 250,
            Carbs = 30.5,
            Fat = 12.2,
            Fiber = 3.1,
            Protein = 8.5,
            Sodium = 150
        };

        // Act
        var json = JsonSerializer.Serialize(values);
        var deserialized = JsonSerializer.Deserialize<NutritionalValues>(json);

        // Assert
        deserialized.Should().NotBeNull();
        deserialized!.Calories.Should().Be(250);
        deserialized.Carbs.Should().Be(30.5);
        deserialized.Fat.Should().Be(12.2);
        deserialized.Fiber.Should().Be(3.1);
        deserialized.Protein.Should().Be(8.5);
        deserialized.Sodium.Should().Be(150);
    }
}
