using Biotrackr.Weight.Api.Models;
using Biotrackr.Weight.Api.Models.FitbitEntities;
using FluentAssertions;
using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.DependencyInjection;
using System.Net;
using System.Net.Http.Json;
using Xunit;
using FitbitWeight = Biotrackr.Weight.Api.Models.FitbitEntities.Weight;

namespace Biotrackr.Weight.Api.IntegrationTests;

/// <summary>
/// End-to-end integration tests for Weight API endpoints
/// </summary>
[Collection(nameof(IntegrationTestCollection))]
public class WeightEndpointsTests : IAsyncLifetime
{
    private readonly IntegrationTestFixture _fixture;
    private readonly Container _container;
    private readonly List<string> _testDocumentIds = new();

    public WeightEndpointsTests(IntegrationTestFixture fixture)
    {
        _fixture = fixture;
        var cosmosClient = fixture.Factory.Services.GetRequiredService<CosmosClient>();
        _container = cosmosClient.GetContainer("biotrackr-test", "weight-test");
    }

    public async Task InitializeAsync()
    {
        // Seed test data
        await SeedTestDataAsync();
    }

    public async Task DisposeAsync()
    {
        // Clean up test data
        foreach (var id in _testDocumentIds)
        {
            try
            {
                await _container.DeleteItemAsync<WeightDocument>(id, new PartitionKey("Weight"));
            }
            catch
            {
                // Ignore cleanup errors
            }
        }
    }

    private async Task SeedTestDataAsync()
    {
        var testDocuments = new[]
        {
            new WeightDocument
            {
                Id = Guid.NewGuid().ToString(),
                DocumentType = "Weight",
                Date = "2024-01-01",
                Weight = new FitbitWeight
                {
                    Bmi = 25.5,
                    Date = "2024-01-01",
                    Fat = 18.5,
                    LogId = 1,
                    Source = "API",
                    Time = "08:00:00",
                    weight = 75.5
                }
            },
            new WeightDocument
            {
                Id = Guid.NewGuid().ToString(),
                DocumentType = "Weight",
                Date = "2024-01-15",
                Weight = new FitbitWeight
                {
                    Bmi = 24.8,
                    Date = "2024-01-15",
                    Fat = 17.2,
                    LogId = 2,
                    Source = "API",
                    Time = "08:30:00",
                    weight = 74.0
                }
            },
            new WeightDocument
            {
                Id = Guid.NewGuid().ToString(),
                DocumentType = "Weight",
                Date = "2024-01-31",
                Weight = new FitbitWeight
                {
                    Bmi = 24.2,
                    Date = "2024-01-31",
                    Fat = 16.5,
                    LogId = 3,
                    Source = "API",
                    Time = "09:00:00",
                    weight = 72.5
                }
            }
        };

        foreach (var doc in testDocuments)
        {
            await _container.CreateItemAsync(doc, new PartitionKey(doc.DocumentType));
            _testDocumentIds.Add(doc.Id);
        }
    }

    [Fact]
    public async Task GetAllWeights_Should_Return_Paginated_Results()
    {
        // Arrange
        var client = _fixture.Client;

        // Act
        var response = await client.GetAsync("/?pageNumber=1&pageSize=10");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<PaginationResponse<WeightDocument>>();
        result.Should().NotBeNull();
        result!.Items.Should().NotBeEmpty();
        result.PageNumber.Should().Be(1);
        result.PageSize.Should().Be(10);
    }

    [Fact]
    public async Task GetWeightByDate_Should_Return_Weight_Document_When_Exists()
    {
        // Arrange
        var client = _fixture.Client;
        var testDate = "2024-01-01";

        // Act
        var response = await client.GetAsync($"/{testDate}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<WeightDocument>();
        result.Should().NotBeNull();
        result!.Date.Should().Be(testDate);
        result.Weight.Should().NotBeNull();
    }

    [Fact]
    public async Task GetWeightByDate_Should_Return_NotFound_When_Date_Does_Not_Exist()
    {
        // Arrange
        var client = _fixture.Client;
        var nonExistentDate = "2099-12-31";

        // Act
        var response = await client.GetAsync($"/{nonExistentDate}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task GetWeightsByDateRange_Should_Return_Weights_In_Range()
    {
        // Arrange
        var client = _fixture.Client;
        var startDate = "2024-01-01";
        var endDate = "2024-01-31";

        // Act
        var response = await client.GetAsync($"/range/{startDate}/{endDate}?pageNumber=1&pageSize=10");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<PaginationResponse<WeightDocument>>();
        result.Should().NotBeNull();
        result!.Items.Should().NotBeEmpty();
        result.Items.Should().AllSatisfy(item =>
        {
            (string.Compare(item.Date, startDate, StringComparison.Ordinal) >= 0).Should().BeTrue();
            (string.Compare(item.Date, endDate, StringComparison.Ordinal) <= 0).Should().BeTrue();
        });
    }

    [Fact]
    public async Task GetWeightsByDateRange_Should_Return_BadRequest_When_StartDate_Invalid()
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
    public async Task GetWeightsByDateRange_Should_Return_BadRequest_When_StartDate_After_EndDate()
    {
        // Arrange
        var client = _fixture.Client;
        var startDate = "2024-12-31";
        var endDate = "2024-01-01";

        // Act
        var response = await client.GetAsync($"/range/{startDate}/{endDate}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Health_Endpoint_Should_Return_OK()
    {
        // Arrange
        var client = _fixture.Client;

        // Act
        var response = await client.GetAsync("/healthz/liveness");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Swagger_Should_Be_Accessible()
    {
        // Arrange
        var client = _fixture.Client;

        // Act
        var response = await client.GetAsync("/swagger/index.html");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact(Skip = "Flaky in CI: Cosmos DB Emulator timeout during cleanup operations")]
    public async Task GetAllWeights_Should_Handle_Empty_Results_Gracefully()
    {
        // Arrange
        var client = _fixture.Client;
        // Clean all test data first
        foreach (var id in _testDocumentIds.ToList())
        {
            await _container.DeleteItemAsync<WeightDocument>(id, new PartitionKey("Weight"));
        }
        _testDocumentIds.Clear();

        // Act
        var response = await client.GetAsync("/?pageNumber=1&pageSize=10");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<PaginationResponse<WeightDocument>>();
        result.Should().NotBeNull();
        result!.Items.Should().BeEmpty();
        result.TotalCount.Should().Be(0);
        
        // Reseed data for other tests
        await SeedTestDataAsync();
    }
}
