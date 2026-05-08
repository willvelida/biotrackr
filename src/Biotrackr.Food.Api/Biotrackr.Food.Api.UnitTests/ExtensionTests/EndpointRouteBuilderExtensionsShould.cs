using Biotrackr.Food.Api.Extensions;
using FluentAssertions;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;

namespace Biotrackr.Food.Api.UnitTests.ExtensionTests;

public class EndpointRouteBuilderExtensionsShould
{
    [Fact]
    public void RegisterFoodEndpoints_ShouldExecuteWithoutException_WhenCalled()
    {
        // Arrange
        var builder = WebApplication.CreateBuilder();
        var app = builder.Build();

        // Act
        Action act = () => app.RegisterFoodEndpoints();

        // Assert
        act.Should().NotThrow();
    }

    [Fact]
    public void RegisterHealthCheckEndpoints_ShouldExecuteWithoutException_WhenCalled()
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
