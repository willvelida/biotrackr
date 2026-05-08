---
schedule: weekly
target-metric: 85
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

### Baseline Caching

After each evaluation, update the `## 📦 Coverage Baselines` section in your state file with per-service coverage data from the evaluation output. For each service with `"source": "tested"` in the `per_service` JSON:
- Update the `Covered`, `Coverable`, and `Pct` columns with the fresh values
- Set `Commit` to the `commit` value from the JSON (short SHA)
- Set `Updated` to today's date (YYYY-MM-DD format)

Do NOT update baselines for services with `"source": "cached"` — their values are already correct.

If the `## 📦 Coverage Baselines` section does not exist in your state file, create it with a markdown table containing all 14 services from the evaluation output.

The baseline table format:

| Service | Covered | Coverable | Pct | Commit | Updated |
|---------|---------|-----------|-----|--------|---------|
| Activity.Api | 1234 | 1500 | 82.27 | a1b2c3d | 2026-05-07 |

Update baselines on both accepted AND rejected iterations — the per-service data reflects the pre-change state and is valid regardless of whether the aggregate metric improved.

The repository is a .NET 10 microservices platform with 14 services. Each service has its own solution file under `src/Biotrackr.{Domain}.{Type}/`. Unit tests use xUnit, FluentAssertions, Moq, and AutoFixture. Follow these conventions strictly:

### Test Naming

- **Class:** `{ClassUnderTest}Should` (e.g., `ActivityHandlersShould`)
- **Method:** `{Method}_Should{Behavior}_When{Condition}` (e.g., `GetActivityByDate_ShouldReturnOk_WhenActivityIsFound`)
- Never use short names like `ShouldSerializeAndDeserialize()` — always include the subject and condition.

### Using Directive Ordering

- `System.*` namespaces must come first, followed by third-party libraries (e.g., `AutoFixture`, `FluentAssertions`, `Moq`), then project namespaces.
- Example:
  ```csharp
  using System.Text.Json;
  using AutoFixture;
  using FluentAssertions;
  using Biotrackr.Mcp.Server.Models.Activity;
  ```

### Code Style

- Use strict Arrange/Act/Assert with `// Arrange`, `// Act`, `// Assert` comments.
- Use file-scoped namespaces (e.g., `namespace Biotrackr.Mcp.Server.UnitTests.Models;`).

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

STATE_FILE="/tmp/gh-aw/repo-memory/autoloop/test_coverage.md"
TOTAL_COVERED=0
TOTAL_COVERABLE=0
FAILED_SERVICES=0
TESTED_SERVICES=0
CACHED_SERVICES=0
PER_SERVICE=""

# Parse a single service's baseline from the state file
# Returns: covered|coverable|commit (pipe-delimited)
parse_baseline() {
  local svc="$1"
  if [ -f "$STATE_FILE" ]; then
    # Match the service name in the baselines table
    local row
    row=$(grep "| ${svc} " "$STATE_FILE" 2>/dev/null | head -1)
    if [ -n "$row" ]; then
      local covered coverable commit
      covered=$(echo "$row" | awk -F'|' '{print $3}' | tr -d ' ')
      coverable=$(echo "$row" | awk -F'|' '{print $4}' | tr -d ' ')
      commit=$(echo "$row" | awk -F'|' '{print $6}' | tr -d ' ')
      echo "${covered}|${coverable}|${commit}"
      return
    fi
  fi
  echo ""
}

