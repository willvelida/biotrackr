namespace Biotrackr.Sleep.Svc.IntegrationTests.Collections
{
    [CollectionDefinition("SleepServiceContractTests")]
    public class ContractTestCollection : ICollectionFixture<Fixtures.ContractTestFixture>
    {
        // This class has no code, and is never created. Its purpose is simply
        // to be the place to apply [CollectionDefinition] and all the
        // ICollectionFixture<> interfaces.
    }
}
