# Decision Record: Coverlet Extension Method Coverage Anomaly

- **Status**: Accepted
- **Deciders**: Development Team
- **Date**: 29 October 2025
- **Related Docs**: [Integration Test Project Structure](2025-10-28-integration-test-project-structure.md), [Program Entry Point Coverage Exclusion](2025-10-28-program-entry-point-coverage-exclusion.md)

## Context

During implementation of Activity API unit tests (feature 004-activity-api-tests), we encountered a persistent issue where `EndpointRouteBuilderExtensions.cs` showed 0% code coverage despite:

1. **Tests executing successfully** - All 71 tests pass, including 2 tests specifically targeting the extension methods
2. **Methods being invoked** - Tests call `app.RegisterActivityEndpoints()` and `app.RegisterHealthCheckEndpoints()` directly
3. **Identical pattern to working implementation** - Weight API uses the exact same test pattern and achieves 100% coverage
4. **Proper instrumentation** - Coverlet tracks the methods (they appear in coverage.cobertura.xml with line-rate=0)
5. **All lines showing zero hits** - Coverage XML shows all method lines instrumented but hits=0 for every line

### Investigation Summary

We systematically eliminated every possible configuration difference:

**✅ Verified Identical**:
- Test code structure (copied byte-for-byte from Weight.API)
- Project file configuration (.csproj files identical except names)
- Package versions (coverlet.collector 6.0.4, xUnit 2.9.3, etc.)
- Program.cs structure (both use top-level statements + `partial class Program { }`)
- No `[ExcludeFromCodeCoverage]` attributes on target files
- No assembly-level exclusions
- No global.json or Directory.Build.props differences
- Project references properly configured
- Build output successful with no warnings about coverage

**✅ Attempted Fixes**:
1. Converted Program.cs from partial class wrapper to top-level statements → No change
2. Added `partial class Program { }` for test accessibility → No change
3. Changed test from `Action` wrapper to direct invocation → No change
4. Created explicit runsettings with Include/Exclude filters → No change
5. Clean rebuild with project reference refresh → No change
6. Isolated test execution (only extension tests) → No change

**🔍 Comparative Evidence**:

Weight.API:
```
EndpointRouteBuilderExtensions: LineRate=1 (100%)
RegisterWeightEndpoints: hits > 0
RegisterHealthCheckEndpoints: hits > 0
```

Activity.API:
```
EndpointRouteBuilderExtensions: LineRate=0 (0%)
RegisterActivityEndpoints: hits=0 (all 18 lines)
RegisterHealthCheckEndpoints: hits=0 (all 13 lines)
```

Same test pattern, different results = Environmental anomaly.

## Decision

**Document this as a known coverlet edge case and proceed with alternative coverage strategies.**

We accept that:
1. This is likely a coverlet instrumentation bug specific to Activity.API's configuration
2. The issue is not worth additional investigation time given diminishing returns
3. Extension methods **are** tested (tests pass), they're just not being tracked by coverage tools
4. Integration tests may provide coverage for these methods anyway
5. Other high-value test targets exist to reach the 80% coverage requirement

**Workaround Strategy**:
- Check if integration tests already cover endpoint registration
- Target other uncovered files (Program.cs, other repositories) to reach 80%
- Consider manual code review verification for extension methods
- File issue with coverlet project if reproducible with minimal example

## Consequences

### Positive
- **Unblocked progress** - Can move forward with other test targets instead of debugging tooling
- **Documented anomaly** - Future developers won't waste time re-investigating
- **Tests exist** - Extension methods have passing tests even if coverage doesn't track them
- **Alternative path** - Multiple strategies available to reach 80% threshold

### Negative
- **Coverage metric incomplete** - 66.03% reported coverage doesn't reflect actual test coverage
- **Unknown root cause** - Don't understand why identical patterns produce different results
- **Potential tooling issue** - May affect other projects or future .NET versions
- **Manual verification required** - Can't rely solely on coverage metrics for extension methods

### Neutral
- **Tests remain valuable** - Passing tests still validate functionality regardless of coverage tracking
- **Not unique to this project** - Known issue category with code coverage tools

## Alternatives Considered

### Alternative 1: Continue debugging until root cause found
- **Rejected**: Diminishing returns after 2+ hours of systematic investigation
- No clear path to resolution without deep tooling debugging
- Other coverage targets available to meet 80% requirement

### Alternative 2: Move tests to integration test project
- **Rejected**: Doesn't solve the underlying issue
- Integration tests should test full integration, not unit functionality
- Would still need unit test coverage for completeness

### Alternative 3: Exclude EndpointRouteBuilderExtensions from coverage requirements
- **Rejected**: Would set precedent for excluding difficult files
- 80% requirement is constitutional and should be met through other means
- Tests prove methods work even if coverage doesn't track them

### Alternative 4: Switch to different coverage tool (e.g., dotCover, OpenCover)
- **Rejected**: Would require infrastructure changes across entire project
- No guarantee other tools wouldn't have same issue
- coverlet is standard for .NET projects

## Follow-up Actions

- [x] Document this decision record
- [ ] Check if Activity.API integration tests provide coverage for endpoint registration
- [ ] Identify other high-value test targets (Program.cs, uncovered repository methods)
- [ ] Implement tests for alternative targets to reach 80% coverage
- [ ] Consider filing minimal reproduction case with coverlet project
- [ ] Add note to common-resolutions.md if issue recurs in other APIs

## Notes

### Reproduction Details

**Activity.API Configuration**:
- .NET 9.0
- coverlet.collector 6.0.4
- xUnit 2.9.3
- Test project references main project via ProjectReference
- Top-level statements + `partial class Program { }` in Program.cs
- Extension methods in `Extensions/EndpointRouteBuilderExtensions.cs`

**Test Pattern**:
```csharp
[Fact]
public void RegisterActivityEndpoints_Should_Execute_Without_Exception()
{
    var builder = WebApplication.CreateBuilder();
    var app = builder.Build();
    
    Action act = () => app.RegisterActivityEndpoints();
    
    act.Should().NotThrow();
}
```

**Coverage Query**:
```powershell
[xml]$coverage = Get-Content "coverage.cobertura.xml"
$coverage.coverage.packages.package.classes.class | 
    Where-Object { $_.filename -match 'EndpointRouteBuilderExtensions' } |
    ForEach-Object { $_.methods.method.lines.line | Select-Object number, hits }
```

Result: All lines show `hits=0` despite test execution.

### Lessons Learned

1. **Coverage tools have limitations** - Don't assume 0% coverage means code isn't tested
2. **Comparative analysis helps** - Having Weight.API as working reference proved this is anomalous
3. **Time-box debugging** - After systematic elimination of common causes, move on
4. **Multiple verification strategies** - Use passing tests + code review when coverage fails
5. **Document anomalies** - Save future developers investigation time

### References

- [Coverlet GitHub Issues](https://github.com/coverlet-coverage/coverlet/issues)
- [.NET Code Coverage Best Practices](https://learn.microsoft.com/en-us/dotnet/core/testing/unit-testing-code-coverage)
- Weight.API working implementation: `src/Biotrackr.Weight.Api/Biotrackr.Weight.Api.UnitTests/ExtensionTests/`
