using Biotrackr.Sleep.Api.Models;
using Biotrackr.Sleep.Api.Models.FitbitEntities;
using Biotrackr.Sleep.Api.UnitTests.TestData;
using FluentAssertions;
using System.Text.Json;
using System.Text.Json.Nodes;
using SleepModel = Biotrackr.Sleep.Api.Models.FitbitEntities.Sleep;

namespace Biotrackr.Sleep.Api.UnitTests.ModelTests;

public class SleepDocumentShould
{
    [Fact]
    public void Initialize_ShouldHaveDefaultValues_WhenCreatedWithDefaultConstructor()
    {
        // Arrange & Act
        var document = new SleepDocument();

        // Assert
        document.Id.Should().BeNull();
        document.Sleep.Should().BeNull();
        document.Date.Should().BeNull();
        document.DocumentType.Should().BeNull();
    }

    [Fact]
    public void AllowPropertyAssignment_ShouldRetainValues_WhenPropertiesAreSet()
    {
        // Arrange
        var sleepResponse = new SleepResponse
        {
            Sleep = new List<SleepModel> { new() { DateOfSleep = "2026-05-07" } },
            Summary = new Summary { TotalMinutesAsleep = 450 }
        };

        // Act
        var document = new SleepDocument
        {
            Id = "sleep-2026-05-07",
            Sleep = sleepResponse,
            Date = "2026-05-07",
            DocumentType = "sleep"
        };

        // Assert
        document.Id.Should().Be("sleep-2026-05-07");
        document.Sleep.Should().NotBeNull();
        document.Sleep.Sleep.Should().HaveCount(1);
        document.Date.Should().Be("2026-05-07");
        document.DocumentType.Should().Be("sleep");
    }

    [Fact]
    public void Serialize_ShouldMatchPreMigrationResponse_WhenBuiltFromVersion1StoredDocument()
    {
        // Arrange
        var stored = CosmosJson.Deserialize<SleepStoredDocument>(SleepDocumentSamples.V1Json);
        var document = new SleepDocument
        {
            Id = stored.Id,
            Sleep = stored.Sleep,
            Date = stored.Date,
            DocumentType = stored.DocumentType
        };

        // Act
        var actual = JsonSerializer.SerializeToNode(document, new JsonSerializerOptions(JsonSerializerDefaults.Web));

        // Assert
        JsonNode.DeepEquals(actual, JsonNode.Parse(SleepDocumentSamples.V1PreMigrationResponseJson)).Should().BeTrue(
            $"AGENT FIX: version 1 response must equal the pre-migration body field for field. Actual: {actual!.ToJsonString()}");
    }
}
