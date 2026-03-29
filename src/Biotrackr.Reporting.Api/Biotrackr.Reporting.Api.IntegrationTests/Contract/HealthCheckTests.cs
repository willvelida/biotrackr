using System.Net;
using Biotrackr.Reporting.Api.IntegrationTests.Fixtures;
using FluentAssertions;

namespace Biotrackr.Reporting.Api.IntegrationTests.Contract;

/// <summary>
/// Smoke tests that verify the API starts up correctly, routes are mapped,
/// and the health check endpoint is accessible without authentication.
/// </summary>
public class HealthCheckTests : IClassFixture<ReportingApiWebApplicationFactory>
{
    private readonly HttpClient _client;

    public HealthCheckTests(ReportingApiWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task HealthCheck_ShouldReturn200WithStatus()
    {
        var response = await _client.GetAsync("/api/healthz");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var content = await response.Content.ReadAsStringAsync();
        content.Should().Contain("healthy");
    }
}
