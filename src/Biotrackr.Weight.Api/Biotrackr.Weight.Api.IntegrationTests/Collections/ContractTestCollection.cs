using Xunit;

namespace Biotrackr.Weight.Api.IntegrationTests;

/// <summary>
/// Collection definition for contract/smoke tests
/// Ensures tests share the same ContractTestFixture instance
/// </summary>
[CollectionDefinition(nameof(ContractTestCollection))]
public class ContractTestCollection : ICollectionFixture<ContractTestFixture>
{
}
