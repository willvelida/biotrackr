---
on:
  schedule: weekly
  workflow_dispatch:
engine:
  id: copilot
permissions:
  contents: read
safe-outputs:
  create-issue:
    title-prefix: "[test-quality] "
    labels: [testing, automated]
    close-older-issues: true
    max: 1
timeout-minutes: 20
---

# Test Quality Sentinel

Analyze all Biotrackr test projects for quality and convention adherence across 14 services.

{{#runtime-import .github/instructions/testing-conventions.instructions.md}}

## Checks

1. **Flaky test detection**: Scan all `*.UnitTests/` and `*.IntegrationTests/` directories for:
   - `[Fact(Skip =` attributes indicating explicitly skipped flaky tests
   - Time-dependent assertions using `DateTime.Now`, `DateTime.UtcNow`, or `Task.Delay` in test bodies
   - Non-deterministic patterns such as `Random` without a seed or `Guid.NewGuid()` in assertion comparisons
   Report each finding with file path, class name, and method name.

2. **Assertion density**: For each test method marked with `[Fact]` or `[Theory]`, count assertions including FluentAssertions `.Should()` calls and Moq `.Verify()` calls. Also count `Assert.ThrowsAsync` and `.Invoking().Should().ThrowAsync()` as assertions. Flag tests with zero assertions as critical. Flag tests with exactly one assertion where the method name implies multiple behaviors as a warning.

3. **Test naming conventions**: Verify test naming across all services:
   - Unit test class names must follow `{ClassUnderTest}Should` pattern — flag violations as warnings
   - Test method names must follow `{Method}_Should{Behavior}_When{Condition}` pattern — flag violations as warnings
   - Integration test classes using `*Tests` suffix is a known accepted divergence — flag as informational only, not as a violation
   This check produces high finding volumes. Report per-service summary counts (total methods, compliant count, violation count, compliance percentage) rather than listing every individual violation. Include the top 5 worst-offending files per service with sample method names.

4. **AAA pattern comments**: Check that test methods include `// Arrange`, `// Act`, and `// Assert` comments. Accept `// Act & Assert` and `// Arrange & Act` as valid combined variants. Flag methods completely missing all AAA comments as warnings. This check produces high finding volumes. Report per-service summary counts and list only the top 5 worst-offending files per service.

5. **Test tier completeness**: For each of the 14 services, verify:
   - A `*.UnitTests/` directory exists (critical if missing)
   - A `*.IntegrationTests/Contract/` directory exists for all services except Biotrackr.UI (warning if missing)
   - A `*.IntegrationTests/E2E/` directory exists where expected (informational if missing)
   The UI service is exempt from integration test requirements.

6. **Excessive mocking**: Count `Mock<T>` field declarations in test classes (fields of type `Mock<T>` or initialized via `new Mock<T>()`). Flag test classes with 6 or more mock fields as warnings, suggesting the class under test may have too many dependencies.

## Output

If findings exist, create an issue organized as follows:

### Summary
A table with columns: Check Category, Critical, Warning, Informational, Total. One row per check (1-6). Include a grand total row.

### Low-Volume Findings (Full Detail)
For checks 1 (flaky tests), 2 (assertion density), 5 (test tier completeness), and 6 (excessive mocking), list every individual finding with:
- Severity emoji (🔴 Critical, 🟡 Warning, 🔵 Informational)
- Category, file path, class/method name, description
- Suggested remediation

### High-Volume Findings (Per-Service Summaries)
For checks 3 (naming conventions) and 4 (AAA pattern), present:
- A per-service summary table with columns: Service, Total Methods, Violations, Compliance %
- For each service with violations, a collapsible `<details>` section containing the top 5 worst-offending files with sample violation names

### Recommendations
List the top 3 priority items to address first, focusing on services with the highest violation density.

If no findings are detected across all services, call `noop` with: "All 14 services pass test quality checks — no convention violations detected."
