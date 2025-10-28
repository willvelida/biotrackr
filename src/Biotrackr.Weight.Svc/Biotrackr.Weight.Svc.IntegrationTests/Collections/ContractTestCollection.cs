using Biotrackr.Weight.Svc.IntegrationTests.Fixtures;
using Xunit;

namespace Biotrackr.Weight.Svc.IntegrationTests.Collections
{
    /// <summary>
    /// xUnit test collection for contract tests.
    /// All tests in this collection share the same ContractTestFixture instance.
    /// </summary>
    [CollectionDefinition("Contract Tests")]
    public class ContractTestCollection : ICollectionFixture<ContractTestFixture>
    {
        // This class is never instantiated - it's just a marker for xUnit
    }
}
