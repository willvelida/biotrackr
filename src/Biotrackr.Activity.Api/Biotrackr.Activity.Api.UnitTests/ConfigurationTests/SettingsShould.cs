using Biotrackr.Activity.Api.Configuration;
using FluentAssertions;
using Xunit;

namespace Biotrackr.Activity.Api.UnitTests.ConfigurationTests;

/// <summary>
/// Unit tests for Settings configuration class
/// T019-T023: Configuration tests
/// </summary>
public class SettingsShould
{
    [Fact]
    public void Allow_DatabaseName_To_Be_Set()
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
    public void Allow_ContainerName_To_Be_Set()
    {
        // Arrange
        var settings = new Settings();
        var expectedContainerName = "activity-test";

        // Act
        settings.ContainerName = expectedContainerName;

        // Assert
        settings.ContainerName.Should().Be(expectedContainerName);
    }

    [Fact]
    public void Create_Valid_Settings_Object_With_All_Properties()
    {
        // Arrange & Act
        var settings = new Settings
        {
            DatabaseName = "biotrackr",
            ContainerName = "activity"
        };

        // Assert
        settings.Should().NotBeNull();
        settings.DatabaseName.Should().Be("biotrackr");
        settings.ContainerName.Should().Be("activity");
    }

    [Fact]
    public void Allow_Properties_To_Be_Modified_After_Creation()
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

    [Fact]
    public void Support_Different_Database_And_Container_Names()
    {
        // Arrange & Act
        var productionSettings = new Settings
        {
            DatabaseName = "biotrackr-prod",
            ContainerName = "activity-prod"
        };

        var testSettings = new Settings
        {
            DatabaseName = "biotrackr-test",
            ContainerName = "activity-test"
        };

        // Assert
        productionSettings.DatabaseName.Should().NotBe(testSettings.DatabaseName);
        productionSettings.ContainerName.Should().NotBe(testSettings.ContainerName);
    }
}
