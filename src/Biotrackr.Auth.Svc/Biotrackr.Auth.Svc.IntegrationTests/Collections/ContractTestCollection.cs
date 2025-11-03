using Biotrackr.Auth.Svc.IntegrationTests.Fixtures;

namespace Biotrackr.Auth.Svc.IntegrationTests.Collections
{
    /// <summary>
    /// xUnit collection for contract tests.
    /// Shares ContractTestFixture across all tests in this collection.
    /// </summary>
    [CollectionDefinition("ContractTestCollection")]
    public class ContractTestCollection : ICollectionFixture<ContractTestFixture>
    {
        // This class has no code, and is never created. Its purpose is simply
        // to be the place to apply [CollectionDefinition] and all the
        // ICollectionFixture<> interfaces.
    }
}
