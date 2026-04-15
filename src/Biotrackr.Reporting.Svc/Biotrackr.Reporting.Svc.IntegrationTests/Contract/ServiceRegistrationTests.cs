using Biotrackr.Reporting.Svc.IntegrationTests.Collections;
using Biotrackr.Reporting.Svc.IntegrationTests.Fixtures;
using Biotrackr.Reporting.Svc.Services.Interfaces;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;

namespace Biotrackr.Reporting.Svc.IntegrationTests.Contract;

[Collection(nameof(ContractTestCollection))]
public class ServiceRegistrationTests(ContractTestFixture fixture)
{
    private readonly ContractTestFixture _fixture = fixture;

    [Fact]
    public void AgentTokenProvider_ShouldBeRegisteredAsSingleton()
    {
        // Arrange
        var serviceProvider = _fixture.ServiceProvider!;

        // Act
        var instance1 = serviceProvider.GetService<IAgentTokenProvider>();
        var instance2 = serviceProvider.GetService<IAgentTokenProvider>();

        // Assert
        instance1.Should().NotBeNull("IAgentTokenProvider should be registered");
        instance2.Should().NotBeNull("IAgentTokenProvider should be registered");
        instance1.Should().BeSameAs(instance2, "IAgentTokenProvider should be registered as Singleton");
    }

    [Fact]
    public void SummaryService_ShouldBeRegisteredAsScoped()
    {
        // Arrange
        var serviceProvider = _fixture.ServiceProvider!;

        // Act & Assert — same instance within scope
        using (var scope = serviceProvider.CreateScope())
        {
            var instance1 = scope.ServiceProvider.GetService<ISummaryService>();
            var instance2 = scope.ServiceProvider.GetService<ISummaryService>();

            instance1.Should().NotBeNull("ISummaryService should be registered");
            instance2.Should().NotBeNull("ISummaryService should be registered");
            instance1.Should().BeSameAs(instance2, "ISummaryService should return same instance within scope");
        }

        // Verify different instances across scopes
        ISummaryService? scopedInstance1;
        ISummaryService? scopedInstance2;

        using (var scope1 = serviceProvider.CreateScope())
        {
            scopedInstance1 = scope1.ServiceProvider.GetService<ISummaryService>();
        }

        using (var scope2 = serviceProvider.CreateScope())
        {
            scopedInstance2 = scope2.ServiceProvider.GetService<ISummaryService>();
        }

        ReferenceEquals(scopedInstance1, scopedInstance2).Should().BeFalse("ISummaryService should return different instances across scopes");
    }

    [Fact]
    public void MetricExtractor_ShouldBeRegisteredAsSingleton()
    {
        // Arrange
        var serviceProvider = _fixture.ServiceProvider!;

        // Act
        var instance1 = serviceProvider.GetService<IMetricExtractor>();
        var instance2 = serviceProvider.GetService<IMetricExtractor>();

        // Assert
        instance1.Should().NotBeNull("IMetricExtractor should be registered");
        instance2.Should().NotBeNull("IMetricExtractor should be registered");
        instance1.Should().BeSameAs(instance2, "IMetricExtractor should be registered as Singleton");
    }
}
