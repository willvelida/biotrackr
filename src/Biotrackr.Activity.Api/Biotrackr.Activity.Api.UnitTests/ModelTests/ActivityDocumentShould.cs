using System.Text.Json;
using System.Text.Json.Nodes;
using Biotrackr.Activity.Api.Models;
using Biotrackr.Activity.Api.Models.FitbitEntities;
using Biotrackr.Activity.Api.UnitTests.TestData;
using FluentAssertions;

namespace Biotrackr.Activity.Api.UnitTests.ModelTests;

public class ActivityDocumentShould
{
    // Minimal APIs serialise responses with the web defaults (camelCase, case-insensitive).
    private static readonly JsonSerializerOptions WebOptions = new(JsonSerializerDefaults.Web);

    [Fact]
    public void Serialize_ShouldKeepVersion1ActivityFieldsUnchanged_WhenVersion1DataIsReturned()
    {
        // Arrange
        var activity = ActivityDocumentSamples.DeserializeLikeCosmos<ActivityResponse>(ActivityDocumentSamples.Version1ActivityJson);
        var document = new ActivityDocument
        {
            Id = "id-1",
            Date = "2024-03-05",
            DocumentType = "Activity",
            Activity = activity,
            Provider = "Fitbit",
            SchemaVersion = 1
        };

        // Act
        var json = JsonNode.Parse(JsonSerializer.Serialize(document, WebOptions))!;

        // Assert
        JsonNode.DeepEquals(json["activity"], JsonNode.Parse(ActivityDocumentSamples.Version1ActivityJson)).Should().BeTrue(
            $"AGENT FIX: version 1 'activity' JSON must match the pre-migration shape exactly. Got: {json["activity"]!.ToJsonString()}");
    }

    [Fact]
    public void Serialize_ShouldAddOnlyProviderSchemaVersionAndNullEnrichment_WhenVersion1DataIsReturned()
    {
        // Arrange
        var document = new ActivityDocument
        {
            Id = "id-1",
            Date = "2024-03-05",
            DocumentType = "Activity",
            Activity = new ActivityResponse(),
            Provider = "Fitbit",
            SchemaVersion = 1
        };

        // Act
        var json = JsonNode.Parse(JsonSerializer.Serialize(document, WebOptions))!.AsObject();

        // Assert
        json.Select(p => p.Key).Should().BeEquivalentTo(
            ["id", "activity", "date", "documentType", "provider", "schemaVersion", "activityEnrichment"],
            "AGENT FIX: the response may only add provider, schemaVersion and activityEnrichment (plan contract C2)");
        json["provider"]!.GetValue<string>().Should().Be("Fitbit");
        json["schemaVersion"]!.GetValue<int>().Should().Be(1);
        json["activityEnrichment"].Should().BeNull();
    }

    [Fact]
    public void Serialize_ShouldUseC2FieldNames_WhenEnrichmentIsPresent()
    {
        // Arrange
        var document = new ActivityDocument
        {
            ActivityEnrichment = new ActivityEnrichment
            {
                ActiveZoneMinutes = new ActiveZoneMinutes { FatBurn = 1, Cardio = 2, Peak = 3, Total = 6 },
                Workouts = [new Workout { Name = "Walk", DistanceKm = 1.5 }]
            }
        };

        // Act
        var json = JsonNode.Parse(JsonSerializer.Serialize(document, WebOptions))!;

        // Assert
        json["activityEnrichment"]!["activeZoneMinutes"]!.AsObject().Select(p => p.Key)
            .Should().Equal("fatBurn", "cardio", "peak", "total");
        json["activityEnrichment"]!["workouts"]![0]!.AsObject().Select(p => p.Key).Should().Equal(
            "name", "exerciseType", "startTime", "endTime", "durationMinutes", "calories", "steps",
            "distanceKm", "averageHeartRate", "activeZoneMinutes");
    }
}
