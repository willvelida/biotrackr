using System.Globalization;
using Biotrackr.Food.Api.Models;
using Biotrackr.Food.Api.Models.FitbitEntities;
using Biotrackr.Food.Api.Services.Interfaces;
using Newtonsoft.Json.Linq;
using FoodEntry = Biotrackr.Food.Api.Models.FitbitEntities.Food;

namespace Biotrackr.Food.Api.Services;

/// <summary>
/// Maps stored Food documents to the read contract. Version 1 passes through unchanged; version 2 is
/// derived from the raw Google payloads per contract C2.
/// </summary>
public sealed class FoodDocumentTranslator : IFoodDocumentTranslator
{
    private const string FitbitProvider = "Fitbit";
    private const string GoogleProvider = "Google";
    private const int AnytimeMealTypeId = 7;
    private const double MilligramsPerGram = 1000;

    private static readonly Dictionary<string, int> MealTypeIds = new(StringComparer.Ordinal)
    {
        ["BREAKFAST"] = 1,
        ["BEFORE_LUNCH"] = 2,
        ["LUNCH"] = 3,
        ["BEFORE_DINNER"] = 4,
        ["DINNER"] = 5
    };

    public FoodDocument Translate(FoodStoredDocument storedDocument)
    {
        ArgumentNullException.ThrowIfNull(storedDocument);

        return storedDocument.SchemaVersion switch
        {
            null or 1 => TranslateVersion1(storedDocument),
            2 => TranslateVersion2(storedDocument),
            _ => throw new NotSupportedException(
                $"Food document '{storedDocument.Id}' has unsupported schemaVersion {storedDocument.SchemaVersion}.")
        };
    }

    private static FoodDocument TranslateVersion1(FoodStoredDocument storedDocument) => new()
    {
        Id = storedDocument.Id,
        Date = storedDocument.Date,
        DocumentType = storedDocument.DocumentType,
        Provider = FitbitProvider,
        SchemaVersion = 1,
        Food = storedDocument.Food ?? new FoodResponse()
    };

    private static FoodDocument TranslateVersion2(FoodStoredDocument storedDocument)
    {
        var google = storedDocument.Google;
        var foodsByName = BuildFoodLookup(google);

        // O(n + f) — one pass over log entries, O(1) food lookup per entry
        var entries = new List<FoodEntry>();
        foreach (var dataPoint in DataPoints(google, "nutritionLog"))
        {
            if (dataPoint["nutritionLog"] is JObject log)
            {
                entries.Add(TranslateEntry(log, storedDocument.Date, foodsByName));
            }
        }

        var water = DataPoints(google, "hydrationLog")
            .Sum(dataPoint => Number(dataPoint.SelectToken("hydrationLog.amountConsumed.milliliters")));

        return new FoodDocument
        {
            Id = storedDocument.Id,
            Date = storedDocument.Date,
            DocumentType = storedDocument.DocumentType,
            Provider = storedDocument.Provider ?? GoogleProvider,
            SchemaVersion = 2,
            Food = new FoodResponse
            {
                Foods = entries,
                Goals = null,
                Summary = Summarise(entries, water)
            }
        };
    }

    private static FoodEntry TranslateEntry(JObject log, string date, Dictionary<string, JObject> foodsByName)
    {
        var foodName = Text(log["food"]);
        var food = foodName is not null && foodsByName.TryGetValue(foodName, out var match) ? match : null;
        var serving = log["serving"] as JObject;
        var calories = Number(log.SelectToken("energy.kcal"));
        var nutrients = SumNutrients(log["nutrients"] as JArray);

        return new FoodEntry
        {
            IsFavorite = null,
            LogDate = date,
            LogId = null,
            LoggedFood = new LoggedFood
            {
                AccessLevel = Text(food?["accessLevel"]),
                Amount = Number(serving?["amount"]),
                Brand = Text(food?["brand"]),
                Calories = calories,
                FoodId = null,
                Locale = Text(food?["languageCode"]),
                MealTypeId = MapMealType(Text(log["mealType"])),
                Name = Text(log["foodDisplayName"]) ?? string.Empty,
                Unit = TranslateUnit(serving, food),
                Units = null
            },
            NutritionalValues = new NutritionalValues
            {
                Calories = calories,
                Carbs = Number(log.SelectToken("totalCarbohydrate.grams")),
                Fat = Number(log.SelectToken("totalFat.grams")),
                Fiber = nutrients.Fiber,
                Protein = nutrients.Protein,
                Sodium = nutrients.SodiumGrams * MilligramsPerGram
            }
        };
    }

