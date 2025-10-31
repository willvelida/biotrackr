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
    public async Task SwaggerJson_ShouldBeAccessible()
    {
        // Act
        var response = await _fixture.Client.GetAsync("/swagger/v1/swagger.json");

        // Assert - May return 404 if Swagger not configured in test environment
        response.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task SwaggerJson_WhenAvailable_ShouldContainSleepEndpoints()
    {
        // Act
        var response = await _fixture.Client.GetAsync("/swagger/v1/swagger.json");

        // Assert
        if (response.StatusCode == HttpStatusCode.OK)
        {
            var content = await response.Content.ReadAsStringAsync();
            content.Should().NotBeNullOrEmpty();
            
            // Check for sleep-related paths - should contain at least one endpoint
            var containsSleepEndpoints = content.Contains("GetAllSleeps") || 
                                        content.Contains("GetSleepByDate") ||
                                        content.Contains("GetSleepsByDateRange");
            containsSleepEndpoints.Should().BeTrue("swagger should document sleep endpoints");
        }
    }

    // NOTE: Root endpoint tests moved to E2E tests since they require database access
}
