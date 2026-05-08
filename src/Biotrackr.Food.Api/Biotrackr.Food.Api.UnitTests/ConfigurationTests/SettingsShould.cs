using Biotrackr.Food.Api.Configuration;
using FluentAssertions;

namespace Biotrackr.Food.Api.UnitTests.ConfigurationTests;

public class SettingsShould
{
    [Fact]
    public void Initialize_ShouldHaveEmptyDefaults_WhenCreatedWithDefaultConstructor()
    {
        // Arrange & Act
        var settings = new Settings();

        // Assert
        settings.DatabaseName.Should().BeEmpty();
        settings.ContainerName.Should().BeEmpty();
    }

    [Fact]
    public void AllowPropertyAssignment_ShouldRetainValues_WhenPropertiesAreSet()
    {
        // Arrange & Act
        var settings = new Settings
        {
            DatabaseName = "biotrackr",
            ContainerName = "food"
        };

        // Assert
        settings.DatabaseName.Should().Be("biotrackr");
        settings.ContainerName.Should().Be("food");
    }

    [Fact]
    public void AllowPropertyModification_ShouldUpdateValues_WhenModifiedAfterCreation()
    {
        // Arrange
        var settings = new Settings
        {
            DatabaseName = "original-db",
            ContainerName = "original-container"
        };

        // Act
        settings.DatabaseName = "updated-db";
        settings.ContainerName = "updated-container";

        // Assert
        settings.DatabaseName.Should().Be("updated-db");
        settings.ContainerName.Should().Be("updated-container");
    }
}
