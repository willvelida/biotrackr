using FluentAssertions;
using System.Net;
using Xunit;

namespace Biotrackr.Weight.Api.IntegrationTests;

/// <summary>
/// Smoke tests to verify API infrastructure
/// </summary>
[Collection(nameof(IntegrationTestCollection))]
public class ApiSmokeTests
{
    private readonly IntegrationTestFixture _fixture;

    public ApiSmokeTests(IntegrationTestFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task Api_Should_Start_Successfully()
    {
        // Arrange
        var client = _fixture.Client;

        // Act
        var response = await client.GetAsync("/health");

        // Assert
        response.Should().NotBeNull();
        response.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.NotFound);
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
