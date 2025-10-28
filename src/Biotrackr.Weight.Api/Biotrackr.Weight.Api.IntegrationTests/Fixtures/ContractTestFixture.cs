namespace Biotrackr.Weight.Api.IntegrationTests;

/// <summary>
/// Lightweight fixture for contract/smoke tests
/// Skips database initialization to allow quick API startup verification
/// </summary>
public class ContractTestFixture : IntegrationTestFixture
{
    protected override bool InitializeDatabase => false;
}
