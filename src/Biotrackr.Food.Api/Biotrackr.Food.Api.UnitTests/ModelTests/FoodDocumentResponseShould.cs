using System.Text.Json;
using System.Text.Json.Nodes;
using Biotrackr.Food.Api.Models;
using Biotrackr.Food.Api.Models.FitbitEntities;
using Biotrackr.Food.Api.UnitTests.Fixtures;
using FluentAssertions;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace Biotrackr.Food.Api.UnitTests.ModelTests;

public class FoodDocumentResponseShould
{
    // ASP.NET Core minimal APIs serialize responses with the web defaults.
    private static readonly JsonSerializerOptions WebOptions = new(JsonSerializerDefaults.Web);

    private static readonly JsonSerializerSettings CosmosSettings = new()
    {
        ContractResolver = new CamelCasePropertyNamesContractResolver()
    };

    [Fact]
    public void Serialize_ShouldEmitVersion1FoodUnchanged_WhenDocumentIsVersion1()
    {
        // Arrange
        var expectedFood = JsonNode.Parse(FoodDocumentSamples.Version1Json)!["food"];
        var document = JsonConvert.DeserializeObject<FoodDocument>(FoodDocumentSamples.Version1Json, CosmosSettings)!;

        // Act
        var output = JsonNode.Parse(System.Text.Json.JsonSerializer.Serialize(document, WebOptions))!;

        // Assert
        JsonNode.DeepEquals(output["food"], expectedFood).Should().BeTrue(
            $"every pre-migration food field must keep its name and value (AC 15, 17). Expected {expectedFood!.ToJsonString()} but got {output["food"]!.ToJsonString()}");
    }

    [Fact]
    public void Serialize_ShouldAddOnlyProviderAndSchemaVersion_WhenDocumentIsVersion1()
    {
        // Arrange
        var document = JsonConvert.DeserializeObject<FoodDocument>(FoodDocumentSamples.Version1Json, CosmosSettings)!;

        // Act
        var output = JsonNode.Parse(System.Text.Json.JsonSerializer.Serialize(document, WebOptions))!.AsObject();

        // Assert
        output.Select(p => p.Key).Should().BeEquivalentTo("id", "food", "date", "documentType", "provider", "schemaVersion");
        output["provider"]!.GetValue<string>().Should().Be("Fitbit");
        output["schemaVersion"]!.GetValue<int>().Should().Be(1);
    }

    [Fact]
    public void Serialize_ShouldWriteWholeNumberAmountAndCaloriesWithoutDecimals_WhenDocumentIsVersion1()
    {
        // Arrange
        var document = JsonConvert.DeserializeObject<FoodDocument>(FoodDocumentSamples.Version1Json, CosmosSettings)!;

        // Act
        var json = System.Text.Json.JsonSerializer.Serialize(document.Food.Foods[0].LoggedFood, WebOptions);

        // Assert
        json.Should().Contain("\"amount\":1,").And.Contain("\"calories\":105,",
            "amount and calories became double; whole numbers must still serialize as before (no .0)");
    }

    [Fact]
    public void Serialize_ShouldEmitPreMigrationDefaults_WhenVersion1EntryOmitsNullableFields()
    {
        // Arrange
        const string sparseV1 = """
            { "id": "v1-sparse", "date": "2025-12-02", "documentType": "Food",
              "food": { "foods": [ { "logDate": "2025-12-02", "loggedFood": { "name": "Synthetic Apple", "unit": { "name": "serving" } } } ] } }
            """;
        var document = JsonConvert.DeserializeObject<FoodDocument>(sparseV1, CosmosSettings)!;

        // Act
        var entry = JsonNode.Parse(System.Text.Json.JsonSerializer.Serialize(document, WebOptions))!["food"]!["foods"]![0]!;

        // Assert
        new object?[]
        {
            entry["logId"]!.GetValue<long>(), entry["isFavorite"]!.GetValue<bool>(),
            entry["loggedFood"]!["foodId"]!.GetValue<int>(), entry["loggedFood"]!["unit"]!["id"]!.GetValue<int>(),
            entry["loggedFood"]!["units"]!.AsArray().Count, document.Food.Goals!.Calories
        }.Should().Equal(new object?[] { 0L, false, 0, 0, 0, 0 },
            "missing v1 fields must serialize as the pre-migration defaults (0, false, [], goals {calories:0}), not null");
    }

    [Fact]
    public void Serialize_ShouldEmitNull_WhenContractNullableFieldsAreNull()
    {
        // Arrange
        var document = new FoodDocument
        {
            Provider = "Google",
            SchemaVersion = 2,
            Food = new FoodResponse
            {
                Goals = null,
                Foods =
                [
                    new Models.FitbitEntities.Food
                    {
                        LogId = null,
                        IsFavorite = null,
                        LoggedFood = new LoggedFood { FoodId = null, Units = null, Unit = new Unit { Id = null } }
                    }
                ]
            }
        };

        // Act
        var output = JsonNode.Parse(System.Text.Json.JsonSerializer.Serialize(document, WebOptions))!;

        // Assert
        var food = output["food"]!;
        var entry = food["foods"]![0]!;
        new[]
        {
            food["goals"], entry["logId"], entry["isFavorite"], entry["loggedFood"]!["foodId"],
            entry["loggedFood"]!["units"], entry["loggedFood"]!["unit"]!["id"]
        }.Should().AllSatisfy(n => n.Should().BeNull("contract C2 lists these fields as null for version 2"));
    }
}
