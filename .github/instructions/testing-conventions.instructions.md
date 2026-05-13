---
description: "Testing conventions for Biotrackr .NET test projects. Use when: writing or editing unit tests, integration tests, contract tests, or E2E tests."
applyTo: "**/*Tests*/**/*.cs"
---

# Testing Conventions

## Framework Stack

- **xUnit** — test runner
- **FluentAssertions** — assertion library (`Should()`, `BeOfType<T>()`)
- **Moq** — mocking (`Mock<T>`, `Setup`, `ReturnsAsync`)
- **AutoFixture** — test data generation (`fixture.Create<T>()`)
- **WebApplicationFactory** — ASP.NET integration testing
- **Cosmos DB Emulator** — E2E database testing

## Naming

- Test class: `{ClassUnderTest}Should` (e.g., `ActivityHandlersShould`)
- Test method: `{Method}_Should{Behavior}_When{Condition}`
- Example: `GetActivityByDate_ShouldReturnOk_WhenActivityIsFound`

## AAA Pattern

- Follow strict Arrange/Act/Assert with comments:

```csharp
[Fact]
public async Task GetActivityByDate_ShouldReturnOk_WhenActivityIsFound()
{
    // Arrange
    var date = "2022-01-01";
    var fixture = new Fixture();
    var activityDocument = fixture.Create<ActivityDocument>();
    activityDocument.Date = date;
    _cosmosRepositoryMock.Setup(x => x.GetActivitySummaryByDate(date)).ReturnsAsync(activityDocument);

    // Act
    var result = await ActivityHandlers.GetActivityByDate(_cosmosRepositoryMock.Object, date);

    // Assert
    result.Result.Should().BeOfType<Ok<ActivityDocument>>();
}
```

## Three Test Tiers

| Tier | Location | Filter | Database |
|------|----------|--------|----------|
| Unit | `*.UnitTests/` | Default (no filter) | Mocked |
| Contract | `*.IntegrationTests/Contract/` | `FullyQualifiedName~Contract` | None (DI only) |
| E2E | `*.IntegrationTests/E2E/` | `FullyQualifiedName~E2E` | Cosmos Emulator |

## Contract Tests

- Use `[Collection(nameof(ContractTestCollection))]` for fixture sharing
- `ContractTestFixture` inherits `IntegrationTestFixture` with `InitializeDatabase => false`
- Verify: service startup, DI registration (singleton/scoped/transient), health endpoint, OpenAPI doc
- Test `WebApplicationFactory` implementations must mirror production DI lifetimes (singleton/scoped/transient) — divergent lifetimes mask bugs and invalidate integration tests

## E2E Tests

- Use `[Collection(nameof(IntegrationTestCollection))]` for fixture sharing
- Implement `IAsyncLifetime` for setup/teardown
- `InitializeAsync()` clears container, seeds test data
- `DisposeAsync()` cleans up test documents
- Force `ConnectionMode.Gateway` for Cosmos Emulator (avoids TCP+SSL issues)

## Coverage

- 70% minimum warning threshold, 80% healthy (CI enforced)
- `coverage.runsettings` per service (excludes OpenAPI generated code)
- Use `[ExcludeFromCodeCoverage]` for generated or infrastructure code

## Agent-Readable Assertion Messages

When writing structural tests (convention enforcement, DI registration verification, route validation), include fix instructions in assertion failure messages. This creates computational feedback sensors that guide agents to self-correct.

Pattern:

```csharp
result.Should().BeOfType<string>(
    $"AGENT FIX: Parameter '{paramName}' must be string type. "
    + $"Date parameters use yyyy-MM-dd format, not DateTime. "
    + $"See .github/instructions/csharp-conventions.instructions.md.");
```

- Start assertion messages with `AGENT FIX:` prefix for easy identification
- Include the specific convention being violated
- Reference the instruction file containing the rule
- Keep messages actionable — tell the agent exactly what to change
