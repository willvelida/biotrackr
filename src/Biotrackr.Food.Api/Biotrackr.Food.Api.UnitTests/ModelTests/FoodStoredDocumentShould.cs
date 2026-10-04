using Biotrackr.Food.Api.Models;
using Biotrackr.Food.Api.UnitTests.Fixtures;
using FluentAssertions;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace Biotrackr.Food.Api.UnitTests.ModelTests;

public class FoodStoredDocumentShould
{
    // Mirrors CosmosPropertyNamingPolicy.CamelCase on the SDK's Newtonsoft serializer.
    private static readonly JsonSerializerSettings CosmosSettings = new()
    {
        ContractResolver = new CamelCasePropertyNamesContractResolver()
    };

    private static FoodStoredDocument Deserialize(string json) =>
        JsonConvert.DeserializeObject<FoodStoredDocument>(json, CosmosSettings)!;

    [Fact]
    public void Deserialize_ShouldPopulateFoodAndLeaveVersionNull_WhenDocumentIsVersion1()
    {
        // Arrange
        var json = FoodDocumentSamples.Version1Json;

        // Act
        var document = Deserialize(json);

        // Assert
        document.SchemaVersion.Should().BeNull("a version 1 document has no schemaVersion field");
        document.Provider.Should().BeNull();
        document.Google.Should().BeNull();
        document.Food!.Foods.Should().ContainSingle().Which.LoggedFood.Name.Should().Be("Synthetic Banana");
    }

    [Fact]
    public void Deserialize_ShouldKeepVersion1ValuesUnchanged_WhenDocumentIsVersion1()
    {
        // Arrange
        var json = FoodDocumentSamples.Version1Json;

        // Act
        var document = Deserialize(json);

        // Assert
        var food = document.Food!.Foods[0];
        food.LogId.Should().Be(987654321);
        food.IsFavorite.Should().BeTrue();
        food.LoggedFood.FoodId.Should().Be(4242);
        food.LoggedFood.Unit.Id.Should().Be(304);
        food.LoggedFood.Units.Should().Equal(304, 226);
        document.Food.Goals!.Calories.Should().Be(2200);
        document.Food.Summary.Water.Should().Be(750);
    }

    [Fact]
    public void Deserialize_ShouldPopulateEnvelopeAndRawGoogle_WhenDocumentIsVersion2()
    {
        // Arrange
        var json = FoodDocumentSamples.Version2Json;

        // Act
        var document = Deserialize(json);

        // Assert
        document.Id.Should().Be("v2-doc-id");
        document.Date.Should().Be("2026-01-15");
        document.Provider.Should().Be("Google");
        document.SchemaVersion.Should().Be(2);
        document.Food.Should().BeNull();
        document.Google.Should().NotBeNull();
        document.Google!.Properties().Select(p => p.Name).Should().BeEquivalentTo("nutritionLog", "hydrationLog", "foods");
    }

    [Fact]
    public void Deserialize_ShouldKeepGoogleKeysVerbatim_WhenDocumentIsVersion2()
    {
        // Arrange
        var json = FoodDocumentSamples.Version2Json;

        // Act
        var document = Deserialize(json);

        // Assert
        document.Google!.SelectToken("nutritionLog.dataPoints[0].nutritionLog.foodDisplayName")!.ToString()
            .Should().Be("Synthetic Oats");
        document.Google.SelectToken("foods.items[0].food.brand")!.ToString().Should().Be("Synthetic Mills");
    }
}
