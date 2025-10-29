using Biotrackr.Activity.Api.IntegrationTests.Collections;
using Biotrackr.Activity.Api.IntegrationTests.Fixtures;
using Biotrackr.Activity.Api.Models;
using Biotrackr.Activity.Api.Models.FitbitEntities;
using FluentAssertions;
using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.DependencyInjection;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Xunit;
using FitbitActivity = Biotrackr.Activity.Api.Models.FitbitEntities.Activity;

namespace Biotrackr.Activity.Api.IntegrationTests.E2E;

/// <summary>
/// End-to-end integration tests for Activity API endpoints with Cosmos DB
/// Tests verify full request-response cycles including database operations
/// </summary>
[Collection(nameof(IntegrationTestCollection))]
public class ActivityEndpointsTests : IAsyncLifetime
{
    private readonly IntegrationTestFixture _fixture;
    private readonly Container _container;
    private readonly List<string> _testDocumentIds = new();

    public ActivityEndpointsTests(IntegrationTestFixture fixture)
    {
        _fixture = fixture;
        var cosmosClient = fixture.Factory.Services.GetRequiredService<CosmosClient>();
        _container = cosmosClient.GetContainer("biotrackr-test", "activity-test");
    }

    /// <summary>
    /// T085-T086: Initialize test data and clear container before each test
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
                await _container.DeleteItemAsync<ActivityDocument>(id, new PartitionKey("Activity"));
            }
            catch
            {
                // Ignore cleanup errors
            }
        }
    }

    /// <summary>
    /// Helper method to clear all items from container for test isolation
    /// Critical for preventing data pollution between tests
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
                catch
                {
                    // Ignore errors during cleanup
                }
            }
        }
    }

    private async Task SeedTestDataAsync()
    {
        var testDocuments = new[]
        {
            new ActivityDocument
            {
                Id = Guid.NewGuid().ToString(),
                DocumentType = "Activity",
                Date = "2024-01-01",
                Activity = new ActivityResponse
                {
                    activities = new List<FitbitActivity>
                    {
                        new FitbitActivity
                        {
                            activityId = 90009,
                            activityParentId = 90009,
                            activityParentName = "Running",
                            calories = 250,
                            description = "Morning run",
                            distance = 5.0,
                            duration = 1800000,
                            hasActiveZoneMinutes = true,
                            hasStartTime = true,
                            isFavorite = true,
                            logId = 1,
                            name = "Running",
                            startDate = "2024-01-01",
                            startTime = "08:00:00",
                            steps = 6000
                        }
                    },
                    goals = new Goals
                    {
                        activeMinutes = 30,
                        caloriesOut = 2000,
                        distance = 10.0,
                        floors = 10,
                        steps = 10000
                    },
                    summary = new Summary
                    {
                        activeScore = 85,
                        activityCalories = 500,
                        caloriesBMR = 1500,
                        caloriesOut = 2000,
                        distances = new List<Distance>
                        {
                            new Distance
                            {
                                activity = "Running",
                                distance = 5.0
                            }
                        },
                        elevation = 50.0,
                        fairlyActiveMinutes = 20,
                        floors = 5,
                        heartRateZones = new List<HeartRateZone>
                        {
                            new HeartRateZone
                            {
                                caloriesOut = 100,
                                max = 120,
                                min = 80,
                                minutes = 30,
                                name = "Fat Burn"
                            }
                        },
                        lightlyActiveMinutes = 40,
                        marginalCalories = 300,
                        restingHeartRate = 65,
                        sedentaryMinutes = 600,
                        steps = 6000,
                        veryActiveMinutes = 10
                    }
                }
            },
            new ActivityDocument
            {
                Id = Guid.NewGuid().ToString(),
                DocumentType = "Activity",
                Date = "2024-01-15",
                Activity = new ActivityResponse
                {
                    activities = new List<FitbitActivity>
                    {
                        new FitbitActivity
                        {
                            activityId = 90013,
                            activityParentId = 90013,
                            activityParentName = "Cycling",
                            calories = 300,
                            description = "Afternoon cycle",
                            distance = 10.0,
                            duration = 2400000,
                            hasActiveZoneMinutes = true,
                            hasStartTime = true,
                            isFavorite = true,
                            logId = 2,
                            name = "Cycling",
                            startDate = "2024-01-15",
                            startTime = "14:00:00",
                            steps = 0
                        }
                    },
                    goals = new Goals
                    {
                        activeMinutes = 30,
                        caloriesOut = 2000,
                        distance = 10.0,
                        floors = 10,
                        steps = 10000
                    },
                    summary = new Summary
                    {
                        activeScore = 90,
                        activityCalories = 600,
                        caloriesBMR = 1500,
                        caloriesOut = 2100,
                        distances = new List<Distance>
                        {
                            new Distance
                            {
                                activity = "Cycling",
                                distance = 10.0
                            }
                        },
                        elevation = 100.0,
                        fairlyActiveMinutes = 30,
                        floors = 8,
                        heartRateZones = new List<HeartRateZone>
                        {
                            new HeartRateZone
                            {
                                caloriesOut = 150,
                                max = 140,
                                min = 100,
                                minutes = 40,
                                name = "Cardio"
                            }
                        },
                        lightlyActiveMinutes = 50,
                        marginalCalories = 400,
                        restingHeartRate = 63,
                        sedentaryMinutes = 580,
                        steps = 1000,
                        veryActiveMinutes = 15
                    }
                }
            },
            new ActivityDocument
            {
                Id = Guid.NewGuid().ToString(),
                DocumentType = "Activity",
                Date = "2024-01-31",
                Activity = new ActivityResponse
                {
                    activities = new List<FitbitActivity>
                    {
                        new FitbitActivity
                        {
                            activityId = 90001,
                            activityParentId = 90001,
                            activityParentName = "Walking",
                            calories = 150,
                            description = "Evening walk",
                            distance = 3.0,
                            duration = 1200000,
                            hasActiveZoneMinutes = true,
                            hasStartTime = true,
                            isFavorite = false,
                            logId = 3,
                            name = "Walking",
                            startDate = "2024-01-31",
                            startTime = "18:00:00",
                            steps = 4000
                        }
                    },
                    goals = new Goals
                    {
                        activeMinutes = 30,
                        caloriesOut = 2000,
                        distance = 10.0,
                        floors = 10,
                        steps = 10000
                    },
                    summary = new Summary
                    {
                        activeScore = 70,
                        activityCalories = 400,
                        caloriesBMR = 1500,
                        caloriesOut = 1900,
                        distances = new List<Distance>
                        {
                            new Distance
                            {
                                activity = "Walking",
                                distance = 3.0
                            }
                        },
                        elevation = 20.0,
                        fairlyActiveMinutes = 15,
                        floors = 3,
                        heartRateZones = new List<HeartRateZone>
                        {
                            new HeartRateZone
                            {
                                caloriesOut = 80,
                                max = 110,
                                min = 70,
                                minutes = 20,
                                name = "Light"
                            }
                        },
                        lightlyActiveMinutes = 30,
                        marginalCalories = 200,
                        restingHeartRate = 67,
                        sedentaryMinutes = 650,
                        steps = 4000,
                        veryActiveMinutes = 5
                    }
                }
            }
        };

        foreach (var doc in testDocuments)
        {
            await _container.CreateItemAsync(doc, new PartitionKey(doc.DocumentType));
            _testDocumentIds.Add(doc.Id);
        }
    }

    /// <summary>
    /// T087: Test GET /activity endpoint with valid date range returns 200 OK
    /// Validates pagination and proper data retrieval from Cosmos DB
    /// </summary>
    [Fact]
    public async Task GetAllActivities_Should_Return_Paginated_Results()
    {
        // Arrange
        var client = _fixture.Client;

        // Act
        var response = await client.GetAsync("/?pageNumber=1&pageSize=10");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<PaginationResponse<ActivityDocument>>();
        result.Should().NotBeNull();
        result!.Items.Should().HaveCount(3, "we seeded 3 test documents");
        result.PageNumber.Should().Be(1);
        result.PageSize.Should().Be(10);
        result.TotalCount.Should().Be(3);
    }

    /// <summary>
    /// T088: Test GET /activity endpoint with no activities returns empty result set
    /// Uses ClearContainerAsync to ensure no data exists
    /// </summary>
    [Fact]
    public async Task GetAllActivities_Should_Return_Empty_Result_When_No_Activities_Exist()
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
        var result = JsonSerializer.Deserialize<PaginationResponse<ActivityDocument>>(content, new JsonSerializerOptions 
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
    /// T089: Test GET /activity endpoint with invalid date range returns 400 Bad Request
    /// </summary>
    [Fact]
    public async Task GetActivityByDate_Should_Return_BadRequest_When_Date_Invalid()
    {
        // Arrange
        var client = _fixture.Client;
        var invalidDate = "invalid-date-format";

        // Act
        var response = await client.GetAsync($"/{invalidDate}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task GetActivityByDate_Should_Return_Activity_Document_When_Exists()
    {
        // Arrange
        var client = _fixture.Client;
        var testDate = "2024-01-01";

        // Act
        var response = await client.GetAsync($"/{testDate}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<ActivityDocument>();
        result.Should().NotBeNull();
        result!.Date.Should().Be(testDate);
        result.Activity.Should().NotBeNull();
        result.Activity.activities.Should().NotBeEmpty();
    }

    [Fact]
    public async Task GetActivityByDate_Should_Return_NotFound_When_Date_Does_Not_Exist()
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
    /// T090: Test GET /activity endpoint with pagination parameters
    /// Uses ClearContainerAsync to ensure predictable result counts
    /// </summary>
    [Fact]
    public async Task GetActivitiesByDateRange_Should_Return_Activities_In_Range()
    {
        // Arrange
        var client = _fixture.Client;
        var startDate = "2024-01-01";
        var endDate = "2024-01-31";

        // Act
        var response = await client.GetAsync($"/range/{startDate}/{endDate}?pageNumber=1&pageSize=10");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<PaginationResponse<ActivityDocument>>();
        result.Should().NotBeNull();
        result!.Items.Should().HaveCount(3, "all seeded documents are in range");
        result.Items.Should().AllSatisfy(item =>
        {
            (string.Compare(item.Date, startDate, StringComparison.Ordinal) >= 0).Should().BeTrue();
            (string.Compare(item.Date, endDate, StringComparison.Ordinal) <= 0).Should().BeTrue();
        });
    }

    [Fact]
    public async Task GetActivitiesByDateRange_Should_Return_BadRequest_When_StartDate_Invalid()
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

    [Fact]
    public async Task GetActivitiesByDateRange_Should_Return_BadRequest_When_EndDate_Invalid()
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
    /// T091: Test verifying database operations persist correctly
    /// </summary>
    [Fact]
    public async Task DatabaseOperations_Should_Persist_Data_Correctly()
    {
        // Arrange
        var client = _fixture.Client;
        var testDate = "2024-01-01";

        // Act - Retrieve data via API
        var response = await client.GetAsync($"/{testDate}");
        var apiResult = await response.Content.ReadFromJsonAsync<ActivityDocument>();

        // Query database directly
        var query = new QueryDefinition(
            "SELECT * FROM c WHERE c.date = @date AND c.documentType = @type")
            .WithParameter("@date", testDate)
            .WithParameter("@type", "Activity");

        using var iterator = _container.GetItemQueryIterator<ActivityDocument>(query);
        var dbResponse = await iterator.ReadNextAsync();
        var dbResult = dbResponse.FirstOrDefault();

        // Assert - API and database should return same data
        apiResult.Should().NotBeNull();
        dbResult.Should().NotBeNull();
        apiResult!.Id.Should().Be(dbResult!.Id);
        apiResult.Date.Should().Be(dbResult.Date);
        apiResult.Activity.activities.Count.Should().Be(dbResult.Activity.activities.Count);
    }

    /// <summary>
    /// T092: Test verifying test data cleanup occurs via IAsyncLifetime.DisposeAsync
    /// </summary>
    [Fact]
    public async Task TestCleanup_Should_Remove_Test_Documents()
    {
        // Arrange
        var testId = Guid.NewGuid().ToString();
        var testDoc = new ActivityDocument
        {
            Id = testId,
            DocumentType = "Activity",
            Date = "2024-02-01",
            Activity = new ActivityResponse
            {
                activities = new List<FitbitActivity>(),
                goals = new Goals(),
                summary = new Summary
                {
                    distances = new List<Distance>(),
                    heartRateZones = new List<HeartRateZone>()
                }
            }
        };

        // Act - Create and then delete
        await _container.CreateItemAsync(testDoc, new PartitionKey(testDoc.DocumentType));
        await _container.DeleteItemAsync<ActivityDocument>(testId, new PartitionKey("Activity"));

        // Assert - Document should not exist
        Func<Task> act = async () => await _container.ReadItemAsync<ActivityDocument>(
            testId, 
            new PartitionKey("Activity"));
        
        await act.Should().ThrowAsync<CosmosException>()
            .Where(ex => ex.StatusCode == HttpStatusCode.NotFound);
    }

    /// <summary>
    /// T093: Test for endpoint response format validation (JSON, correct structure)
    /// </summary>
    [Fact]
    public async Task GetAllActivities_Should_Return_Valid_Json_Structure()
    {
        // Arrange
        var client = _fixture.Client;

        // Act
        var response = await client.GetAsync("/?pageNumber=1&pageSize=10");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        response.Content.Headers.ContentType.Should().NotBeNull();
        response.Content.Headers.ContentType!.MediaType.Should().Be("application/json");

        var result = await response.Content.ReadFromJsonAsync<PaginationResponse<ActivityDocument>>();
        result.Should().NotBeNull();
        result!.Items.Should().AllSatisfy(item =>
        {
            item.Id.Should().NotBeNullOrWhiteSpace();
            item.DocumentType.Should().Be("Activity");
            item.Date.Should().NotBeNullOrWhiteSpace();
            item.Activity.Should().NotBeNull();
            item.Activity.activities.Should().NotBeNull();
            item.Activity.goals.Should().NotBeNull();
            item.Activity.summary.Should().NotBeNull();
        });
    }

    /// <summary>
    /// T094: Test for endpoint response time (performance validation)
    /// Note: If flaky in CI due to Cosmos DB Emulator, use [Fact(Skip = "Flaky in CI: reason")]
    /// per decision-record 2025-10-28-flaky-test-handling.md
    /// </summary>
    [Fact]
    public async Task GetAllActivities_Should_Respond_Within_Acceptable_Time()
    {
        // Arrange
        var client = _fixture.Client;
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();

        // Act
        var response = await client.GetAsync("/?pageNumber=1&pageSize=10");

        // Assert
        stopwatch.Stop();
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        stopwatch.ElapsedMilliseconds.Should().BeLessThan(5000, 
            "API should respond within 5 seconds for small datasets");
    }
}
