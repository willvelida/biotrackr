---
schedule: every 6h
target-metric: 0.85
---

# Test Coverage

## Goal

Increase unit test line coverage across the Biotrackr repository. Add new test cases targeting untested code paths, edge cases, error handling, and boundary conditions.

Focus on:
- Functions and branches with low or no coverage
- Edge cases in existing test classes (unusual inputs, boundary values, empty/null cases)
- Error paths and exception handling
- Missing test coverage for service handlers, repository methods, and middleware

The metric is `coverage_percent`. **Higher is better.**

The repository is a .NET 10 microservices platform with 14 services. Each service has its own solution file under `src/Biotrackr.{Domain}.{Type}/`. Unit tests use xUnit, FluentAssertions, Moq, and AutoFixture. Follow the existing test naming convention: class `{ClassUnderTest}Should`, method `{Method}_Should{Behavior}_When{Condition}`. Use strict Arrange/Act/Assert with comments.

## Target

Only modify or create files in unit test projects:
- `src/*/Biotrackr.*.UnitTests/**/*.cs`

Do NOT modify:
- Source code (only tests)
- Integration test projects (`*.IntegrationTests/`)
- Configuration files, Dockerfiles, or CI/CD workflows
- Coverage settings (`coverage.runsettings`)

## Evaluation

```bash
#!/bin/bash
set -e

# Find all solution files and run unit tests with coverage collection
TOTAL_COVERED=0
TOTAL_COVERABLE=0

for sln in src/Biotrackr.*/Biotrackr.*.sln src/Biotrackr.*/Biotrackr.*.slnx; do
  [ -f "$sln" ] || continue
  SVC_DIR=$(dirname "$sln")
  
  # Restore and run unit tests with coverage
  dotnet restore "$sln" -v:q 2>/dev/null
  dotnet test "$sln" --no-restore \
    --collect:"XPlat Code Coverage" \
    --settings "$SVC_DIR/coverage.runsettings" \
    --results-directory "$SVC_DIR/TestResults" \
    --filter "FullyQualifiedName!~Contract&FullyQualifiedName!~E2E" \
    -v:q 2>/dev/null || true

  # Parse coverage XML
  for cov in "$SVC_DIR"/TestResults/*/coverage.cobertura.xml; do
    [ -f "$cov" ] || continue
    COVERED=$(grep -oP 'lines-covered="\K[0-9]+' "$cov" | head -1)
    COVERABLE=$(grep -oP 'lines-valid="\K[0-9]+' "$cov" | head -1)
    TOTAL_COVERED=$((TOTAL_COVERED + COVERED))
    TOTAL_COVERABLE=$((TOTAL_COVERABLE + COVERABLE))
  done

  # Clean up results
  rm -rf "$SVC_DIR/TestResults"
done

# Calculate overall percentage
if [ "$TOTAL_COVERABLE" -gt 0 ]; then
  PERCENT=$(python3 -c "print(round($TOTAL_COVERED * 100 / $TOTAL_COVERABLE, 2))")
else
  PERCENT=0
fi

echo "{\"coverage_percent\": $PERCENT, \"covered_lines\": $TOTAL_COVERED, \"coverable_lines\": $TOTAL_COVERABLE}"
```

The metric is `coverage_percent` from the JSON output. **Higher is better.**
