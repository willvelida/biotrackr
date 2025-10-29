using Biotrackr.Activity.Api.IntegrationTests.Fixtures;
using Xunit;

namespace Biotrackr.Activity.Api.IntegrationTests.Collections;

/// <summary>
/// Collection definition for contract/smoke tests
/// Ensures tests share the same ContractTestFixture instance
/// Per decision-record 2025-10-28-integration-test-project-structure.md
/// </summary>
[CollectionDefinition(nameof(ContractTestCollection))]
public class ContractTestCollection : ICollectionFixture<ContractTestFixture>
{
}
