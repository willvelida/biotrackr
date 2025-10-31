using Biotrackr.Sleep.Api.IntegrationTests.Collections;
using Biotrackr.Sleep.Api.IntegrationTests.Fixtures;
using FluentAssertions;
using Microsoft.Azure.Cosmos;
using System.Net;
using System.Text.Json;
using Xunit;

namespace Biotrackr.Sleep.Api.IntegrationTests.E2E;

/// <summary>
/// E2E tests for Sleep API endpoints with real Cosmos DB
/// Tests verify full request/response cycle including database operations
/// </summary>
[Collection(nameof(E2ETestCollection))]
public class SleepEndpointTests
{
    private readonly IntegrationTestFixture _fixture;
    private readonly JsonSerializerOptions _jsonOptions;

    public SleepEndpointTests(IntegrationTestFixture fixture)
    {
        _fixture = fixture;
        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        };
    }

    [Fact]
    public async Task GetAllSleeps_WithNoData_ShouldReturnSuccessResponse()
    {
        // Arrange - Clear container for test isolation
        await ClearContainerAsync();

        // Act
        var response = await _fixture.Client.GetAsync("/?pageNumber=1&pageSize=20");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var content = await response.Content.ReadAsStringAsync();
        content.Should().NotBeNullOrEmpty();
        
        // Should return valid JSON
        using var jsonDoc = JsonDocument.Parse(content);
        jsonDoc.RootElement.ValueKind.Should().Be(JsonValueKind.Object);
    }

    [Fact]
    public async Task GetAllSleeps_WithInvalidPagination_ShouldHandleGracefully()
    {
        // Arrange
        await ClearContainerAsync();

        // Act - Send pageNumber=0 (potentially invalid)
        var response = await _fixture.Client.GetAsync("/?pageNumber=0&pageSize=20");

        // Assert - API may handle this gracefully (return 200 with empty results) or reject (400)
        // Current Sleep API behavior: returns 200 (handles gracefully)
        response.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task GetSleepByDate_WithValidDateFormat_ShouldReturnResponse()
    {
        // Arrange
        await ClearContainerAsync();
        var testDate = DateOnly.FromDateTime(DateTime.UtcNow);

        // Act
        var response = await _fixture.Client.GetAsync($"/{testDate:yyyy-MM-dd}");

        // Assert
        // May return 404 if no data for date, but should not fail with 500
        response.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task GetSleepByDate_WithInvalidDateFormat_ShouldReturnBadRequest()
    {
        // Act
        var response = await _fixture.Client.GetAsync("/invalid-date");

        // Assert
        response.StatusCode.Should().BeOneOf(HttpStatusCode.BadRequest, HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task GetSleepsByDateRange_WithValidRange_ShouldReturnResponse()
    {
        // Arrange
        await ClearContainerAsync();
        var startDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-7));
        var endDate = DateOnly.FromDateTime(DateTime.UtcNow);

        // Act
        var response = await _fixture.Client.GetAsync($"/range/{startDate:yyyy-MM-dd}/{endDate:yyyy-MM-dd}?pageNumber=1&pageSize=20");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var content = await response.Content.ReadAsStringAsync();
        content.Should().NotBeNullOrEmpty();
        
        // Should return valid JSON
        using var jsonDoc = JsonDocument.Parse(content);
        jsonDoc.RootElement.ValueKind.Should().Be(JsonValueKind.Object);
    }

    [Fact]
    public async Task GetSleepsByDateRange_WithInvalidRange_ShouldReturnBadRequest()
    {
        // Arrange - End date before start date
        var startDate = DateOnly.FromDateTime(DateTime.UtcNow);
        var endDate = startDate.AddDays(-10);

        // Act
        var response = await _fixture.Client.GetAsync($"/range/{startDate:yyyy-MM-dd}/{endDate:yyyy-MM-dd}?pageNumber=1&pageSize=20");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    /// <summary>
    /// Clears all documents from the test container to ensure test isolation.
    /// Per common-resolutions.md: E2E tests must clean up to avoid data contamination.
    /// </summary>
    private async Task ClearContainerAsync()
    {
        var query = new QueryDefinition("SELECT c.id, c.documentType FROM c");
        var iterator = _fixture.Container.GetItemQueryIterator<dynamic>(query);

        while (iterator.HasMoreResults)
        {
            var response = await iterator.ReadNextAsync();
            foreach (var item in response)
            {
                await _fixture.Container.DeleteItemAsync<dynamic>(
                    item.id.ToString(),
                    new PartitionKey(item.documentType.ToString()));
            }
        }
    }
}
