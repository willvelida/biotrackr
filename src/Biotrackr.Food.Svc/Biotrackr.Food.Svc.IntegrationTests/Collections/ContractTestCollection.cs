namespace Biotrackr.Food.Svc.IntegrationTests.Collections;

/// <summary>
/// xUnit collection definition for contract tests.
/// All test classes decorated with [Collection("ContractTests")] will share the same ContractTestFixture instance.
/// </summary>
[CollectionDefinition("ContractTests")]
public class ContractTestCollection : ICollectionFixture<ContractTestFixture>
{
    // This class has no code, and is never created. Its purpose is simply
    // to be the place to apply [CollectionDefinition] and all the
    // ICollectionFixture<> interfaces.
}
