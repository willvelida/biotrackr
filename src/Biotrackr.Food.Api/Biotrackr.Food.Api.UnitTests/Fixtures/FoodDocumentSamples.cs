namespace Biotrackr.Food.Api.UnitTests.Fixtures;

/// <summary>
/// Synthetic Cosmos documents. Version 2 samples follow contract C1 and the masked Google response shapes;
/// every value is made up.
/// </summary>
public static class FoodDocumentSamples
{
    public const string Version1Json = """
        {
          "id": "v1-doc-id",
          "date": "2025-12-01",
          "documentType": "Food",
          "food": {
            "foods": [
              {
                "isFavorite": true,
                "logDate": "2025-12-01",
                "logId": 987654321,
                "loggedFood": {
                  "accessLevel": "PUBLIC",
                  "amount": 1,
                  "brand": "Synthetic Farms",
                  "calories": 105,
                  "foodId": 4242,
                  "locale": "en_AU",
                  "mealTypeId": 1,
                  "name": "Synthetic Banana",
                  "unit": { "id": 304, "name": "serving", "plural": "servings" },
                  "units": [ 304, 226 ]
                },
                "nutritionalValues": {
                  "calories": 105, "carbs": 27, "fat": 0.4, "fiber": 3.1, "protein": 1.3, "sodium": 1
                }
              }
            ],
            "goals": { "calories": 2200 },
            "summary": {
              "calories": 105, "carbs": 27, "fat": 0.4, "fiber": 3.1, "protein": 1.3, "sodium": 1, "water": 750
            }
          },
          "_rid": "synthetic-rid",
          "_ts": 1764547200
        }
        """;

    public const string Version2Json = """
        {
          "id": "v2-doc-id",
          "date": "2026-01-15",
          "documentType": "Food",
          "provider": "Google",
          "schemaVersion": 2,
          "google": {
            "nutritionLog": {
              "dataPoints": [
                {
                  "name": "users/me/dataTypes/nutrition-log/dataPoints/synthetic-1",
                  "dataSource": { "recordingMethod": "MANUAL", "platform": "FITBIT" },
                  "nutritionLog": {
                    "interval": {
                      "startTime": "2026-01-14T21:00:00Z",
                      "startUtcOffset": "39600s",
                      "endTime": "2026-01-14T21:00:00Z",
                      "endUtcOffset": "39600s",
                      "civilStartTime": { "date": { "year": 2026, "month": 1, "day": 15 }, "time": { "hours": 8 } },
                      "civilEndTime": { "date": { "year": 2026, "month": 1, "day": 15 }, "time": { "hours": 8 } }
                    },
                    "nutrients": [
                      { "quantity": { "grams": 5.5, "userProvidedUnit": "GRAM" }, "nutrient": "DIETARY_FIBER" },
                      { "quantity": { "grams": 12.5, "userProvidedUnit": "GRAM" }, "nutrient": "PROTEIN" },
                      { "quantity": { "grams": 0.4, "userProvidedUnit": "GRAM" }, "nutrient": "SODIUM" },
                      { "quantity": { "grams": 9, "userProvidedUnit": "GRAM" }, "nutrient": "SUGAR" }
                    ],
                    "energy": { "kcal": 250.5, "userProvidedUnit": "KILOCALORIE" },
                    "totalCarbohydrate": { "grams": 30 },
                    "totalFat": { "grams": 10 },
                    "mealType": "BREAKFAST",
                    "serving": {
                      "amount": 2,
                      "foodMeasurementUnit": "users/me/dataTypes/food-measurement-unit/dataPoints/304",
                      "foodMeasurementUnitDisplayName": "serving"
                    },
                    "food": "users/me/foods/synthetic-1001",
                    "foodDisplayName": "Synthetic Oats"
                  }
                },
                {
                  "name": "users/me/dataTypes/nutrition-log/dataPoints/synthetic-2",
                  "dataSource": { "recordingMethod": "MANUAL", "platform": "FITBIT" },
                  "nutritionLog": {
                    "interval": {
                      "startTime": "2026-01-15T08:00:00Z",
                      "startUtcOffset": "39600s",
                      "endTime": "2026-01-15T08:00:00Z",
                      "endUtcOffset": "39600s",
                      "civilStartTime": { "date": { "year": 2026, "month": 1, "day": 15 }, "time": { "hours": 19 } },
                      "civilEndTime": { "date": { "year": 2026, "month": 1, "day": 15 }, "time": { "hours": 19 } }
                    },
                    "energy": { "kcal": 100, "userProvidedUnit": "KILOCALORIE" },
                    "mealType": "DINNER",
                    "foodDisplayName": "Synthetic Soup"
                  }
                }
              ]
            },
            "hydrationLog": {
              "dataPoints": [
                {
                  "name": "users/me/dataTypes/hydration-log/dataPoints/synthetic-h1",
                  "hydrationLog": {
                    "interval": { "startTime": "2026-01-14T22:00:00Z", "endTime": "2026-01-14T22:00:00Z" },
                    "amountConsumed": { "milliliters": 500, "userProvidedUnit": "MILLILITER" }
                  }
                },
                {
                  "name": "users/me/dataTypes/hydration-log/dataPoints/synthetic-h2",
                  "hydrationLog": {
                    "interval": { "startTime": "2026-01-15T02:00:00Z", "endTime": "2026-01-15T02:00:00Z" },
                    "amountConsumed": { "milliliters": 250.5 }
                  }
                }
              ]
            },
            "foods": {
              "items": [
                {
                  "name": "users/me/foods/synthetic-1001",
                  "food": {
                    "displayName": "Synthetic Oats",
                    "brand": "Synthetic Mills",
                    "accessLevel": "FOOD_ACCESS_LEVEL_PUBLIC",
                    "languageCode": "en_AU",
                    "energyAvg": { "kcal": 125.25 },
                    "defaultServing": {
                      "amount": 1,
                      "foodMeasurementUnit": "users/me/dataTypes/food-measurement-unit/dataPoints/304",
                      "foodMeasurementUnitDisplayName": "serving",
                      "foodMeasurementUnitDisplayNamePlural": "servings",
                      "multiplier": 1
                    }
                  }
                }
              ]
            }
          },
          "_rid": "synthetic-rid-2",
          "_ts": 1768435200
        }
        """;

    public const string Version2EmptyDayJson = """
        {
          "id": "v2-empty-id",
          "date": "2026-01-16",
          "documentType": "Food",
          "provider": "Google",
          "schemaVersion": 2,
          "google": {
            "nutritionLog": {},
            "hydrationLog": {},
            "foods": { "items": [] }
          }
        }
        """;
}
