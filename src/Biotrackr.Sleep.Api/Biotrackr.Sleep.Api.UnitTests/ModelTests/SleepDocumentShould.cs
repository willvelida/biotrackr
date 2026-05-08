using Biotrackr.Sleep.Api.Models;
using Biotrackr.Sleep.Api.Models.FitbitEntities;
using FluentAssertions;
using SleepModel = Biotrackr.Sleep.Api.Models.FitbitEntities.Sleep;

namespace Biotrackr.Sleep.Api.UnitTests.ModelTests;

public class SleepDocumentShould
{
    [Fact]
    public void Initialize_ShouldHaveDefaultValues_WhenCreatedWithDefaultConstructor()
    {
        // Arrange & Act
        var document = new SleepDocument();

        // Assert
        document.Id.Should().BeNull();
        document.Sleep.Should().BeNull();
        document.Date.Should().BeNull();
        document.DocumentType.Should().BeNull();
    }

    [Fact]
    public void AllowPropertyAssignment_ShouldRetainValues_WhenPropertiesAreSet()
    {
        // Arrange
        var sleepResponse = new SleepResponse
        {
            Sleep = new List<SleepModel> { new() { DateOfSleep = "2026-05-07" } },
            Summary = new Summary { TotalMinutesAsleep = 450 }
        };

        // Act
        var document = new SleepDocument
        {
            Id = "sleep-2026-05-07",
            Sleep = sleepResponse,
            Date = "2026-05-07",
            DocumentType = "sleep"
        };

        // Assert
        document.Id.Should().Be("sleep-2026-05-07");
        document.Sleep.Should().NotBeNull();
        document.Sleep.Sleep.Should().HaveCount(1);
        document.Date.Should().Be("2026-05-07");
        document.DocumentType.Should().Be("sleep");
    }
}
