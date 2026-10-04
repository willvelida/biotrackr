using Biotrackr.Food.Api.Models;
using Biotrackr.Food.Api.Services;
using Biotrackr.Food.Api.UnitTests.Fixtures;
using FluentAssertions;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Serialization;

namespace Biotrackr.Food.Api.UnitTests.ServiceTests;

public class FoodDocumentTranslatorShould
{
    private static readonly JsonSerializerSettings CosmosSettings = new()
    {
        ContractResolver = new CamelCasePropertyNamesContractResolver()
    };

    private readonly FoodDocumentTranslator _translator = new();

    private static FoodStoredDocument Stored(string json) =>
        JsonConvert.DeserializeObject<FoodStoredDocument>(json, CosmosSettings)!;

    private static FoodStoredDocument SingleEntryDocument(string nutritionLogJson, string foodsItemsJson = "[]") => new()
    {
        Id = "v2-single",
        Date = "2026-02-01",
        Provider = "Google",
        SchemaVersion = 2,
        Google = JObject.Parse($$"""
            {
              "nutritionLog": { "dataPoints": [ { "nutritionLog": {{nutritionLogJson}} } ] },
              "hydrationLog": {},
              "foods": { "items": {{foodsItemsJson}} }
            }
            """)
    };

    private FoodDocument TranslateSample() => _translator.Translate(Stored(FoodDocumentSamples.Version2Json));

