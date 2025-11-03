namespace Biotrackr.Food.Svc.IntegrationTests.Collections;

/// <summary>
/// xUnit collection definition for E2E integration tests.
/// All test classes decorated with [Collection("IntegrationTests")] will share the same IntegrationTestFixture instance.
/// </summary>
[CollectionDefinition("IntegrationTests")]
public class IntegrationTestCollection : ICollectionFixture<IntegrationTestFixture>
{
    // This class has no code, and is never created. Its purpose is simply
    // to be the place to apply [CollectionDefinition] and all the
    // ICollectionFixture<> interfaces.
}
