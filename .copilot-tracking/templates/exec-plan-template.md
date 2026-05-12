<!-- markdownlint-disable-file -->
# ExecPlan: {Feature Name}

## Purpose / Big Picture

{What user-visible behavior this enables. A novice should understand end-to-end.}

## Complexity Score

{CS-1 through CS-5. See docs/standards/harness-governance.md for rubric.}

## Progress

- [ ] {Step 1} — {timestamp when started/completed}
- [ ] {Step 2}
- [ ] {Step 3}

## Current State

{1-2 paragraph briefing for the next session. THE key bridging mechanism.
Describe: what's done, what's next, any blockers, and the current build/test status.}

## Decision Log

| Date | Decision | Rationale |
|------|----------|-----------|
| | | |

## Surprises & Discoveries

{Unexpected behaviors, bugs, or insights with evidence. Include error messages, stack traces, or unexpected test results.}

## Validation

{Exact commands to run and expected output.}

```text
cd src/Biotrackr.{Domain}.{Type}
dotnet build --no-restore -v:q
dotnet test --no-build --collect:"XPlat Code Coverage" --settings ../coverage.runsettings --results-directory ./TestResults
```

## Services Affected

{Which of the 14 Biotrackr services are touched. List service names from the architecture table.}

## Files Modified

{Track all files touched during execution — enables diff review and rollback assessment.}
