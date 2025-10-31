using Biotrackr.Activity.Svc.IntegrationTests.Fixtures;

namespace Biotrackr.Activity.Svc.IntegrationTests.Collections;

[CollectionDefinition(nameof(IntegrationTestCollection))]
public class IntegrationTestCollection : ICollectionFixture<IntegrationTestFixture>
{
}
