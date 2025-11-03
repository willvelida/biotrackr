using Biotrackr.Auth.Svc.IntegrationTests.Fixtures;
using FluentAssertions;
using Microsoft.Extensions.Configuration;

namespace Biotrackr.Auth.Svc.IntegrationTests.Contract
{
    /// <summary>
    /// Contract tests verifying that the application host can build successfully
    /// and configuration values are accessible.
    /// </summary>
    [Collection("ContractTestCollection")]
    public class ProgramStartupTests
    {
        private readonly ContractTestFixture _fixture;

        public ProgramStartupTests(ContractTestFixture fixture)
        {
            _fixture = fixture;
        }

        [Fact]
        public void HostBuildsSuccessfullyWithInMemoryConfiguration()
        {
            // Arrange & Act
            var serviceProvider = _fixture.ServiceProvider;
            var configuration = _fixture.Configuration;

            // Assert
            serviceProvider.Should().NotBeNull("the service provider should be successfully created");
            configuration.Should().NotBeNull("the configuration should be successfully created");
        }

        [Fact]
        public void ConfigurationValuesAreAccessibleFromServiceProvider()
        {
            // Arrange
            var configuration = _fixture.Configuration;

            // Act
            var keyVaultUrl = configuration["keyvaulturl"];
            var managedIdentityClientId = configuration["managedidentityclientid"];
            var appInsightsConnectionString = configuration["applicationinsightsconnectionstring"];

            // Assert
            keyVaultUrl.Should().NotBeNullOrEmpty("keyvaulturl should be configured");
            keyVaultUrl.Should().Be("https://test-keyvault.vault.azure.net/");

            managedIdentityClientId.Should().NotBeNullOrEmpty("managedidentityclientid should be configured");
            managedIdentityClientId.Should().Be("00000000-0000-0000-0000-000000000000");

            appInsightsConnectionString.Should().NotBeNullOrEmpty("applicationinsightsconnectionstring should be configured");
            appInsightsConnectionString.Should().StartWith("InstrumentationKey=");
        }
    }
}
