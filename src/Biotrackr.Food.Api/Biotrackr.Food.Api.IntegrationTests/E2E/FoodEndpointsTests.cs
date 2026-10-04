using Biotrackr.Food.Api.IntegrationTests.Fixtures;
using Biotrackr.Food.Api.Models;
using Biotrackr.Food.Api.Models.FitbitEntities;
using FluentAssertions;
using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.DependencyInjection;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Xunit;
using FoodEntity = Biotrackr.Food.Api.Models.FitbitEntities.Food;

namespace Biotrackr.Food.Api.IntegrationTests.E2E;

/// <summary>
/// End-to-end integration tests for Food API endpoints with Cosmos DB
/// Tests verify full request-response cycles including database operations
/// </summary>
[Collection(nameof(IntegrationTestCollection))]
public class FoodEndpointsTests : IAsyncLifetime
{
    private readonly IntegrationTestFixture _fixture;
    private readonly Container _container;
    private readonly List<string> _testDocumentIds = new();

    public FoodEndpointsTests(IntegrationTestFixture fixture)
    {
        _fixture = fixture;
        var cosmosClient = fixture.Factory.Services.GetRequiredService<CosmosClient>();
        _container = cosmosClient.GetContainer("biotrackr-test", "food-test");
    }

    /// <summary>
    /// Initialize test data and clear container before each test
    /// Per common-resolutions.md: Use ClearContainerAsync() for test isolation
    /// </summary>
    public async Task InitializeAsync()
    {
        // Clear container to ensure test isolation
        await ClearContainerAsync();
        
        // Seed test data
        await SeedTestDataAsync();
    }

    /// <summary>
    /// Clean up test data after test execution
    /// Collection-level cleanup via IAsyncLifetime.DisposeAsync
    /// </summary>
    public async Task DisposeAsync()
    {
        // Clean up test data
        foreach (var id in _testDocumentIds)
        {
            try
            {
                await _container.DeleteItemAsync<FoodStoredDocument>(id, new PartitionKey("Food"));
            }
            catch (Exception ex)
            {
                // Log but don't fail test on cleanup errors
                Console.WriteLine($"Failed to delete test document {id}: {ex.Message}");
            }
        }
    }

    /// <summary>
    /// Helper method to clear all items from container for test isolation
    /// Critical for preventing data pollution between tests
    /// Per common-resolutions.md: Always use ClearContainerAsync for E2E test isolation
    /// </summary>
    private async Task ClearContainerAsync()
    {
        var query = new QueryDefinition("SELECT c.id, c.documentType FROM c");
        using var iterator = _container.GetItemQueryIterator<dynamic>(query);

        while (iterator.HasMoreResults)
        {
            var response = await iterator.ReadNextAsync();
            foreach (var item in response)
            {
                try
                {
                    await _container.DeleteItemAsync<dynamic>(
                        item.id.ToString(),
                        new PartitionKey(item.documentType.ToString()));
                }
                catch (Exception ex)
                {
                    // Log but don't fail on cleanup errors
                    Console.WriteLine($"Failed to delete document {item.id}: {ex.Message}");
                }
            }
        }
    }

