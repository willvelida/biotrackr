using System.Net;
using System.Text.Json;
using Biotrackr.Mcp.Server.IntegrationTests.Fixtures;
using FluentAssertions;

namespace Biotrackr.Mcp.Server.IntegrationTests.Contract
{
    /// <summary>
    /// Tests that verify the health endpoint behavior under various scenarios.
    /// </summary>
    [Collection(nameof(Collections.IntegrationTestCollection))]
    public class HealthEndpointShould
    {
        private readonly IntegrationTestFixture _fixture;

        public HealthEndpointShould(IntegrationTestFixture fixture)
        {
            _fixture = fixture;
        }

        [Fact]
        public async Task ReturnOkStatusCode()
        {
            // Arrange & Act
            var response = await _fixture.Client.GetAsync("/api/healthz");

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);
        }

        [Fact]
        public async Task ReturnJsonResponse()
        {
            // Arrange & Act
            var response = await _fixture.Client.GetAsync("/api/healthz");
            var content = await response.Content.ReadAsStringAsync();

            // Assert
            response.Content.Headers.ContentType?.MediaType.Should().Be("application/json");
            var json = JsonDocument.Parse(content);
            json.RootElement.GetProperty("status").GetString().Should().NotBeNullOrEmpty();
        }

        [Fact]
        public async Task IncludeDownstreamStatus()
        {
            // Arrange & Act
            var response = await _fixture.Client.GetAsync("/api/healthz");
            var content = await response.Content.ReadAsStringAsync();

            // Assert
            var json = JsonDocument.Parse(content);
            json.RootElement.TryGetProperty("downstream", out _).Should().BeTrue();
        }
    }
}
