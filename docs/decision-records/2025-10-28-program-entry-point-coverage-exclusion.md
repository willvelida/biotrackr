# Decision Record: Program Entry Point Coverage Exclusion

- **Status**: Accepted
- **Deciders**: Development Team
- **Date**: 28 October 2025
- **Related Docs**: [Integration Test Project Structure](2025-10-28-integration-test-project-structure.md), Feature Spec `002-weight-svc-coverage`

## Context

The Biotrackr.Weight.Svc project uses top-level statements in Program.cs as the application entry point, containing dependency injection configuration, service registration, OpenTelemetry setup, and Azure App Configuration integration. During unit test coverage analysis, Program.cs showed 0% coverage, significantly impacting the overall coverage percentage (bringing it down from 100% testable code coverage to ~55% overall).

Application entry points like Program.cs are difficult to unit test because they:
- Require actual infrastructure (Azure services, Key Vault, Cosmos DB)
- Contain environment-specific configuration
- Are best validated through integration or end-to-end tests
- Primarily consist of framework-level wiring rather than business logic

## Decision

Program.cs will be excluded from unit test code coverage metrics using the `[ExcludeFromCodeCoverage]` attribute. This is implemented by:

1. Wrapping the top-level statements in an internal `Program` class with a static `Main` method
2. Applying `[ExcludeFromCodeCoverage]` attribute to the Program class
3. Adding `using System.Diagnostics.CodeAnalysis;` to access the attribute

**Implementation:**
```csharp
using System.Diagnostics.CodeAnalysis;
// ... other usings ...

[ExcludeFromCodeCoverage]
internal class Program
{
    private static void Main(string[] args)
    {
        // ... existing Program.cs code ...
    }
}
```

Program.cs functionality will continue to be validated through:
- Integration tests that verify the full application startup and configuration
- Manual testing during deployment validation
- Health check endpoints that confirm services are properly configured

## Consequences

### Positive
- **Accurate coverage metrics**: Coverage percentages now reflect only testable business logic (100% coverage achieved)
- **Industry standard practice**: Aligns with common practices in .NET projects where entry points are excluded from unit test coverage
- **Focus on value**: Development effort focuses on testing business logic rather than infrastructure wiring
- **Clear separation**: Distinguishes between code that should be unit tested vs. integration tested

### Negative
- **Reduced visibility**: Program.cs configuration issues won't be caught by unit tests
- **Manual validation needed**: Changes to DI registration require careful integration testing
- **Documentation requirement**: Team must understand that Program.cs is intentionally excluded

### Mitigation
- Integration tests in `Biotrackr.Weight.Svc.IntegrationTests` validate the full application startup
- Code reviews specifically check Program.cs changes for proper service registration
- Health check endpoints verify critical dependencies are configured correctly

## Alternatives Considered

### 1. Write Unit Tests for Program.cs
**Pros**: Would achieve 100% coverage including entry point  
**Cons**: 
- Requires complex test setup with test servers and mocked infrastructure
- Creates brittle tests that break with every DI configuration change
- Provides minimal value as these are better tested through integration tests
- Significant time investment for low ROI

**Why rejected**: The effort-to-value ratio is poor, and integration tests provide better validation of Program.cs functionality.

### 2. Use Integration Tests Only, No Coverage Exclusion
**Pros**: Tests the actual runtime behavior  
**Cons**: 
- Integration tests don't contribute to unit test coverage metrics
- Would still show 0% coverage for Program.cs
- Doesn't solve the coverage reporting problem

**Why rejected**: Doesn't address the core issue of coverage metrics being skewed by untestable entry point code.

### 3. Exclude via Coverlet Configuration
**Pros**: Could exclude Program.cs in coverlet configuration without code changes  
**Cons**: 
- Less explicit than attribute-based exclusion
- Configuration can be overlooked during code reviews
- Doesn't self-document the exclusion decision in the codebase

**Why rejected**: The `[ExcludeFromCodeCoverage]` attribute makes the exclusion explicit and self-documenting in the source code itself.

### 4. Refactor All Configuration into Testable Services
**Pros**: Would make everything unit testable  
**Cons**: 
- Significantly increases complexity with additional abstraction layers
- Goes against .NET conventions for program startup
- Creates maintenance burden for minimal benefit
- Still requires integration tests to verify actual configuration

**Why rejected**: Over-engineering the solution; the current approach with attributed exclusion is simpler and more maintainable.

## Follow-up Actions

- [x] Apply `[ExcludeFromCodeCoverage]` attribute to Program.cs - **Completed 2025-10-28**
- [x] Verify coverage reports correctly exclude Program.cs - **Completed 2025-10-28**
- [ ] Document this pattern in project README/contribution guidelines - **Owner: TBD**
- [ ] Apply same exclusion pattern to other service Program.cs files (Activity, Sleep, Auth, Food) - **Owner: TBD**
- [ ] Ensure CI/CD pipeline coverage gates use the correct coverage thresholds (70%+) - **Owner: TBD**

## Notes

This decision applies specifically to Program.cs entry point files. Other infrastructure/configuration code (e.g., Startup classes, middleware) should be evaluated on a case-by-case basis.

Reference projects following similar patterns:
- Microsoft.eShopOnContainers
- .NET Aspire samples
- ASP.NET Core documentation examples

**Coverage Achievement**: After applying this exclusion, Biotrackr.Weight.Svc achieved 100% coverage of testable code (105/105 lines), exceeding the 70% requirement.