    private async Task SeedTestDataAsync()
    {
        var testDocuments = new[]
        {
            new FoodStoredDocument
            {
                Id = Guid.NewGuid().ToString(),
                DocumentType = "Food",
                Date = "2024-01-01",
                Food = new FoodResponse
                {
                    Foods = new List<FoodEntity>
                    {
                        new FoodEntity
                        {
                            IsFavorite = false,
                            LogDate = "2024-01-01",
                            LogId = 1,
                            LoggedFood = new LoggedFood
                            {
                                AccessLevel = "PUBLIC",
                                Amount = 1,
                                Brand = "Generic",
                                Calories = 95,
                                FoodId = 123456,
                                Locale = "en_US",
                                MealTypeId = 1,
                                Name = "Apple",
                                Unit = new Unit
                                {
                                    Id = 200,
                                    Name = "piece",
                                    Plural = "pieces"
                                },
                                Units = new List<int> { 200 }
                            },
                            NutritionalValues = new NutritionalValues
                            {
                                Calories = 95,
                                Carbs = 25.0,
                                Fat = 0.3,
                                Fiber = 4.0,
                                Protein = 0.5,
                                Sodium = 2.0
                            }
                        }
                    },
                    Summary = new Summary
                    {
                        Calories = 2000,
                        Carbs = 250.0,
                        Fat = 70.0,
                        Fiber = 25.0,
                        Protein = 80.0,
                        Sodium = 2300.0,
                        Water = 2000.0
                    },
                    Goals = new Goals
                    {
                        Calories = 2200
                    }
                }
            },
            new FoodStoredDocument
            {
                Id = Guid.NewGuid().ToString(),
                DocumentType = "Food",
                Date = "2024-01-15",
                Food = new FoodResponse
                {
                    Foods = new List<FoodEntity>
                    {
                        new FoodEntity
                        {
                            IsFavorite = false,
                            LogDate = "2024-01-15",
                            LogId = 2,
                            LoggedFood = new LoggedFood
                            {
                                AccessLevel = "PUBLIC",
                                Amount = 1,
                                Brand = "Brand X",
                                Calories = 250,
                                FoodId = 234567,
                                Locale = "en_US",
                                MealTypeId = 2,
                                Name = "Chicken Breast",
                                Unit = new Unit
                                {
                                    Id = 300,
                                    Name = "oz",
                                    Plural = "oz"
                                },
                                Units = new List<int> { 300 }
                            },
                            NutritionalValues = new NutritionalValues
                            {
                                Calories = 250,
                                Carbs = 0.0,
                                Fat = 3.5,
                                Fiber = 0.0,
                                Protein = 53.0,
                                Sodium = 80.0
                            }
                        }
                    },
                    Summary = new Summary
                    {
                        Calories = 2100,
                        Carbs = 200.0,
                        Fat = 65.0,
                        Fiber = 30.0,
                        Protein = 120.0,
                        Sodium = 2000.0,
                        Water = 2200.0
                    },
                    Goals = new Goals
                    {
                        Calories = 2200
                    }
                }
            },
            new FoodStoredDocument
            {
                Id = Guid.NewGuid().ToString(),
                DocumentType = "Food",
                Date = "2024-01-31",
                Food = new FoodResponse
                {
                    Foods = new List<FoodEntity>
                    {
                        new FoodEntity
                        {
                            IsFavorite = true,
                            LogDate = "2024-01-31",
                            LogId = 3,
                            LoggedFood = new LoggedFood
                            {
                                AccessLevel = "PUBLIC",
                                Amount = 2,
                                Brand = "Brand Y",
                                Calories = 140,
                                FoodId = 345678,
                                Locale = "en_US",
                                MealTypeId = 3,
                                Name = "Greek Yogurt",
                                Unit = new Unit
                                {
                                    Id = 400,
                                    Name = "cup",
                                    Plural = "cups"
                                },
                                Units = new List<int> { 400 }
                            },
                            NutritionalValues = new NutritionalValues
                            {
                                Calories = 140,
                                Carbs = 10.0,
                                Fat = 4.0,
                                Fiber = 0.0,
                                Protein = 20.0,
                                Sodium = 75.0
                            }
                        }
                    },
                    Summary = new Summary
                    {
                        Calories = 1950,
                        Carbs = 230.0,
                        Fat = 60.0,
                        Fiber = 28.0,
                        Protein = 100.0,
                        Sodium = 2100.0,
                        Water = 2100.0
                    },
                    Goals = new Goals
                    {
                        Calories = 2200
                    }
                }
            }
        };

        foreach (var document in testDocuments)
        {
            await _container.CreateItemAsync(document, new PartitionKey(document.DocumentType));
            _testDocumentIds.Add(document.Id);
        }
    }

    /// <summary>
    /// Test GET / endpoint returns paginated results with all food logs
    /// </summary>
    [Fact]
    public async Task GetAllFoodLogs_Should_Return_Paginated_Results()
    {
        // Arrange
        var client = _fixture.Client;

        // Act
        var response = await client.GetAsync("/?pageNumber=1&pageSize=10");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<PaginationResponse<FoodDocument>>();
        result.Should().NotBeNull();
        result!.Items.Should().HaveCount(3, "we seeded 3 test documents");
        result.PageNumber.Should().Be(1);
        result.PageSize.Should().Be(10);
        result.TotalCount.Should().Be(3);
    }

    /// <summary>
    /// Test GET / endpoint with no food logs returns empty result set
    /// Uses ClearContainerAsync to ensure no data exists
    /// </summary>
    [Fact]
    public async Task GetAllFoodLogs_Should_Return_Empty_Result_When_No_FoodLogs_Exist()
    {
        // Arrange
        var client = _fixture.Client;
        
        // Clear all data to ensure empty container
        await ClearContainerAsync();
        _testDocumentIds.Clear();

        // Act
        var response = await client.GetAsync("/?pageNumber=1&pageSize=10");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        
        var content = await response.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<PaginationResponse<FoodDocument>>(content, new JsonSerializerOptions 
        { 
            PropertyNameCaseInsensitive = true 
        });

        result.Should().NotBeNull();
        result!.Items.Should().BeEmpty();
        result.PageNumber.Should().Be(1);
        result.PageSize.Should().Be(10);
        result.TotalCount.Should().Be(0);
    }

