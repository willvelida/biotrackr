namespace Biotrackr.Sleep.Api.IntegrationTests.Fixtures;

/// <summary>
/// Lightweight fixture for contract/smoke tests
/// Skips database initialization to allow quick API startup verification
/// Per decision-record 2025-10-28-contract-test-architecture.md - contract tests validate
/// service registration and basic startup without database dependencies
/// </summary>
public class ContractTestFixture : IntegrationTestFixture
{
    /// <summary>
    /// Disables database initialization for fast contract test execution
    /// </summary>
    protected override bool InitializeDatabase => false;
}
