using Biotrackr.Weight.Api.Extensions;
using FluentAssertions;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Biotrackr.Weight.Api.UnitTests.ExtensionTests;

/// <summary>
/// Tests for EndpointRouteBuilderExtensions.
/// Note: These tests verify that the extension methods execute without throwing exceptions,
/// which provides code coverage. Detailed endpoint routing and behavior is verified in integration tests.
/// </summary>
public class EndpointRouteBuilderExtensionsShould
{
    [Fact]
    public void RegisterWeightEndpoints_Should_Execute_Without_Exception()
    {
        // Arrange
        var builder = WebApplication.CreateBuilder();
        var app = builder.Build();

        // Act
        Action act = () => app.RegisterWeightEndpoints();

        // Assert
        act.Should().NotThrow();
    }

    [Fact]
    public void RegisterHealthCheckEndpoints_Should_Execute_Without_Exception()
    {
        // Arrange
        var builder = WebApplication.CreateBuilder();
        builder.Services.AddHealthChecks();
        var app = builder.Build();

        // Act
        Action act = () => app.RegisterHealthCheckEndpoints();

        // Assert
        act.Should().NotThrow();
    }
}
