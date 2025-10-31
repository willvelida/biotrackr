using Biotrackr.Sleep.Api.IntegrationTests.Collections;
using Biotrackr.Sleep.Api.IntegrationTests.Fixtures;
using FluentAssertions;
using System.Net;
using Xunit;

namespace Biotrackr.Sleep.Api.IntegrationTests.E2E;

/// <summary>
/// E2E tests for health check endpoints with real Cosmos DB
/// Tests verify that health checks properly validate database connectivity
/// </summary>
[Collection(E2ETestCollection.CollectionName)]
public class HealthCheckTests
{
    private readonly IntegrationTestFixture _fixture;

    public HealthCheckTests(IntegrationTestFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task LivenessHealthCheck_WithDatabase_ShouldReturnHealthy()
    {
        // Arrange - Clear container for test isolation per common-resolutions.md
        await ClearContainerAsync();

        // Act
        var response = await _fixture.Client.GetAsync("/healthz/liveness");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var content = await response.Content.ReadAsStringAsync();
        content.Should().Be("Healthy");
    }

    // Note: Sleep API only has /healthz/liveness endpoint
    // Readiness check test removed as endpoint doesn't exist in this API

    /// <summary>
    /// Clears all documents from the test container to ensure test isolation.
    /// Per common-resolutions.md: E2E tests must clean up to avoid data contamination.
    /// </summary>
    private async Task ClearContainerAsync()
    {
        var query = new Microsoft.Azure.Cosmos.QueryDefinition("SELECT c.id, c.documentType FROM c");
        var iterator = _fixture.Container.GetItemQueryIterator<dynamic>(query);

        while (iterator.HasMoreResults)
        {
            var response = await iterator.ReadNextAsync();
            foreach (var item in response)
            {
                await _fixture.Container.DeleteItemAsync<dynamic>(
                    item.id.ToString(),
                    new Microsoft.Azure.Cosmos.PartitionKey(item.documentType.ToString()));
            }
        }
    }
}
