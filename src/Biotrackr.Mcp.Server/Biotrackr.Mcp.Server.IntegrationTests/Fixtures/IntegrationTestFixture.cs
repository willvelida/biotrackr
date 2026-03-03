namespace Biotrackr.Mcp.Server.IntegrationTests.Fixtures
{
    /// <summary>
    /// Fixture for integration tests. Creates the factory and HTTP client.
    /// Implements IAsyncLifetime for proper setup/teardown.
    /// </summary>
    public class IntegrationTestFixture : IAsyncLifetime
    {
        public McpServerWebApplicationFactory Factory { get; private set; } = null!;
        public HttpClient Client { get; private set; } = null!;

        public Task InitializeAsync()
        {
            Factory = new McpServerWebApplicationFactory();
            Client = Factory.CreateClient();
            return Task.CompletedTask;
        }

        public async Task DisposeAsync()
        {
            Client?.Dispose();
            if (Factory != null)
            {
                await Factory.DisposeAsync();
            }
        }
    }
}
