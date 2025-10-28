using Biotrackr.Weight.Svc.Configuration;
using Biotrackr.Weight.Svc.IntegrationTests.Collections;
using Biotrackr.Weight.Svc.IntegrationTests.Fixtures;
using Biotrackr.Weight.Svc.Repositories.Interfaces;
using Biotrackr.Weight.Svc.Services.Interfaces;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Xunit;

namespace Biotrackr.Weight.Svc.IntegrationTests.Contract
{
    /// <summary>
    /// Contract tests for application startup and service provider configuration.
    /// These tests verify DI configuration without connecting to external dependencies.
    /// </summary>
    [Collection("Contract Tests")]
    public class ProgramStartupTests
    {
        private readonly ContractTestFixture _fixture;

        public ProgramStartupTests(ContractTestFixture fixture)
        {
            _fixture = fixture;
        }

        [Fact]
        public void Application_Builds_Service_Provider_Successfully()
        {
            // Arrange & Act
            var serviceProvider = _fixture.ServiceProvider;

            // Assert
            serviceProvider.Should().NotBeNull("the service provider should be created successfully");
        }

        [Fact]
        public void All_Required_Services_Are_Registered()
        {
            // Arrange
            var serviceProvider = _fixture.ServiceProvider;

            // Act - Resolve each required service
            var cosmosRepository = serviceProvider.GetService<ICosmosRepository>();
            var weightService = serviceProvider.GetService<IWeightService>();
            var fitbitService = serviceProvider.GetService<IFitbitService>();

            // Assert
            cosmosRepository.Should().NotBeNull("ICosmosRepository should be registered");
            cosmosRepository.Should().BeAssignableTo<ICosmosRepository>("ICosmosRepository should resolve to correct implementation");

            weightService.Should().NotBeNull("IWeightService should be registered");
            weightService.Should().BeAssignableTo<IWeightService>("IWeightService should resolve to correct implementation");

            fitbitService.Should().NotBeNull("IFitbitService should be registered");
            fitbitService.Should().BeAssignableTo<IFitbitService>("IFitbitService should resolve to correct implementation");
        }

        [Fact]
        public void Settings_Are_Bound_From_Configuration()
        {
            // Arrange
            var serviceProvider = _fixture.ServiceProvider;

            // Act
            var settings = serviceProvider.GetService<IOptions<Settings>>();

            // Assert
            settings.Should().NotBeNull("Settings should be registered with Options pattern");
            settings!.Value.Should().NotBeNull("Settings value should be bound");
            settings.Value.DatabaseName.Should().Be("test-db", "DatabaseName should match configuration");
            settings.Value.ContainerName.Should().Be("test-container", "ContainerName should match configuration");
        }
    }
}
