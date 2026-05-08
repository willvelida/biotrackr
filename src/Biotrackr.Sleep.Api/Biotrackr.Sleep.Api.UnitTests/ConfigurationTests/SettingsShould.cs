using Biotrackr.Sleep.Api.Configuration;
using FluentAssertions;

namespace Biotrackr.Sleep.Api.UnitTests.ConfigurationTests;

public class SettingsShould
{
    [Fact]
    public void Allow_DatabaseName_ShouldBeSetAndRetrieved_WhenAssigned()
    {
        // Arrange
        var settings = new Settings();

        // Act
        settings.DatabaseName = "biotrackr-test";

        // Assert
        settings.DatabaseName.Should().Be("biotrackr-test");
    }

    [Fact]
    public void Allow_ContainerName_ShouldBeSetAndRetrieved_WhenAssigned()
    {
        // Arrange
        var settings = new Settings();

        // Act
        settings.ContainerName = "sleep-test";

        // Assert
        settings.ContainerName.Should().Be("sleep-test");
    }

    [Fact]
    public void Create_ShouldInitializeAllProperties_WhenObjectInitializerUsed()
    {
        // Arrange & Act
        var settings = new Settings
        {
            DatabaseName = "biotrackr",
            ContainerName = "sleep"
        };

        // Assert
        settings.Should().NotBeNull();
        settings.DatabaseName.Should().Be("biotrackr");
        settings.ContainerName.Should().Be("sleep");
    }

    [Fact]
    public void Allow_ShouldUpdateProperties_WhenModifiedAfterCreation()
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
