using Biotrackr.Weight.Api.Configuration;
using FluentAssertions;
using Xunit;

namespace Biotrackr.Weight.Api.UnitTests.ConfigurationTests;

public class SettingsShould
{
    [Fact]
    public void Create_Settings_With_Default_Values()
    {
        // Act
        var settings = new Settings();

        // Assert
        settings.Should().NotBeNull();
        settings.DatabaseName.Should().BeNull();
        settings.ContainerName.Should().BeNull();
    }

    [Fact]
    public void Set_DatabaseName_Property()
    {
        // Arrange
        var settings = new Settings();
        var expectedDatabaseName = "biotrackr-test";

        // Act
        settings.DatabaseName = expectedDatabaseName;

        // Assert
        settings.DatabaseName.Should().Be(expectedDatabaseName);
    }

    [Fact]
    public void Set_ContainerName_Property()
    {
        // Arrange
        var settings = new Settings();
        var expectedContainerName = "weight-test";

        // Act
        settings.ContainerName = expectedContainerName;

        // Assert
        settings.ContainerName.Should().Be(expectedContainerName);
    }

    [Fact]
    public void Allow_Empty_String_For_DatabaseName()
    {
        // Arrange
        var settings = new Settings();

        // Act
        settings.DatabaseName = string.Empty;

        // Assert
        settings.DatabaseName.Should().BeEmpty();
    }

    [Fact]
    public void Allow_Empty_String_For_ContainerName()
    {
        // Arrange
        var settings = new Settings();

        // Act
        settings.ContainerName = string.Empty;

        // Assert
        settings.ContainerName.Should().BeEmpty();
    }

    [Fact]
    public void Create_Settings_With_Object_Initializer()
    {
        // Arrange & Act
        var settings = new Settings
        {
            DatabaseName = "biotrackr",
            ContainerName = "weight"
        };

        // Assert
        settings.DatabaseName.Should().Be("biotrackr");
        settings.ContainerName.Should().Be("weight");
    }

    [Fact]
    public void Allow_Modification_After_Creation()
    {
        // Arrange
        var settings = new Settings
        {
            DatabaseName = "initial-db",
            ContainerName = "initial-container"
        };

        // Act
        settings.DatabaseName = "modified-db";
        settings.ContainerName = "modified-container";

        // Assert
        settings.DatabaseName.Should().Be("modified-db");
        settings.ContainerName.Should().Be("modified-container");
    }
}
