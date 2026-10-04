using System.Text.Json;
using System.Text.Json.Nodes;
using Biotrackr.Sleep.Api.EndpointHandlers;
using Biotrackr.Sleep.Api.Models;
using Biotrackr.Sleep.Api.Repositories.Interfaces;
using Biotrackr.Sleep.Api.Services;
using Biotrackr.Sleep.Api.UnitTests.TestData;
using FluentAssertions;
using Microsoft.AspNetCore.Http.HttpResults;
using Moq;

namespace Biotrackr.Sleep.Api.UnitTests.EndpointHandlerTests;

/// <summary>
/// Handler behaviour for Google-era (version 2) documents and pages that mix both versions.
/// </summary>
public class SleepHandlersVersion2Should
{
    private readonly Mock<ICosmosRepository> _cosmosRepositoryMock = new();
    private readonly SleepDocumentTranslator _translator = new();

    [Fact]
    public async Task GetSleepByDate_ShouldReturnGoogleProvider_WhenStoredDocumentIsVersion2()
    {
        // Arrange
        var stored = CosmosJson.Deserialize<SleepStoredDocument>(SleepDocumentSamples.V2Json);
        _cosmosRepositoryMock.Setup(x => x.GetSleepSummaryByDate(stored.Date)).ReturnsAsync(stored);

        // Act
        var result = await SleepHandlers.GetSleepByDate(_cosmosRepositoryMock.Object, _translator, stored.Date);

        // Assert
        result.Result.Should().BeOfType<Ok<SleepDocument>>().Which.Value!.Provider.Should().Be("Google");
    }

    [Fact]
    public async Task GetSleepByDate_ShouldNotExposeRawGooglePayload_WhenStoredDocumentIsVersion2()
    {
        // Arrange
        var stored = CosmosJson.Deserialize<SleepStoredDocument>(SleepDocumentSamples.V2Json);
        _cosmosRepositoryMock.Setup(x => x.GetSleepSummaryByDate(stored.Date)).ReturnsAsync(stored);

        // Act
        var result = await SleepHandlers.GetSleepByDate(_cosmosRepositoryMock.Object, _translator, stored.Date);

        // Assert
        var body = JsonSerializer.SerializeToNode(((Ok<SleepDocument>)result.Result).Value, new JsonSerializerOptions(JsonSerializerDefaults.Web))!.AsObject();
        body.ContainsKey("google").Should().BeFalse("AGENT FIX: handlers must return the translated SleepDocument, never the stored model (D-03).");
    }

    [Fact]
    public async Task GetSleepsByDateRange_ShouldReturnBothVersionsInOnePage_WhenRangeSpansCutover()
    {
        // Arrange
        var page = new PaginationResponse<SleepStoredDocument>
        {
            Items =
            [
                CosmosJson.Deserialize<SleepStoredDocument>(SleepDocumentSamples.V2Json),
                CosmosJson.Deserialize<SleepStoredDocument>(SleepDocumentSamples.V1Json)
            ],
            TotalCount = 2,
            PageNumber = 1,
            PageSize = 20
        };
        _cosmosRepositoryMock.Setup(x => x.GetSleepDocumentsByDateRange("2025-01-01", "2026-03-31", It.IsAny<PaginationRequest>()))
            .ReturnsAsync(page);

        // Act
        var result = await SleepHandlers.GetSleepsByDateRange(_cosmosRepositoryMock.Object, _translator, "2025-01-01", "2026-03-31");

        // Assert
        var value = result.Result.Should().BeOfType<Ok<PaginationResponse<SleepDocument>>>().Subject.Value!;
        value.Items.Select(item => (item.Provider, item.SchemaVersion)).Should().Equal(("Google", 2), ("Fitbit", 1));
    }

    [Fact]
    public async Task GetAllSleeps_ShouldPreservePaginationEnvelope_WhenItemsAreTranslated()
    {
        // Arrange
        var page = new PaginationResponse<SleepStoredDocument>
        {
            Items = [CosmosJson.Deserialize<SleepStoredDocument>(SleepDocumentSamples.V2Json)],
            TotalCount = 41,
            PageNumber = 3,
            PageSize = 20
        };
        _cosmosRepositoryMock.Setup(x => x.GetAllSleepDocuments(It.IsAny<PaginationRequest>())).ReturnsAsync(page);

        // Act
        var result = await SleepHandlers.GetAllSleeps(_cosmosRepositoryMock.Object, _translator, 3, 20);

        // Assert
        var body = JsonSerializer.SerializeToNode(result.Value, new JsonSerializerOptions(JsonSerializerDefaults.Web))!.AsObject();
        body.Select(property => property.Key).Should().Equal(
            ["items", "totalCount", "pageNumber", "pageSize", "totalPages", "hasPreviousPage", "hasNextPage"],
            "AGENT FIX: AC 19 requires the PaginationResponse envelope to be unchanged.");
    }
}
