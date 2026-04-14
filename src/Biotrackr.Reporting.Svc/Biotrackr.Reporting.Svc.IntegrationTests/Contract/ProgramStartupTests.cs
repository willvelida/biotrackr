using Biotrackr.Reporting.Svc.IntegrationTests.Collections;
using Biotrackr.Reporting.Svc.IntegrationTests.Fixtures;
using Biotrackr.Reporting.Svc.Services.Interfaces;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Biotrackr.Reporting.Svc.IntegrationTests.Contract;

[Collection(nameof(ContractTestCollection))]
public class ProgramStartupTests(ContractTestFixture fixture)
{
    private readonly ContractTestFixture _fixture = fixture;

    [Fact]
    public void ApplicationHost_ShouldBuildSuccessfully()
    {
        // Arrange & Act - fixture initializes in constructor

        // Assert
        _fixture.ServiceProvider.Should().NotBeNull("application host should build successfully");
    }

    [Fact]
    public void ServiceProvider_ShouldResolveAllRequiredServices()
    {
        // Arrange
        var serviceProvider = _fixture.ServiceProvider!;

        // Act & Assert
        using var scope = serviceProvider.CreateScope();

        var summaryService = scope.ServiceProvider.GetService<ISummaryService>();
        summaryService.Should().NotBeNull("ISummaryService should be registered");

        var healthDataService = scope.ServiceProvider.GetService<IHealthDataService>();
        healthDataService.Should().NotBeNull("IHealthDataService should be registered");

        var reportingApiService = scope.ServiceProvider.GetService<IReportingApiService>();
        reportingApiService.Should().NotBeNull("IReportingApiService should be registered");

        var emailService = scope.ServiceProvider.GetService<IEmailService>();
        emailService.Should().NotBeNull("IEmailService should be registered");

        var agentTokenProvider = serviceProvider.GetService<IAgentTokenProvider>();
        agentTokenProvider.Should().NotBeNull("IAgentTokenProvider should be registered");
    }

    [Fact]
    public void Configuration_ShouldContainAllRequiredKeys()
    {
        // Arrange
        var configuration = _fixture.Configuration!;

        // Act & Assert
        configuration["keyvaulturl"].Should().NotBeNullOrEmpty("keyvaulturl configuration should be present");
        configuration["managedidentityclientid"].Should().NotBeNullOrEmpty("managedidentityclientid configuration should be present");
        configuration["applicationinsightsconnectionstring"].Should().NotBeNullOrEmpty("applicationinsightsconnectionstring configuration should be present");
        configuration["azureappconfigendpoint"].Should().NotBeNullOrEmpty("azureappconfigendpoint configuration should be present");
        configuration["Biotrackr:ReportingApiUrl"].Should().NotBeNullOrEmpty("Biotrackr:ReportingApiUrl configuration should be present");
        configuration["Biotrackr:McpServerUrl"].Should().NotBeNullOrEmpty("Biotrackr:McpServerUrl configuration should be present");
        configuration["Biotrackr:AcsEndpoint"].Should().NotBeNullOrEmpty("Biotrackr:AcsEndpoint configuration should be present");
    }
}
