using Biotrackr.Weight.Svc.IntegrationTests.Fixtures;
using Xunit;

namespace Biotrackr.Weight.Svc.IntegrationTests.Collections
{
    /// <summary>
    /// xUnit test collection for E2E integration tests.
    /// All tests in this collection share the same IntegrationTestFixture instance.
    /// The fixture handles Cosmos DB Emulator lifecycle (setup/teardown).
    /// </summary>
    [CollectionDefinition("Integration Tests")]
    public class IntegrationTestCollection : ICollectionFixture<IntegrationTestFixture>
    {
        // This class is never instantiated - it's just a marker for xUnit
    }
}
