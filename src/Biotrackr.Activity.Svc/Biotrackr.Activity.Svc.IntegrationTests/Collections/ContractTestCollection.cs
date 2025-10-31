using Biotrackr.Activity.Svc.IntegrationTests.Fixtures;

namespace Biotrackr.Activity.Svc.IntegrationTests.Collections;

[CollectionDefinition(nameof(ContractTestCollection))]
public class ContractTestCollection : ICollectionFixture<ContractTestFixture>
{
}
