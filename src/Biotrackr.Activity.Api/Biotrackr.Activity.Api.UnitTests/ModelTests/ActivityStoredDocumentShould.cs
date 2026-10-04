using Biotrackr.Activity.Api.Models;
using Biotrackr.Activity.Api.UnitTests.TestData;
using FluentAssertions;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Biotrackr.Activity.Api.UnitTests.ModelTests;

public class ActivityStoredDocumentShould
{
    [Fact]
    public void Deserialize_ShouldReadVersion1Fields_WhenDocumentHasNoSchemaVersion()
    {
        // Arrange
        var json = ActivityDocumentSamples.Version1Document;

        // Act
        var document = ActivityDocumentSamples.DeserializeLikeCosmos<ActivityStoredDocument>(json);

        // Assert
        document.Should().BeEquivalentTo(new
        {
            Id = ActivityDocumentSamples.Version1Id,
            Date = "2024-03-05",
            DocumentType = "Activity",
            Provider = (string?)null,
            SchemaVersion = (int?)null,
            Google = (JObject?)null
        }, "AGENT FIX: a version 1 document must deserialise with null Provider, SchemaVersion and Google");
        document.Activity!.summary.steps.Should().Be(10432);
        document.Activity.activities.Should().ContainSingle().Which.logId.Should().Be(61234567890);
    }

    [Fact]
    public void Deserialize_ShouldReadGooglePayload_WhenDocumentIsVersion2()
    {
        // Arrange
        var json = ActivityDocumentSamples.Version2Document;

        // Act
        var document = ActivityDocumentSamples.DeserializeLikeCosmos<ActivityStoredDocument>(json);

        // Assert
        document.Provider.Should().Be("Google");
        document.SchemaVersion.Should().Be(2);
        document.Activity.Should().BeNull();
        document.Google.Should().NotBeNull();
        document.Google!.Properties().Select(p => p.Name).Should().Contain(
            ["steps", "totalCalories", "activeEnergyBurned", "distance", "floors", "altitude", "activeMinutes",
             "sedentaryPeriod", "activeZoneMinutes", "timeInHeartRateZone", "caloriesInHeartRateZone",
             "dailyRestingHeartRate", "dailyHeartRateZones", "exercise"],
            "AGENT FIX: Google source keys must keep their C1 names; check the Cosmos naming policy does not rewrite JObject keys");
        ((string?)document.Google.SelectToken("steps.rollupDataPoints[0].steps.countSum")).Should().Be("8432");
    }

    [Fact]
    public void Serialize_ShouldOmitVersion2Fields_WhenTheyAreNull()
    {
        // Arrange
        var document = new ActivityStoredDocument { Id = "id-1", Date = "2024-03-05", DocumentType = "Activity" };

        // Act
        var json = JObject.Parse(JsonConvert.SerializeObject(document, ActivityDocumentSamples.CosmosSettings));

        // Assert
        json.Properties().Select(p => p.Name).Should().BeEquivalentTo(["id", "date", "documentType"]);
    }
}
