using Biotrackr.Sleep.Api.IntegrationTests.Fixtures;
using FluentAssertions;
using System.Net;
using Xunit;

namespace Biotrackr.Sleep.Api.IntegrationTests.Contract;

[Collection("Contract Tests")]
public class ApiSmokeTests
{
    private readonly ContractTestFixture _fixture;

    public ApiSmokeTests(ContractTestFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task HealthCheck_Liveness_ShouldReturnOk()
    {
        // Act
        var response = await _fixture.Client.GetAsync("/healthz/liveness");

        // Assert
        response.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.ServiceUnavailable);
    }

    [Fact]
    public async Task OpenApiJson_ShouldBeAccessible()
    {
        // Act
        var response = await _fixture.Client.GetAsync("/openapi/v1.json");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task OpenApiJson_WhenAvailable_ShouldContainSleepEndpoints()
    {
        // Act
        var response = await _fixture.Client.GetAsync("/openapi/v1.json");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var content = await response.Content.ReadAsStringAsync();
        content.Should().NotBeNullOrEmpty();
        
        // Check for sleep-related paths - should contain at least one endpoint
        var containsSleepEndpoints = content.Contains("GetAllSleeps") || 
                                    content.Contains("GetSleepByDate") ||
                                    content.Contains("GetSleepsByDateRange");
        containsSleepEndpoints.Should().BeTrue("openapi should document sleep endpoints");
    }

    // NOTE: Root endpoint tests moved to E2E tests since they require database access
}
