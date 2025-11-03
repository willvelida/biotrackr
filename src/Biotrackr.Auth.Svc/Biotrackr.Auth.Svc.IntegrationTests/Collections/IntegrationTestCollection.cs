using Biotrackr.Auth.Svc.IntegrationTests.Fixtures;

namespace Biotrackr.Auth.Svc.IntegrationTests.Collections
{
    /// <summary>
    /// xUnit collection for E2E integration tests.
    /// Shares IntegrationTestFixture across all tests in this collection.
    /// </summary>
    [CollectionDefinition("IntegrationTestCollection")]
    public class IntegrationTestCollection : ICollectionFixture<IntegrationTestFixture>
    {
        // This class has no code, and is never created. Its purpose is simply
        // to be the place to apply [CollectionDefinition] and all the
        // ICollectionFixture<> interfaces.
    }
}
