using Biotrackr.Sleep.Api.Models;
using Biotrackr.Sleep.Api.UnitTests.TestData;
using FluentAssertions;
using Newtonsoft.Json.Linq;

namespace Biotrackr.Sleep.Api.UnitTests.ModelTests;

public class SleepStoredDocumentShould
{
    [Fact]
    public void Deserialize_ShouldPopulateSleepAndLeaveVersionFieldsNull_WhenDocumentIsVersion1()
    {
        // Arrange
        var json = SleepDocumentSamples.V1Json;

        // Act
        var document = CosmosJson.Deserialize<SleepStoredDocument>(json);

        // Assert
        document.Id.Should().Be("11111111-1111-1111-1111-111111111111");
        document.Date.Should().Be("2025-01-10");
        document.DocumentType.Should().Be("Sleep");
        document.Provider.Should().BeNull();
        document.SchemaVersion.Should().BeNull();
        document.Google.Should().BeNull();
        document.Sleep!.Sleep.Should().ContainSingle().Which.LogId.Should().Be(123456789L);
        document.Sleep.Summary.TotalMinutesAsleep.Should().Be(420);
    }

    [Fact]
    public void Deserialize_ShouldPopulateEnvelopeAndRawGooglePayload_WhenDocumentIsVersion2()
    {
        // Arrange
        var json = SleepDocumentSamples.V2Json;

        // Act
        var document = CosmosJson.Deserialize<SleepStoredDocument>(json);

        // Assert
        document.Id.Should().Be("22222222-2222-2222-2222-222222222222");
        document.Date.Should().Be("2026-03-15");
        document.DocumentType.Should().Be("Sleep");
        document.Provider.Should().Be("Google");
        document.SchemaVersion.Should().Be(2);
        document.Sleep.Should().BeNull();
        document.Google.Should().NotBeNull();
        document.Google!.Properties().Select(p => p.Name).Should().BeEquivalentTo(
            "sleep", "dailyHeartRateVariability", "dailyOxygenSaturation", "dailyRespiratoryRate", "dailySleepTemperatureDerivations");
        ((JArray)document.Google["sleep"]!["dataPoints"]!).Should().HaveCount(2);
    }

    [Fact]
    public void Deserialize_ShouldKeepGoogleSourceKeysUnchanged_WhenSourcesAreEmpty()
    {
        // Arrange
        var json = SleepDocumentSamples.V2EmptyJson;

        // Act
        var document = CosmosJson.Deserialize<SleepStoredDocument>(json);

        // Assert
        document.Google!["dailyHeartRateVariability"].Should().BeOfType<JObject>().Which.Should().BeEmpty();
        ((JArray)document.Google["sleep"]!["dataPoints"]!).Should().BeEmpty();
    }
}
