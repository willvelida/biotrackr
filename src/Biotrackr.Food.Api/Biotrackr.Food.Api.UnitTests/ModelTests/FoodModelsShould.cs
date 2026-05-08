using System.Text.Json;
using Biotrackr.Food.Api.Models;
using Biotrackr.Food.Api.Models.FitbitEntities;
using FluentAssertions;

namespace Biotrackr.Food.Api.UnitTests.ModelTests
{
    public class FoodModelsShould
    {
        [Fact]
        public void FoodDocument_ShouldInitializeWithDefaults()
        {
            // Act
            var doc = new FoodDocument();

            // Assert
            doc.Id.Should().BeEmpty();
            doc.Date.Should().BeEmpty();
            doc.DocumentType.Should().Be("Food");
            doc.Food.Should().NotBeNull();
        }

        [Fact]
        public void FoodResponse_ShouldInitializeWithDefaults()
        {
            // Act
            var response = new FoodResponse();

            // Assert
            response.Foods.Should().NotBeNull().And.BeEmpty();
            response.Goals.Should().NotBeNull();
            response.Summary.Should().NotBeNull();
        }

        [Fact]
        public void Food_ShouldSerializeAndDeserialize()
        {
            // Arrange
            var food = new Biotrackr.Food.Api.Models.FitbitEntities.Food
            {
                IsFavorite = true,
                LogDate = "2023-05-01",
                LogId = 12345678,
                LoggedFood = new LoggedFood { Name = "Banana", Calories = 105 },
                NutritionalValues = new NutritionalValues { Calories = 105, Protein = 1.3 }
            };

            // Act
            var json = JsonSerializer.Serialize(food);
            var deserialized = JsonSerializer.Deserialize<Biotrackr.Food.Api.Models.FitbitEntities.Food>(json);

            // Assert
            deserialized.Should().NotBeNull();
            deserialized!.IsFavorite.Should().BeTrue();
            deserialized.LogDate.Should().Be("2023-05-01");
            deserialized.LogId.Should().Be(12345678);
            deserialized.LoggedFood.Name.Should().Be("Banana");
            deserialized.NutritionalValues.Calories.Should().Be(105);
        }

        [Fact]
        public void LoggedFood_ShouldInitializeWithDefaults()
        {
            // Act
            var loggedFood = new LoggedFood();

            // Assert
            loggedFood.AccessLevel.Should().BeEmpty();
            loggedFood.Amount.Should().Be(0);
            loggedFood.Brand.Should().BeEmpty();
            loggedFood.Calories.Should().Be(0);
            loggedFood.FoodId.Should().Be(0);
            loggedFood.Locale.Should().BeEmpty();
            loggedFood.MealTypeId.Should().Be(0);
            loggedFood.Name.Should().BeEmpty();
            loggedFood.Unit.Should().NotBeNull();
            loggedFood.Units.Should().NotBeNull().And.BeEmpty();
        }

        [Fact]
        public void NutritionalValues_ShouldSerializeCorrectly()
        {
            // Arrange
            var values = new NutritionalValues
            {
                Calories = 250.5,
                Carbs = 30.2,
                Fat = 12.1,
                Fiber = 3.5,
                Protein = 20.0,
                Sodium = 500.0
            };

            // Act
            var json = JsonSerializer.Serialize(values);
            var deserialized = JsonSerializer.Deserialize<NutritionalValues>(json);

            // Assert
            deserialized.Should().NotBeNull();
            deserialized!.Calories.Should().Be(250.5);
            deserialized.Carbs.Should().Be(30.2);
            deserialized.Fat.Should().Be(12.1);
            deserialized.Fiber.Should().Be(3.5);
            deserialized.Protein.Should().Be(20.0);
            deserialized.Sodium.Should().Be(500.0);
        }

        [Fact]
        public void Summary_ShouldSerializeCorrectly()
        {
            // Arrange
            var summary = new Summary
            {
                Calories = 2000,
                Carbs = 250,
                Fat = 65,
                Fiber = 25,
                Protein = 80,
                Sodium = 2300,
                Water = 2500
            };

            // Act
            var json = JsonSerializer.Serialize(summary);
            var deserialized = JsonSerializer.Deserialize<Summary>(json);

            // Assert
            deserialized.Should().NotBeNull();
            deserialized!.Calories.Should().Be(2000);
            deserialized.Water.Should().Be(2500);
        }

        [Fact]
        public void Goals_ShouldSerializeCorrectly()
        {
            // Arrange
            var goals = new Goals { Calories = 2000 };

            // Act
            var json = JsonSerializer.Serialize(goals);
            var deserialized = JsonSerializer.Deserialize<Goals>(json);

            // Assert
            deserialized.Should().NotBeNull();
            deserialized!.Calories.Should().Be(2000);
        }

        [Fact]
        public void Unit_ShouldInitializeWithDefaults()
        {
            // Act
            var unit = new Unit();

            // Assert
            unit.Id.Should().Be(0);
            unit.Name.Should().BeEmpty();
            unit.Plural.Should().BeEmpty();
        }
    }
}