    private static Unit TranslateUnit(JObject? serving, JObject? food)
    {
        var measurementUnit = Text(serving?["foodMeasurementUnit"]);
        var foodServing = measurementUnit is null
            ? null
            : (food?["servings"] as JArray)?
                .OfType<JObject>()
                .FirstOrDefault(s => Text(s["foodMeasurementUnit"]) == measurementUnit);

        if (foodServing is not null)
        {
            return new Unit
            {
                Id = null,
                Name = Text(foodServing["foodMeasurementUnitDisplayName"]),
                Plural = Text(foodServing["foodMeasurementUnitDisplayNamePlural"])
            };
        }

        return new Unit
        {
            Id = null,
            Name = Text(serving?["foodMeasurementUnitDisplayName"]),
            Plural = null
        };
    }

    private static (double Fiber, double Protein, double SodiumGrams) SumNutrients(JArray? nutrients)
    {
        double fiber = 0, protein = 0, sodium = 0;
        if (nutrients is null)
        {
            return (fiber, protein, sodium);
        }

        foreach (var nutrient in nutrients.OfType<JObject>())
        {
            var grams = Number(nutrient.SelectToken("quantity.grams"));
            switch (Text(nutrient["nutrient"]))
            {
                case "DIETARY_FIBER": fiber += grams; break;
                case "PROTEIN": protein += grams; break;
                case "SODIUM": sodium += grams; break;
            }
        }

        return (fiber, protein, sodium);
    }

    private static Summary Summarise(List<FoodEntry> entries, double water)
    {
        var summary = new Summary { Water = water };
        foreach (var values in entries.Select(e => e.NutritionalValues))
        {
            summary.Calories += values.Calories;
            summary.Carbs += values.Carbs;
            summary.Fat += values.Fat;
            summary.Fiber += values.Fiber;
            summary.Protein += values.Protein;
            summary.Sodium += values.Sodium;
        }

        return summary;
    }

    private static int MapMealType(string? mealType) =>
        mealType is not null && MealTypeIds.TryGetValue(mealType, out var id) ? id : AnytimeMealTypeId;

    // O(f) space — one entry per distinct food resource name
    private static Dictionary<string, JObject> BuildFoodLookup(JObject? google)
    {
        var lookup = new Dictionary<string, JObject>(StringComparer.Ordinal);
        if ((google?["foods"] as JObject)?["items"] is not JArray items)
        {
            return lookup;
        }

        foreach (var item in items.OfType<JObject>())
        {
            if (Text(item["name"]) is { } name && item["food"] is JObject food)
            {
                lookup.TryAdd(name, food);
            }
        }

        return lookup;
    }

    // An empty Google result is stored as {} (no dataPoints), so absence means no data.
    private static IEnumerable<JObject> DataPoints(JObject? google, string sourceKey) =>
        ((google?[sourceKey] as JObject)?["dataPoints"] as JArray)?.OfType<JObject>() ?? [];

    private static string? Text(JToken? token) =>
        token?.Type == JTokenType.String ? token.Value<string>() : null;

    private static double Number(JToken? token) => token?.Type switch
    {
        JTokenType.Integer or JTokenType.Float => token.Value<double>(),
        JTokenType.String when double.TryParse(token.Value<string>(), NumberStyles.Float, CultureInfo.InvariantCulture, out var parsed) => parsed,
        _ => 0
    };
}
