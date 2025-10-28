using FluentAssertions;
using Xunit;

namespace Biotrackr.Weight.Api.IntegrationTests;

/// <summary>
/// Smoke tests to verify API infrastructure
/// </summary>
[Collection(nameof(ContractTestCollection))]
public class ApiSmokeTests
{
    private readonly ContractTestFixture _fixture;

    public ApiSmokeTests(ContractTestFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public void Api_Should_Start_Successfully()
    {
        // Arrange
        var factory = _fixture.Factory;

        // Act - Just verify we can create a client (proves app started)
        var client = factory.CreateClient();

        // Assert
        client.Should().NotBeNull();
        client.BaseAddress.Should().NotBeNull();
    }

    [Fact]
    public void WebApplicationFactory_Should_Create_Client()
    {
        // Arrange & Act
        var client = _fixture.Factory.CreateClient();

        // Assert
        client.Should().NotBeNull();
        client.BaseAddress.Should().NotBeNull();
    }
}