    /// <summary>
    /// Test GET /{date} endpoint with invalid date returns 400 Bad Request
    /// </summary>
    [Fact]
    public async Task GetFoodLogByDate_Should_Return_BadRequest_When_Date_Invalid()
    {
        // Arrange
        var client = _fixture.Client;
        var invalidDate = "invalid-date-format";

        // Act
        var response = await client.GetAsync($"/{invalidDate}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    /// <summary>
    /// Test GET /{date} endpoint returns food document when date exists
    /// </summary>
    [Fact]
    public async Task GetFoodLogByDate_Should_Return_FoodDocument_When_Exists()
    {
        // Arrange
        var client = _fixture.Client;
        var testDate = "2024-01-01";

        // Act
        var response = await client.GetAsync($"/{testDate}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<FoodDocument>();
        result.Should().NotBeNull();
        result!.Date.Should().Be(testDate);
        result.Food.Should().NotBeNull();
        result.Food.Foods.Should().NotBeEmpty();
    }

    /// <summary>
    /// Test GET /{date} endpoint returns NotFound when date doesn't exist
    /// </summary>
    [Fact]
    public async Task GetFoodLogByDate_Should_Return_NotFound_When_Date_Does_Not_Exist()
    {
        // Arrange
        var client = _fixture.Client;
        var nonExistentDate = "2099-12-31";

        // Act
        var response = await client.GetAsync($"/{nonExistentDate}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    /// <summary>
    /// Test GET /range/{startDate}/{endDate} endpoint returns food logs in date range
    /// </summary>
    [Fact]
    public async Task GetFoodLogsByDateRange_Should_Return_FoodLogs_In_Range()
    {
        // Arrange
        var client = _fixture.Client;
        var startDate = "2024-01-01";
        var endDate = "2024-01-31";

        // Act
        var response = await client.GetAsync($"/range/{startDate}/{endDate}?pageNumber=1&pageSize=10");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<PaginationResponse<FoodDocument>>();
        result.Should().NotBeNull();
        result!.Items.Should().HaveCount(3, "all seeded documents are in range");
        result.Items.Should().AllSatisfy(item =>
        {
            (string.Compare(item.Date, startDate, StringComparison.Ordinal) >= 0).Should().BeTrue();
            (string.Compare(item.Date, endDate, StringComparison.Ordinal) <= 0).Should().BeTrue();
        });
    }

    /// <summary>
    /// Test GET /range/{startDate}/{endDate} endpoint with invalid startDate returns 400
    /// </summary>
    [Fact]
    public async Task GetFoodLogsByDateRange_Should_Return_BadRequest_When_StartDate_Invalid()
    {
        // Arrange
        var client = _fixture.Client;
        var invalidStartDate = "invalid-date";
        var endDate = "2024-01-31";

        // Act
        var response = await client.GetAsync($"/range/{invalidStartDate}/{endDate}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    /// <summary>
    /// Test GET /range/{startDate}/{endDate} endpoint with invalid endDate returns 400
    /// </summary>
    [Fact]
    public async Task GetFoodLogsByDateRange_Should_Return_BadRequest_When_EndDate_Invalid()
    {
        // Arrange
        var client = _fixture.Client;
        var startDate = "2024-01-01";
        var invalidEndDate = "invalid-date";

        // Act
        var response = await client.GetAsync($"/range/{startDate}/{invalidEndDate}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    /// <summary>
    /// Test GET /range/{startDate}/{endDate} endpoint returns empty when no logs in range
    /// </summary>
    [Fact]
    public async Task GetFoodLogsByDateRange_Should_Return_Empty_When_No_Logs_In_Range()
    {
        // Arrange
        var client = _fixture.Client;
        var startDate = "2099-01-01";
        var endDate = "2099-12-31";

        // Act
        var response = await client.GetAsync($"/range/{startDate}/{endDate}?pageNumber=1&pageSize=10");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<PaginationResponse<FoodDocument>>();
        result.Should().NotBeNull();
        result!.Items.Should().BeEmpty();
        result.TotalCount.Should().Be(0);
    }

    /// <summary>
    /// Test GET / endpoint with custom pagination parameters
    /// </summary>
    [Fact]
    public async Task GetAllFoodLogs_Should_Respect_Pagination_Parameters()
    {
        // Arrange
        var client = _fixture.Client;
        var pageNumber = 1;
        var pageSize = 2;

        // Act
        var response = await client.GetAsync($"/?pageNumber={pageNumber}&pageSize={pageSize}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<PaginationResponse<FoodDocument>>();
        result.Should().NotBeNull();
        result!.Items.Should().HaveCountLessThanOrEqualTo(pageSize);
        result.PageNumber.Should().Be(pageNumber);
        result.PageSize.Should().Be(pageSize);
        result.TotalCount.Should().Be(3);
    }

    /// <summary>
    /// Test GET /range/{startDate}/{endDate} endpoint with partial date range
    /// </summary>
    [Fact]
    public async Task GetFoodLogsByDateRange_Should_Return_Partial_Results()
    {
        // Arrange
        var client = _fixture.Client;
        var startDate = "2024-01-01";
        var endDate = "2024-01-15"; // Only includes first two documents

        // Act
        var response = await client.GetAsync($"/range/{startDate}/{endDate}?pageNumber=1&pageSize=10");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<PaginationResponse<FoodDocument>>();
        result.Should().NotBeNull();
        result!.Items.Should().HaveCount(2, "only two documents fall within this range");
        result.Items.Should().AllSatisfy(item =>
        {
            (string.Compare(item.Date, startDate, StringComparison.Ordinal) >= 0).Should().BeTrue();
            (string.Compare(item.Date, endDate, StringComparison.Ordinal) <= 0).Should().BeTrue();
        });
    }

    /// <summary>
    /// Test health check endpoint returns OK status
    /// </summary>
    [Fact]
    public async Task HealthCheck_Should_Return_OK()
    {
        // Arrange
        var client = _fixture.Client;

        // Act
        var response = await client.GetAsync("/healthz/liveness");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    private const string Version1RawJson = """
        {
          "id": "e2e-v1-no-schema",
          "date": "2025-06-01",
          "documentType": "Food",
          "food": {
            "foods": [
              {
                "isFavorite": false,
                "logDate": "2025-06-01",
                "logId": 111222333,
                "loggedFood": {
                  "accessLevel": "PUBLIC", "amount": 1, "brand": "Synthetic Farms", "calories": 95,
                  "foodId": 5150, "locale": "en_AU", "mealTypeId": 1, "name": "Synthetic Apple",
                  "unit": { "id": 304, "name": "serving", "plural": "servings" }, "units": [ 304 ]
                },
                "nutritionalValues": { "calories": 95, "carbs": 25, "fat": 0.3, "fiber": 4.4, "protein": 0.5, "sodium": 2 }
              }
            ],
            "goals": { "calories": 2000 },
            "summary": { "calories": 95, "carbs": 25, "fat": 0.3, "fiber": 4.4, "protein": 0.5, "sodium": 2, "water": 500 }
          }
        }
        """;

    private const string Version2RawJson = """
        {
          "id": "e2e-v2-google",
          "date": "2025-06-02",
          "documentType": "Food",
          "provider": "Google",
          "schemaVersion": 2,
          "google": {
            "nutritionLog": {
              "dataPoints": [
                {
                  "name": "users/me/dataTypes/nutrition-log/dataPoints/e2e-1",
                  "nutritionLog": {
                    "interval": { "civilStartTime": { "date": { "year": 2025, "month": 6, "day": 2 }, "time": { "hours": 8 } } },
                    "nutrients": [
                      { "quantity": { "grams": 3 }, "nutrient": "DIETARY_FIBER" },
                      { "quantity": { "grams": 10 }, "nutrient": "PROTEIN" },
                      { "quantity": { "grams": 0.25 }, "nutrient": "SODIUM" }
                    ],
                    "energy": { "kcal": 320 },
                    "totalCarbohydrate": { "grams": 40 },
                    "totalFat": { "grams": 12 },
                    "mealType": "LUNCH",
                    "serving": { "amount": 1.5, "foodMeasurementUnit": "users/me/dataTypes/food-measurement-unit/dataPoints/304" },
                    "food": "users/me/foods/e2e-food",
                    "foodDisplayName": "Synthetic Wrap"
                  }
                }
              ]
            },
            "hydrationLog": {
              "dataPoints": [
                { "hydrationLog": { "amountConsumed": { "milliliters": 600 } } },
                { "hydrationLog": { "amountConsumed": { "milliliters": 400 } } }
              ]
            },
            "foods": {
              "items": [
                {
                  "name": "users/me/foods/e2e-food",
                  "food": {
                    "displayName": "Synthetic Wrap", "brand": "Synthetic Deli", "accessLevel": "FOOD_ACCESS_LEVEL_PRIVATE",
                    "languageCode": "en_AU",
                    "servings": [
                      { "foodMeasurementUnit": "users/me/dataTypes/food-measurement-unit/dataPoints/304",
                        "foodMeasurementUnitDisplayName": "wrap", "foodMeasurementUnitDisplayNamePlural": "wraps" }
                    ]
                  }
                }
              ]
            }
          }
        }
        """;

    private async Task SeedRawAsync(string json)
    {
        using var stream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(json));
        using var response = await _container.CreateItemStreamAsync(stream, new PartitionKey("Food"));
        response.IsSuccessStatusCode.Should().BeTrue($"seeding a raw document should succeed but returned {response.StatusCode}");
        _testDocumentIds.Add(System.Text.Json.Nodes.JsonNode.Parse(json)!["id"]!.GetValue<string>());
    }

    /// <summary>
    /// AC 17: a pre-cutover document without schemaVersion keeps every field and gains provider Fitbit, schemaVersion 1
    /// </summary>
    [Fact]
    public async Task GetFoodLogByDate_Should_Return_Version1_Unchanged_When_Document_Has_No_SchemaVersion()
    {
        // Arrange
        await SeedRawAsync(Version1RawJson);
        var expectedFood = System.Text.Json.Nodes.JsonNode.Parse(Version1RawJson)!["food"];

        // Act
        var response = await _fixture.Client.GetAsync("/2025-06-01");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = System.Text.Json.Nodes.JsonNode.Parse(await response.Content.ReadAsStringAsync())!;
        body["provider"]!.GetValue<string>().Should().Be("Fitbit");
        body["schemaVersion"]!.GetValue<int>().Should().Be(1);
        System.Text.Json.Nodes.JsonNode.DeepEquals(body["food"], expectedFood).Should().BeTrue(
            $"version 1 food must match the stored document. Expected {expectedFood!.ToJsonString()} but got {body["food"]!.ToJsonString()}");
    }

    /// <summary>
    /// AC 20, 23: a version 2 document is translated from the raw Google payloads per contract C2
    /// </summary>
    [Fact]
    public async Task GetFoodLogByDate_Should_Return_Translated_Version2_When_Document_Is_Google()
    {
        // Arrange
        await SeedRawAsync(Version2RawJson);

        // Act
        var response = await _fixture.Client.GetAsync("/2025-06-02");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = System.Text.Json.Nodes.JsonNode.Parse(await response.Content.ReadAsStringAsync())!;
        var entry = body["food"]!["foods"]![0]!;
        new object?[]
        {
            body["provider"]!.GetValue<string>(), body["schemaVersion"]!.GetValue<int>(), body["google"],
            body["food"]!["goals"], entry["loggedFood"]!["name"]!.GetValue<string>(),
            entry["loggedFood"]!["brand"]!.GetValue<string>(), entry["loggedFood"]!["mealTypeId"]!.GetValue<int>(),
            entry["loggedFood"]!["amount"]!.GetValue<double>(), entry["loggedFood"]!["unit"]!["name"]!.GetValue<string>(),
            entry["nutritionalValues"]!["sodium"]!.GetValue<double>(), body["food"]!["summary"]!["water"]!.GetValue<double>()
        }.Should().Equal(new object?[]
        {
            "Google", 2, null, null, "Synthetic Wrap", "Synthetic Deli", 3, 1.5, "wrap", 250.0, 1000.0
        }, "the response must follow C2 and never expose the raw google payload");
    }

    /// <summary>
    /// AC 18: a range spanning both versions returns every document in one paginated response
    /// </summary>
    [Fact]
    public async Task GetFoodLogsByDateRange_Should_Return_Both_Versions_When_Range_Spans_Cutover()
    {
        // Arrange
        await SeedRawAsync(Version1RawJson);
        await SeedRawAsync(Version2RawJson);

        // Act
        var response = await _fixture.Client.GetAsync("/range/2025-06-01/2025-06-02");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<PaginationResponse<FoodDocument>>();
        result!.TotalCount.Should().Be(2);
        result.Items.Select(i => (i.Date, i.Provider, i.SchemaVersion))
            .Should().Equal(("2025-06-01", "Fitbit", 1), ("2025-06-02", "Google", 2));
    }
}
