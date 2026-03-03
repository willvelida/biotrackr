using System.Net;
using Biotrackr.Mcp.Server.IntegrationTests.Fixtures;
using FluentAssertions;

namespace Biotrackr.Mcp.Server.IntegrationTests.Contract
{
    /// <summary>
    /// Tests that verify the MCP server starts up correctly and
    /// core infrastructure endpoints respond as expected.
    /// </summary>
    [Collection(nameof(Collections.IntegrationTestCollection))]
    public class ServerStartupShould
    {
        private readonly IntegrationTestFixture _fixture;

        public ServerStartupShould(IntegrationTestFixture fixture)
        {
            _fixture = fixture;
        }

        [Fact]
        public async Task StartSuccessfully_WhenConfigurationIsValid()
        {
            // Arrange & Act
            var response = await _fixture.Client.GetAsync("/api/healthz");

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);
        }

        [Fact]
        public async Task ReturnHealthyStatus_WhenDownstreamApiIsReachable()
        {
            // Arrange & Act
            var response = await _fixture.Client.GetAsync("/api/healthz");
            var content = await response.Content.ReadAsStringAsync();

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);
            content.Should().Contain("Healthy");
            content.Should().Contain("Reachable");
        }

        [Fact]
        public async Task ExposeMcpEndpoint()
        {
            // Arrange - MCP SDK default MapMcp() maps to root path "/"
            var request = new HttpRequestMessage(HttpMethod.Post, "/")
            {
                Content = new StringContent(
                    """{"jsonrpc": "2.0", "method": "initialize", "id": 1, "params": {"protocolVersion": "2025-03-26", "capabilities": {}, "clientInfo": {"name": "test", "version": "1.0"}}}""",
                    System.Text.Encoding.UTF8,
                    "application/json")
            };
            request.Headers.Accept.Add(new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("application/json"));
            request.Headers.Accept.Add(new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("text/event-stream"));

            // Act
            var response = await _fixture.Client.SendAsync(request);

            // Assert - Should get a valid response (not 404)
            response.StatusCode.Should().NotBe(HttpStatusCode.NotFound);
        }

        [Fact]
        public async Task Return404_ForUnknownEndpoints()
        {
            // Arrange & Act
            var response = await _fixture.Client.GetAsync("/nonexistent/endpoint");

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        }
    }
}
