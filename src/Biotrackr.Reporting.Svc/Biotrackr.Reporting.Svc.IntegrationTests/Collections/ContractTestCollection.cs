using Biotrackr.Reporting.Svc.IntegrationTests.Fixtures;

namespace Biotrackr.Reporting.Svc.IntegrationTests.Collections;

[CollectionDefinition(nameof(ContractTestCollection))]
public class ContractTestCollection : ICollectionFixture<ContractTestFixture>
{
}