    [Fact]
    public void Translate_ShouldThrowArgumentNullException_WhenDocumentIsNull()
    {
        // Arrange
        FoodStoredDocument storedDocument = null!;

        // Act
        var act = () => _translator.Translate(storedDocument);

        // Assert
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void Translate_ShouldThrowNotSupportedException_WhenSchemaVersionIsUnknown()
    {
        // Arrange
        var storedDocument = new FoodStoredDocument { Id = "future", SchemaVersion = 3 };

        // Act
        var act = () => _translator.Translate(storedDocument);

        // Assert
        act.Should().Throw<NotSupportedException>().WithMessage("*schemaVersion 3*");
    }

    [Fact]
    public void Translate_ShouldPassFoodThroughUnchanged_WhenDocumentIsVersion1()
    {
        // Arrange
        var storedDocument = Stored(FoodDocumentSamples.Version1Json);

        // Act
        var result = _translator.Translate(storedDocument);

        // Assert
        result.Food.Should().BeSameAs(storedDocument.Food, "version 1 food must pass through untouched (AC 17)");
    }

    [Fact]
    public void Translate_ShouldStampFitbitAndVersion1_WhenSchemaVersionIsMissing()
    {
        // Arrange
        var storedDocument = Stored(FoodDocumentSamples.Version1Json);

        // Act
        var result = _translator.Translate(storedDocument);

        // Assert
        new object[] { result.Id, result.Date, result.DocumentType, result.Provider, result.SchemaVersion }
            .Should().Equal("v1-doc-id", "2025-12-01", "Food", "Fitbit", 1);
    }

    [Fact]
    public void Translate_ShouldReturnEmptyFoodResponse_WhenVersion1DocumentHasNoFood()
    {
        // Arrange
        var storedDocument = new FoodStoredDocument { Id = "v1-empty", Date = "2025-12-03" };

        // Act
        var result = _translator.Translate(storedDocument);

        // Assert
        result.Food.Foods.Should().BeEmpty();
    }

    [Fact]
    public void Translate_ShouldStampGoogleAndVersion2_WhenDocumentIsVersion2()
    {
        // Act
        var result = TranslateSample();

        // Assert
        new object[] { result.Id, result.Date, result.DocumentType, result.Provider, result.SchemaVersion }
            .Should().Equal("v2-doc-id", "2026-01-15", "Food", "Google", 2);
    }

    [Fact]
    public void Translate_ShouldMapEveryNutritionLogEntry_WhenDocumentIsVersion2()
    {
        // Act
        var result = TranslateSample();

        // Assert
        result.Food.Foods.Select(f => f.LoggedFood.Name).Should().Equal("Synthetic Oats", "Synthetic Soup");
    }

    [Fact]
    public void Translate_ShouldSetGoalsToNull_WhenDocumentIsVersion2()
    {
        // Act
        var result = TranslateSample();

        // Assert
        result.Food.Goals.Should().BeNull("C2 sets food.goals to null for version 2");
    }

    [Fact]
    public void Translate_ShouldSetFitbitOnlyIdentifiersToNull_WhenDocumentIsVersion2()
    {
        // Act
        var entry = TranslateSample().Food.Foods[0];

        // Assert
        new object?[] { entry.LogId, entry.IsFavorite, entry.LoggedFood.FoodId, entry.LoggedFood.Units, entry.LoggedFood.Unit.Id }
            .Should().AllSatisfy(v => v.Should().BeNull("C2 sets logId, isFavorite, foodId, units, and unit.id to null"));
    }

    [Fact]
    public void Translate_ShouldUseDocumentDateAsLogDate_WhenDocumentIsVersion2()
    {
        // Act
        var result = TranslateSample();

        // Assert
        result.Food.Foods.Select(f => f.LogDate).Should().AllBe("2026-01-15");
    }

    [Fact]
    public void Translate_ShouldMapEnergyToCalories_WhenDocumentIsVersion2()
    {
        // Act
        var entry = TranslateSample().Food.Foods[0];

        // Assert
        new[] { entry.LoggedFood.Calories, entry.NutritionalValues.Calories }.Should().Equal(250.5, 250.5);
    }

    [Fact]
    public void Translate_ShouldMapCarbsAndFatFromTotals_WhenDocumentIsVersion2()
    {
        // Act
        var values = TranslateSample().Food.Foods[0].NutritionalValues;

        // Assert
        new[] { values.Carbs, values.Fat }.Should().Equal(30, 10);
    }

    [Fact]
    public void Translate_ShouldMapFiberProteinAndSodiumMilligrams_WhenNutrientsArePresent()
    {
        // Act
        var values = TranslateSample().Food.Foods[0].NutritionalValues;

        // Assert
        values.Fiber.Should().Be(5.5);
        values.Protein.Should().Be(12.5);
        values.Sodium.Should().BeApproximately(400, 1e-9, "sodium is grams x 1000 (0.4 g = 400 mg)");
    }

    [Fact]
    public void Translate_ShouldDefaultNutrientsToZero_WhenEntryHasNoNutrients()
    {
        // Act
        var values = TranslateSample().Food.Foods[1].NutritionalValues;

        // Assert
        new[] { values.Carbs, values.Fat, values.Fiber, values.Protein, values.Sodium }.Should().AllBeEquivalentTo(0.0);
    }

    [Fact]
    public void Translate_ShouldMapServingAmount_WhenServingIsPresent()
    {
        // Act
        var entry = TranslateSample().Food.Foods[0];

        // Assert
        entry.LoggedFood.Amount.Should().Be(2);
    }

    [Fact]
    public void Translate_ShouldMapBrandAccessLevelAndLocaleFromFoods_WhenFoodMatches()
    {
        // Act
        var loggedFood = TranslateSample().Food.Foods[0].LoggedFood;

        // Assert
        new[] { loggedFood.Brand, loggedFood.AccessLevel, loggedFood.Locale }
            .Should().Equal("Synthetic Mills", "FOOD_ACCESS_LEVEL_PUBLIC", "en_AU");
    }

    [Fact]
    public void Translate_ShouldLeaveBrandAccessLevelAndLocaleNull_WhenEntryHasNoMatchingFood()
    {
        // Act
        var loggedFood = TranslateSample().Food.Foods[1].LoggedFood;

        // Assert
        new[] { loggedFood.Brand, loggedFood.AccessLevel, loggedFood.Locale }.Should().AllSatisfy(v => v.Should().BeNull());
    }

    [Fact]
    public void Translate_ShouldTakeUnitNamesFromMatchingFoodServing_WhenMeasurementUnitMatches()
    {
        // Act
        var unit = TranslateSample().Food.Foods[0].LoggedFood.Unit;

        // Assert
        new[] { unit.Name, unit.Plural }.Should().Equal(new[] { "bowl", "bowls" },
            "the google.foods servings[] entry wins over nutritionLog.serving.foodMeasurementUnitDisplayName");
    }

    [Fact]
    public void Translate_ShouldFallBackToServingDisplayName_WhenNoFoodServingMatches()
    {
        // Arrange
        var storedDocument = SingleEntryDocument("""
            { "foodDisplayName": "Synthetic Tea", "food": "users/me/foods/none",
              "serving": { "amount": 1.5, "foodMeasurementUnit": "users/me/dataTypes/food-measurement-unit/dataPoints/999",
                           "foodMeasurementUnitDisplayName": "cup" } }
            """);

        // Act
        var unit = _translator.Translate(storedDocument).Food.Foods[0].LoggedFood.Unit;

        // Assert
        new[] { unit.Name, unit.Plural }.Should().Equal("cup", null);
    }

    [Fact]
    public void Translate_ShouldSetUnitNamesToNull_WhenNoServingInformationExists()
    {
        // Act
        var unit = TranslateSample().Food.Foods[1].LoggedFood.Unit;

        // Assert
        new[] { unit.Name, unit.Plural }.Should().AllSatisfy(v => v.Should().BeNull());
    }

    [Fact]
    public void Translate_ShouldFallBackToServingDisplayName_WhenMatchingFoodHasNoServings()
    {
        // Arrange
        var storedDocument = SingleEntryDocument(
            """
            { "foodDisplayName": "Synthetic Rice", "food": "users/me/foods/rice",
              "serving": { "amount": 1, "foodMeasurementUnit": "u/304", "foodMeasurementUnitDisplayName": "serving" } }
            """,
            """[ { "name": "users/me/foods/rice", "food": { "displayName": "Synthetic Rice", "brand": "Synthetic Paddy" } } ]""");

        // Act
        var loggedFood = _translator.Translate(storedDocument).Food.Foods[0].LoggedFood;

        // Assert
        new[] { loggedFood.Brand, loggedFood.Unit.Name, loggedFood.Unit.Plural }.Should().Equal("Synthetic Paddy", "serving", null);
    }

    [Theory]
    [InlineData("\"BREAKFAST\"", 1)]
    [InlineData("\"BEFORE_LUNCH\"", 2)]
    [InlineData("\"LUNCH\"", 3)]
    [InlineData("\"BEFORE_DINNER\"", 4)]
    [InlineData("\"DINNER\"", 5)]
    [InlineData("\"AFTER_DINNER\"", 7)]
    [InlineData("\"SNACK\"", 7)]
    [InlineData("\"BEFORE_BREAKFAST\"", 7)]
    [InlineData("\"ANYTIME\"", 7)]
    [InlineData("\"MEAL_TYPE_UNSPECIFIED\"", 7)]
    [InlineData("null", 7)]
    public void Translate_ShouldMapMealTypeToFitbitMealTypeId_WhenMealTypeIsGiven(string mealTypeJson, int expectedMealTypeId)
    {
        // Arrange
        var storedDocument = SingleEntryDocument($$"""{ "foodDisplayName": "Synthetic Meal", "mealType": {{mealTypeJson}} }""");

        // Act
        var mealTypeId = _translator.Translate(storedDocument).Food.Foods[0].LoggedFood.MealTypeId;

        // Assert
        mealTypeId.Should().Be(expectedMealTypeId, $"C2 maps mealType {mealTypeJson} to {expectedMealTypeId}");
    }

    [Fact]
    public void Translate_ShouldMapMissingMealTypeToAnytime_WhenMealTypeIsAbsent()
    {
        // Arrange
        var storedDocument = SingleEntryDocument("""{ "foodDisplayName": "Synthetic Meal" }""");

        // Act
        var mealTypeId = _translator.Translate(storedDocument).Food.Foods[0].LoggedFood.MealTypeId;

        // Assert
        mealTypeId.Should().Be(7);
    }

    [Fact]
    public void Translate_ShouldParseStringNumbers_WhenGoogleEncodesNumbersAsStrings()
    {
        // Arrange
        var storedDocument = SingleEntryDocument("""{ "foodDisplayName": "Synthetic Bar", "energy": { "kcal": "199.5" } }""");

        // Act
        var calories = _translator.Translate(storedDocument).Food.Foods[0].LoggedFood.Calories;

        // Assert
        calories.Should().Be(199.5);
    }

    [Fact]
    public void Translate_ShouldSumEntriesIntoSummary_WhenDocumentIsVersion2()
    {
        // Act
        var summary = TranslateSample().Food.Summary;

        // Assert
        summary.Calories.Should().Be(350.5);
        summary.Carbs.Should().Be(30);
        summary.Fat.Should().Be(10);
        summary.Fiber.Should().Be(5.5);
        summary.Protein.Should().Be(12.5);
        summary.Sodium.Should().BeApproximately(400, 1e-9);
    }

    [Fact]
    public void Translate_ShouldSumHydrationIntoWater_WhenHydrationLogsExist()
    {
        // Act
        var summary = TranslateSample().Food.Summary;

        // Assert
        summary.Water.Should().Be(750.5, "water is the sum of hydrationLog amountConsumed.milliliters (500 + 250.5)");
    }

    [Fact]
    public void Translate_ShouldReturnEmptyFoodsAndZeroSummary_WhenDayIsEmpty()
    {
        // Arrange
        var storedDocument = Stored(FoodDocumentSamples.Version2EmptyDayJson);

        // Act
        var food = _translator.Translate(storedDocument).Food;

        // Assert
        food.Foods.Should().BeEmpty();
        food.Summary.Should().BeEquivalentTo(new Models.FitbitEntities.Summary(), "an empty day has a zero summary");
    }

    [Fact]
    public void Translate_ShouldReturnEmptyFoods_WhenGooglePayloadIsMissing()
    {
        // Arrange
        var storedDocument = new FoodStoredDocument { Id = "v2-no-google", Date = "2026-02-02", Provider = "Google", SchemaVersion = 2 };

        // Act
        var result = _translator.Translate(storedDocument);

        // Assert
        result.Food.Foods.Should().BeEmpty();
    }
}
