namespace Biotrackr.Mcp.Server.IntegrationTests.Collections
{
    /// <summary>
    /// Collection definition for integration tests that share a single
    /// McpServerWebApplicationFactory instance.
    /// </summary>
    [CollectionDefinition(nameof(IntegrationTestCollection))]
    public class IntegrationTestCollection : ICollectionFixture<Fixtures.IntegrationTestFixture>
    {
    }
}
