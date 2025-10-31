using Biotrackr.Sleep.Api.IntegrationTests.Fixtures;
using Xunit;

namespace Biotrackr.Sleep.Api.IntegrationTests.E2E;

[CollectionDefinition("E2E Tests")]
public class E2ETestCollection : ICollectionFixture<IntegrationTestFixture>
{
    // This class has no code, and is never created.
    // Its purpose is simply to be the place to apply [CollectionDefinition]
    // and all the ICollectionFixture<> interfaces.
}
