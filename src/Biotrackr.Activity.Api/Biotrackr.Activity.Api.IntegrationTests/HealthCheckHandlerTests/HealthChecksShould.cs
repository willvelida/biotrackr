using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Testing;
using System.Net;

namespace Biotrackr.Activity.Api.IntegrationTests.HealthCheckHandlerTests
{
    public class HealthChecksShould : IClassFixture<CustomWebApplicationFactory<Program>>
    {
        private readonly CustomWebApplicationFactory<Program> _factory;

        public HealthChecksShould(CustomWebApplicationFactory<Program> factory)
        {
            _factory = factory;
        }

        [Fact]
        public async Task MapHealthChecks_ShouldReturn200Ok_WhenHealthCheckIsHealthy()
        {
            // Arrange
            var client = _factory.CreateClient(new WebApplicationFactoryClientOptions
            {
                BaseAddress = new Uri(Environment.GetEnvironmentVariable("apiurl"))
            });

            // Act
            var response = await client.GetAsync("/healthz/liveness");

            // Assert
            response.EnsureSuccessStatusCode();
            Assert.Equal((HttpStatusCode)StatusCodes.Status200OK, response.StatusCode);
        }
    }
}
