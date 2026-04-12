using Biotrackr.Chat.Api.Configuration;
using Biotrackr.Chat.Api.IntegrationTests.Fixtures;
using Biotrackr.Chat.Api.Services;
using Biotrackr.Chat.Api.Tools;
using FluentAssertions;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;

namespace Biotrackr.Chat.Api.IntegrationTests.Contract;

[Collection(nameof(ContractTestCollection))]
public class ServiceRegistrationTests
{
    private readonly ChatApiWebApplicationFactory _factory;

    public ServiceRegistrationTests(ChatApiWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public void McpToolService_ShouldBeRegisteredAsSingleton()
    {
        // Arrange & Act
        var instance1 = _factory.Services.GetRequiredService<IMcpToolService>();
        var instance2 = _factory.Services.GetRequiredService<IMcpToolService>();

        // Assert
        instance1.Should().BeSameAs(instance2);
    }

    [Fact]
    public void CosmosClientFactory_ShouldBeRegisteredAsScoped()
    {
        // Arrange & Act
        using var scope1 = _factory.Services.CreateScope();
        using var scope2 = _factory.Services.CreateScope();
        var instance1 = scope1.ServiceProvider.GetRequiredService<ICosmosClientFactory>();
        var instance2 = scope2.ServiceProvider.GetRequiredService<ICosmosClientFactory>();

        // Assert
        instance1.Should().NotBeSameAs(instance2);
    }

    [Fact]
    public void ChatHistoryRepository_ShouldBeRegisteredAsScoped()
    {
        // Arrange & Act
        using var scope1 = _factory.Services.CreateScope();
        using var scope2 = _factory.Services.CreateScope();
        var instance1 = scope1.ServiceProvider.GetRequiredService<IChatHistoryRepository>();
        var instance2 = scope2.ServiceProvider.GetRequiredService<IChatHistoryRepository>();

        // Assert
        instance1.Should().NotBeSameAs(instance2);
    }

    [Fact]
    public void AgentTokenProvider_ShouldBeRegisteredAsSingleton()
    {
        // Arrange & Act
        var instance1 = _factory.Services.GetRequiredService<IAgentTokenProvider>();
        var instance2 = _factory.Services.GetRequiredService<IAgentTokenProvider>();

        // Assert
        instance1.Should().BeSameAs(instance2);
    }

    [Fact]
    public void ReportReviewerService_ShouldBeRegisteredAsSingleton()
    {
        // Arrange & Act
        var instance1 = _factory.Services.GetRequiredService<IReportReviewerService>();
        var instance2 = _factory.Services.GetRequiredService<IReportReviewerService>();

        // Assert
        instance1.Should().BeSameAs(instance2);
    }

    [Fact]
    public void ChatAgentProvider_ShouldBeRegisteredAsSingleton()
    {
        // Arrange & Act
        var instance1 = _factory.Services.GetRequiredService<ChatAgentProvider>();
        var instance2 = _factory.Services.GetRequiredService<ChatAgentProvider>();

        // Assert
        instance1.Should().BeSameAs(instance2);
    }

    [Fact]
    public void MemoryCache_ShouldBeRegistered()
    {
        // Arrange & Act
        var cache = _factory.Services.GetRequiredService<IMemoryCache>();

        // Assert
        cache.Should().NotBeNull();
    }

    [Fact]
    public void Settings_ShouldBeRegisteredViaIOptions()
    {
        // Arrange & Act
        var options = _factory.Services.GetRequiredService<IOptions<Settings>>();

        // Assert
        options.Value.Should().NotBeNull();
    }

    [Fact]
    public void HealthChecks_ShouldBeRegistered()
    {
        // Arrange & Act
        var healthCheckService = _factory.Services.GetRequiredService<HealthCheckService>();

        // Assert
        healthCheckService.Should().NotBeNull();
    }
}
