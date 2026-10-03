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
        document.Provider.Should().BeNull();
        document.SchemaVersion.Should().Be(0);
        document.SleepEnrichment.Should().BeNull();
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
    public void Serialize_ShouldMatchPreMigrationResponseExceptAdditions_WhenBuiltFromVersion1StoredDocument()
    {
        // Arrange
        var stored = CosmosJson.Deserialize<SleepStoredDocument>(SleepDocumentSamples.V1Json);
        var document = new SleepDocument
        {
            Id = stored.Id,
            Sleep = stored.Sleep!,
            Date = stored.Date,
            DocumentType = stored.DocumentType,
            Provider = "Fitbit",
            SchemaVersion = 1,
            SleepEnrichment = null
        };

        // Act
        var actual = JsonSerializer.SerializeToNode(document, new JsonSerializerOptions(JsonSerializerDefaults.Web))!.AsObject();
        var withoutAdditions = actual.DeepClone().AsObject();
        withoutAdditions.Remove("provider");
        withoutAdditions.Remove("schemaVersion");
        withoutAdditions.Remove("sleepEnrichment");

        // Assert
        JsonNode.DeepEquals(withoutAdditions, JsonNode.Parse(SleepDocumentSamples.V1PreMigrationResponseJson)).Should().BeTrue(
            $"AGENT FIX: version 1 fields must equal the pre-migration body exactly; only provider, schemaVersion and sleepEnrichment may be added. Actual: {actual.ToJsonString()}");
    }

    [Fact]
    public void Serialize_ShouldEmitProviderSchemaVersionAndNullEnrichment_WhenDocumentIsVersion1()
    {
        // Arrange
        var document = new SleepDocument { Provider = "Fitbit", SchemaVersion = 1, SleepEnrichment = null };

        // Act
        var actual = JsonSerializer.SerializeToNode(document, new JsonSerializerOptions(JsonSerializerDefaults.Web))!.AsObject();

        // Assert
        actual["provider"]!.GetValue<string>().Should().Be("Fitbit");
        actual["schemaVersion"]!.GetValue<int>().Should().Be(1);
        actual.ContainsKey("sleepEnrichment").Should().BeTrue("AGENT FIX: sleepEnrichment must be emitted as null, not omitted, for version 1 (C2).");
        actual["sleepEnrichment"].Should().BeNull();
    }

    [Fact]
    public void Serialize_ShouldUseC2PropertyNames_WhenEnrichmentIsPopulated()
    {
        // Arrange
        var document = new SleepDocument
        {
            SleepEnrichment = new SleepEnrichment
            {
                HeartRateVariability = new HeartRateVariabilityEnrichment { AverageMs = 40.5, DeepSleepRmssdMs = 50.0 },
                OxygenSaturation = new OxygenSaturationEnrichment { AveragePercent = 96.0, LowerBoundPercent = 94.0, UpperBoundPercent = 98.0 },
                RespiratoryRate = new RespiratoryRateEnrichment { BreathsPerMinute = 14.0 },
                SkinTemperature = new SkinTemperatureEnrichment { NightlyCelsius = 33.0, BaselineCelsius = 33.5, DeviationCelsius = 0.2 },
                ShortAwakenings = [new ShortAwakening { StartTime = new DateTime(2026, 3, 15, 0, 10, 0), EndTime = new DateTime(2026, 3, 15, 0, 11, 30), Seconds = 90 }]
            }
        };
        var expected = JsonNode.Parse("""
            {
              "heartRateVariability": { "averageMs": 40.5, "deepSleepRmssdMs": 50.0 },
              "oxygenSaturation": { "averagePercent": 96.0, "lowerBoundPercent": 94.0, "upperBoundPercent": 98.0 },
              "respiratoryRate": { "breathsPerMinute": 14.0 },
              "skinTemperature": { "nightlyCelsius": 33.0, "baselineCelsius": 33.5, "deviationCelsius": 0.2 },
              "shortAwakenings": [ { "startTime": "2026-03-15T00:10:00", "endTime": "2026-03-15T00:11:30", "seconds": 90 } ]
            }
            """);

        // Act
        var actual = JsonSerializer.SerializeToNode(document, new JsonSerializerOptions(JsonSerializerDefaults.Web))!["sleepEnrichment"];

        // Assert
        JsonNode.DeepEquals(actual, expected).Should().BeTrue(
            $"AGENT FIX: sleepEnrichment must match the C2 shape in the migration plan. Actual: {actual!.ToJsonString()}");
    }
}