for svc_dir in src/Biotrackr.*/; do
  [ -d "$svc_dir" ] || continue
  SVC_NAME=$(basename "$svc_dir" | sed 's/Biotrackr\.//')

  # Find solution file (.sln or .slnx)
  sln=""
  for f in "$svc_dir"/*.sln "$svc_dir"/*.slnx; do
    [ -f "$f" ] && sln="$f" && break
  done
  [ -n "$sln" ] || continue

  # Check for cached baseline
  BASELINE=$(parse_baseline "$SVC_NAME")
  NEED_TEST=true

  if [ -n "$BASELINE" ]; then
    BASELINE_COMMIT=$(echo "$BASELINE" | cut -d'|' -f3)
    if [ -n "$BASELINE_COMMIT" ] && git diff --quiet "$BASELINE_COMMIT" HEAD -- "$svc_dir" 2>/dev/null; then
      # Unchanged since baseline — use cached values
      CACHED_COVERED=$(echo "$BASELINE" | cut -d'|' -f1)
      CACHED_COVERABLE=$(echo "$BASELINE" | cut -d'|' -f2)
      if [ -n "$CACHED_COVERED" ] && [ -n "$CACHED_COVERABLE" ] && \
         [ "$CACHED_COVERED" -ge 0 ] 2>/dev/null && [ "$CACHED_COVERABLE" -gt 0 ] 2>/dev/null; then
        TOTAL_COVERED=$((TOTAL_COVERED + CACHED_COVERED))
        TOTAL_COVERABLE=$((TOTAL_COVERABLE + CACHED_COVERABLE))
        CACHED_SERVICES=$((CACHED_SERVICES + 1))
        CACHED_PCT=$(python3 -c "print(round($CACHED_COVERED * 100 / $CACHED_COVERABLE, 2))")
        PER_SERVICE="${PER_SERVICE}\"${SVC_NAME}\": {\"covered\": ${CACHED_COVERED}, \"coverable\": ${CACHED_COVERABLE}, \"pct\": ${CACHED_PCT}, \"source\": \"cached\"}, "
        NEED_TEST=false
      fi
    fi
  fi

  if [ "$NEED_TEST" = true ]; then
    # Determine coverage settings path
    SETTINGS_FLAG=""
    if [ -f "$svc_dir/coverage.runsettings" ]; then
      SETTINGS_FLAG="--settings $svc_dir/coverage.runsettings"
    fi

    # Restore and run unit tests with coverage
    dotnet restore "$sln" -v:q 2>/dev/null
    if ! dotnet test "$sln" --no-restore \
      --collect:"XPlat Code Coverage" \
      $SETTINGS_FLAG \
      --results-directory "$svc_dir/TestResults" \
      --filter "FullyQualifiedName!~Contract&FullyQualifiedName!~E2E" \
      -v:q 2>/dev/null; then
      FAILED_SERVICES=$((FAILED_SERVICES + 1))
    fi

    # Parse coverage XML
    SVC_COVERED=0
    SVC_COVERABLE=0
    for cov in "$svc_dir"/TestResults/*/coverage.cobertura.xml; do
      [ -f "$cov" ] || continue
      COVERED=$(grep -oP 'lines-covered="\K[0-9]+' "$cov" | head -1)
      COVERABLE=$(grep -oP 'lines-valid="\K[0-9]+' "$cov" | head -1)
      SVC_COVERED=$((SVC_COVERED + ${COVERED:-0}))
      SVC_COVERABLE=$((SVC_COVERABLE + ${COVERABLE:-0}))
    done

    TOTAL_COVERED=$((TOTAL_COVERED + SVC_COVERED))
    TOTAL_COVERABLE=$((TOTAL_COVERABLE + SVC_COVERABLE))
    TESTED_SERVICES=$((TESTED_SERVICES + 1))

    SVC_PCT=0
    if [ "$SVC_COVERABLE" -gt 0 ]; then
      SVC_PCT=$(python3 -c "print(round($SVC_COVERED * 100 / $SVC_COVERABLE, 2))")
    fi
    SVC_COMMIT=$(git rev-parse --short HEAD)
    PER_SERVICE="${PER_SERVICE}\"${SVC_NAME}\": {\"covered\": ${SVC_COVERED}, \"coverable\": ${SVC_COVERABLE}, \"pct\": ${SVC_PCT}, \"commit\": \"${SVC_COMMIT}\", \"source\": \"tested\"}, "

    # Clean up results
    rm -rf "$svc_dir/TestResults"
  fi
done

# Calculate overall percentage
if [ "$TOTAL_COVERABLE" -gt 0 ]; then
  PERCENT=$(python3 -c "print(round($TOTAL_COVERED * 100 / $TOTAL_COVERABLE, 2))")
else
  PERCENT=0
fi

# Remove trailing comma+space from per-service JSON
PER_SERVICE=$(echo "$PER_SERVICE" | sed 's/, $//')

echo "{\"coverage_percent\": $PERCENT, \"covered_lines\": $TOTAL_COVERED, \"coverable_lines\": $TOTAL_COVERABLE, \"failed_services\": $FAILED_SERVICES, \"tested_services\": $TESTED_SERVICES, \"cached_services\": $CACHED_SERVICES, \"per_service\": {${PER_SERVICE}}}"

# Fail evaluation if any service had test failures
if [ "$FAILED_SERVICES" -gt 0 ]; then
  exit 1
fi
```

The metric is `coverage_percent` from the JSON output. **Higher is better.**
