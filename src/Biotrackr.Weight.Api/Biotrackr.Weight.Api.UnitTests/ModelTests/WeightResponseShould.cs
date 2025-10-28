using AutoFixture;
using Biotrackr.Weight.Api.Models.FitbitEntities;
using FluentAssertions;
using System.Text.Json;
using Xunit;
using FitbitWeight = Biotrackr.Weight.Api.Models.FitbitEntities.Weight;

namespace Biotrackr.Weight.Api.UnitTests.ModelTests;

public class WeightResponseShould
{
    private readonly Fixture _fixture;

    public WeightResponseShould()
    {
        _fixture = new Fixture();
    }

    [Fact]
    public void Create_WeightResponse_With_Default_Values()
    {
        // Act
        var response = new WeightResponse();

        // Assert
        response.Should().NotBeNull();
        response.Weight.Should().BeNull();
    }

    [Fact]
    public void Set_Weight_Property()
    {
        // Arrange
        var response = new WeightResponse();
        var weights = new List<FitbitWeight>
        {
            new FitbitWeight { weight = 70.5, Date = "2025-10-28" },
            new FitbitWeight { weight = 71.0, Date = "2025-10-27" }
        };

        // Act
        response.Weight = weights;

        // Assert
        response.Weight.Should().NotBeNull();
        response.Weight.Should().HaveCount(2);
        response.Weight.Should().Contain(w => w.weight == 70.5);
    }

    [Fact]
    public void Create_WeightResponse_With_Object_Initializer()
    {
        // Arrange & Act
        var response = new WeightResponse
        {
            Weight = new List<FitbitWeight>
            {
                new FitbitWeight { weight = 75.0, Date = "2025-10-28" }
            }
        };

        // Assert
        response.Weight.Should().NotBeNull();
        response.Weight.Should().HaveCount(1);
        response.Weight.First().weight.Should().Be(75.0);
    }

    [Fact]
    public void Allow_Empty_Weight_List()
    {
        // Arrange & Act
        var response = new WeightResponse
        {
            Weight = new List<FitbitWeight>()
        };

        // Assert
        response.Weight.Should().NotBeNull();
        response.Weight.Should().BeEmpty();
    }

    [Fact]
    public void Serialize_To_Json_With_Correct_Property_Name()
    {
        // Arrange
        var response = new WeightResponse
        {
            Weight = new List<FitbitWeight>
            {
                new FitbitWeight 
                { 
                    weight = 70.5, 
                    Date = "2025-10-28",
                    Bmi = 22.5,
                    Fat = 15.0,
                    Time = "10:30:00",
                    Source = "API"
                }
            }
        };

        // Act
        var json = JsonSerializer.Serialize(response);

        // Assert
        json.Should().Contain("\"weight\":");
        json.Should().NotContain("\"Weight\":");
    }

    [Fact]
    public void Deserialize_From_Json_With_Correct_Property_Name()
    {
        // Arrange
        var json = @"{
            ""weight"": [
                {
                    ""weight"": 70.5,
                    ""date"": ""2025-10-28"",
                    ""bmi"": 22.5,
                    ""fat"": 15.0,
                    ""time"": ""10:30:00"",
                    ""source"": ""API"",
                    ""logId"": null
                }
            ]
        }";

        // Act
        var response = JsonSerializer.Deserialize<WeightResponse>(json);

        // Assert
        response.Should().NotBeNull();
        response!.Weight.Should().NotBeNull();
        response.Weight.Should().HaveCount(1);
        response.Weight.First().weight.Should().Be(70.5);
    }

    [Fact]
    public void Handle_Multiple_Weight_Entries()
    {
        // Arrange
        var weights = Enumerable.Range(1, 10)
            .Select(i => new FitbitWeight 
            { 
                weight = 70.0 + i,
                Date = $"2025-10-{i:D2}"
            })
            .ToList();

        // Act
        var response = new WeightResponse { Weight = weights };

        // Assert
        response.Weight.Should().HaveCount(10);
        response.Weight.Should().BeInAscendingOrder(w => w.weight);
    }

    [Fact]
    public void Allow_Modification_After_Creation()
    {
        // Arrange
        var response = new WeightResponse
        {
            Weight = new List<FitbitWeight>
            {
                new FitbitWeight { weight = 70.0, Date = "2025-10-28" }
            }
        };

        // Act
        response.Weight.Add(new FitbitWeight { weight = 71.0, Date = "2025-10-27" });

        // Assert
        response.Weight.Should().HaveCount(2);
    }
}
