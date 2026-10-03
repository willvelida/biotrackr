using Biotrackr.Sleep.Api.IntegrationTests.Collections;
using Biotrackr.Sleep.Api.IntegrationTests.Fixtures;
using Biotrackr.Sleep.Api.IntegrationTests.Helpers;
using FluentAssertions;
using Microsoft.Azure.Cosmos;
using System.Net;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using Xunit;

namespace Biotrackr.Sleep.Api.IntegrationTests.E2E;

/// <summary>
/// E2E tests for Sleep API endpoints with real Cosmos DB
/// Tests verify full request/response cycle including database operations
/// </summary>
[Collection(E2ETestCollection.CollectionName)]
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

    [Fact]
    public async Task GetSleepByDate_WithVersion1Document_ShouldReturnFitbitFieldsUnchangedPlusAdditions()
    {
        // Arrange
        await ClearContainerAsync();
        await SeedRawAsync(SleepDocumentSamples.V1Json);

        // Act
        var response = await _fixture.Client.GetAsync($"/{SleepDocumentSamples.V1Date}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = JsonNode.Parse(await response.Content.ReadAsStringAsync())!;
        body["provider"]!.GetValue<string>().Should().Be("Fitbit");
        body["schemaVersion"]!.GetValue<int>().Should().Be(1);
        body.AsObject().ContainsKey("sleepEnrichment").Should().BeTrue("AGENT FIX: sleepEnrichment must be emitted as null for version 1 (AC 15, C2).");
        body["sleepEnrichment"].Should().BeNull();
        var session = body["sleep"]!["sleep"]![0]!;
        session["logId"]!.GetValue<long>().Should().Be(123456789L);
        session["efficiency"]!.GetValue<int>().Should().Be(93);
        session["startTime"]!.GetValue<string>().Should().Be("2025-01-09T23:00:00");
    }

    [Fact]
    public async Task GetSleepByDate_WithVersion2Document_ShouldReturnTranslatedGoogleData()
    {
        // Arrange
        await ClearContainerAsync();
        await SeedRawAsync(SleepDocumentSamples.V2Json);

        // Act
        var response = await _fixture.Client.GetAsync($"/{SleepDocumentSamples.V2Date}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = JsonNode.Parse(await response.Content.ReadAsStringAsync())!;
        body["provider"]!.GetValue<string>().Should().Be("Google");
        body["schemaVersion"]!.GetValue<int>().Should().Be(2);
        body.AsObject().ContainsKey("google").Should().BeFalse("AGENT FIX: the raw Google payload must never be returned (D-03).");
        var session = body["sleep"]!["sleep"]![0]!;
        session["efficiency"]!.GetValue<int>().Should().Be(90, "AC 21: round(432 / 480 * 100)");
        session["logId"].Should().BeNull();
        session["startTime"]!.GetValue<string>().Should().Be("2026-03-14T23:30:00");
        session["levels"]!["data"]![0]!["level"]!.GetValue<string>().Should().Be("wake");
        body["sleepEnrichment"]!["respiratoryRate"]!["breathsPerMinute"]!.GetValue<double>().Should().Be(14.2);
        body["sleepEnrichment"]!["oxygenSaturation"].Should().BeNull();
        body["sleepEnrichment"]!["shortAwakenings"]!.AsArray().Should().ContainSingle();
    }

    [Fact]
    public async Task GetSleepsByDateRange_WithVersion1AndVersion2Documents_ShouldReturnBothInOnePage()
    {
        // Arrange
        await ClearContainerAsync();
        await SeedRawAsync(SleepDocumentSamples.V1Json);
        await SeedRawAsync(SleepDocumentSamples.V2Json);

        // Act
        var response = await _fixture.Client.GetAsync($"/range/{SleepDocumentSamples.V1Date}/{SleepDocumentSamples.V2Date}?pageNumber=1&pageSize=20");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = JsonNode.Parse(await response.Content.ReadAsStringAsync())!;
        body["totalCount"]!.GetValue<int>().Should().Be(2);
        body["items"]!.AsArray().Select(item => item!["provider"]!.GetValue<string>())
            .Should().BeEquivalentTo(["Fitbit", "Google"], "AGENT FIX: AC 18 requires v1 and v2 documents in one page without error.");
    }

    private async Task SeedRawAsync(string json)
    {
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(json));
        using var response = await _fixture.Container.CreateItemStreamAsync(stream, new PartitionKey("Sleep"));
        response.IsSuccessStatusCode.Should().BeTrue($"seeding the emulator failed with {response.StatusCode}: {response.ErrorMessage}");
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
