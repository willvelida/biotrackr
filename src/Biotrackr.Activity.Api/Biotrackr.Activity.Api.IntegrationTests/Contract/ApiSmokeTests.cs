using FluentAssertions;
using System.Net;
using System.Text.Json;
using Biotrackr.Activity.Api.IntegrationTests.Collections;
using Biotrackr.Activity.Api.IntegrationTests.Fixtures;
using Xunit;

namespace Biotrackr.Activity.Api.IntegrationTests.Contract;

/// <summary>
/// Smoke tests to verify API infrastructure and basic endpoints
/// These tests verify the API starts correctly and basic endpoints are accessible
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

    [Fact]
    public async Task HealthCheck_Endpoint_Should_Return_Healthy()
    {
        // Arrange
        var client = _fixture.Factory.CreateClient();

        // Act
        var response = await client.GetAsync("/healthz/liveness");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Swagger_UI_Should_Be_Available()
    {
        // Arrange
        var client = _fixture.Factory.CreateClient();

        // Act
        var response = await client.GetAsync("/swagger");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Swagger_JSON_Should_Return_Valid_OpenAPI_Document()
    {
        // Arrange
        var client = _fixture.Factory.CreateClient();

        // Act
        var response = await client.GetAsync("/swagger/v1/swagger.json");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        response.Content.Headers.ContentType.Should().NotBeNull();
        response.Content.Headers.ContentType!.MediaType.Should().Be("application/json");

        var content = await response.Content.ReadAsStringAsync();
        content.Should().NotBeNullOrEmpty();

        // Verify it's valid JSON
        var jsonDoc = JsonDocument.Parse(content);
        jsonDoc.Should().NotBeNull();

        // Verify it contains OpenAPI structure
        jsonDoc.RootElement.TryGetProperty("openapi", out var openapiVersion).Should().BeTrue();
        jsonDoc.RootElement.TryGetProperty("info", out var info).Should().BeTrue();
        jsonDoc.RootElement.TryGetProperty("paths", out var paths).Should().BeTrue();
    }
}
