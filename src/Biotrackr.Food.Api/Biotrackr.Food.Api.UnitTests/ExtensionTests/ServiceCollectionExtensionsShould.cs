using Biotrackr.Food.Api.Extensions;
using Biotrackr.Food.Api.Services;
using Biotrackr.Food.Api.Services.Interfaces;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;

namespace Biotrackr.Food.Api.UnitTests.ExtensionTests;

public class ServiceCollectionExtensionsShould
{
    [Fact]
    public void AddFoodDocumentTranslator_ShouldRegisterSingleton_WhenCalled()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        services.AddFoodDocumentTranslator();

        // Assert
        services.Should().ContainSingle(d => d.ServiceType == typeof(IFoodDocumentTranslator))
            .Which.Should().Match<ServiceDescriptor>(d =>
                d.Lifetime == ServiceLifetime.Singleton && d.ImplementationType == typeof(FoodDocumentTranslator),
                "AGENT FIX: register IFoodDocumentTranslator as a Singleton of FoodDocumentTranslator; it is stateless. "
                + "See .github/instructions/csharp-conventions.instructions.md.");
    }
}
