using Biotrackr.Sleep.Api.IntegrationTests.Fixtures;
using Xunit;

namespace Biotrackr.Sleep.Api.IntegrationTests.Contract;

[CollectionDefinition("Contract Tests")]
public class ContractTestCollection : ICollectionFixture<ContractTestFixture>
{
    // This class has no code, and is never created.
    // Its purpose is simply to be the place to apply [CollectionDefinition]
    // and all the ICollectionFixture<> interfaces.
}
