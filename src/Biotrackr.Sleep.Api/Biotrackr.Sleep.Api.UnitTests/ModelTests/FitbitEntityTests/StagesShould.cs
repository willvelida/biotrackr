using System.Text.Json;
using Biotrackr.Sleep.Api.Models.FitbitEntities;
using FluentAssertions;

namespace Biotrackr.Sleep.Api.UnitTests.ModelTests.FitbitEntityTests;

public class StagesShould
{
    [Fact]
    public void Initialize_ShouldHaveDefaultValues_WhenCreatedWithDefaultConstructor()
    {
        // Arrange & Act
        var stages = new Stages();

        // Assert
        stages.Deep.Should().Be(0);
        stages.Light.Should().Be(0);
        stages.Rem.Should().Be(0);
        stages.Wake.Should().Be(0);
    }

    [Fact]
    public void AllowPropertyAssignment_ShouldRetainValues_WhenPropertiesAreSet()
    {
        // Arrange & Act
        var stages = new Stages
        {
            Deep = 90,
            Light = 200,
            Rem = 120,
            Wake = 30
        };

        // Assert
        stages.Deep.Should().Be(90);
        stages.Light.Should().Be(200);
        stages.Rem.Should().Be(120);
        stages.Wake.Should().Be(30);
    }

    [Fact]
    public void Deserialize_ShouldMapJsonPropertyNames_WhenDeserializedFromJson()
    {
        // Arrange
        var json = """{"deep": 90, "light": 200, "rem": 120, "wake": 30}""";

        // Act
        var stages = JsonSerializer.Deserialize<Stages>(json);

        // Assert
        stages.Should().NotBeNull();
        stages!.Deep.Should().Be(90);
        stages.Light.Should().Be(200);
        stages.Rem.Should().Be(120);
        stages.Wake.Should().Be(30);
    }
}
