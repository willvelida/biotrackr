using Biotrackr.Sleep.Api.IntegrationTests.Fixtures;
using Xunit;

namespace Biotrackr.Sleep.Api.IntegrationTests.Collections;

/// <summary>
/// Collection definition for E2E tests that require full database initialization
/// Per decision-record 2025-10-28-integration-test-project-structure.md
/// </summary>
[CollectionDefinition(nameof(E2ETestCollection))]
public class E2ETestCollection : ICollectionFixture<IntegrationTestFixture>
{
    // This class has no code, and is never created. Its purpose is simply
    // to be the place to apply [CollectionDefinition] and all the
    // ICollectionFixture<> interfaces.
}
