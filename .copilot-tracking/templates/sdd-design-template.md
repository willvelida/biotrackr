<!-- markdownlint-disable-file -->
# SDD Plan: {Feature Name}

## Purpose / Big Picture

{What user-visible behavior this enables. A novice should understand end-to-end.}

## Complexity Score

{CS-1 through CS-5. See project doctrine for the complexity scoring rubric.

| Factor | Score (0-2) | Rationale |
|--------|-------------|-----------|
| Surface Area (S) | | |
| Integration (I) | | |
| Data/State (D) | | |
| Novelty (N) | | |
| Non-Functional (F) | | |
| Testing (T) | | |
| **Total** | | **CS-{N}** |

## Goals

* {Goal 1 — user-visible outcome}
* {Goal 2}

## Non-Goals

* {Explicitly out of scope item 1}
* {Explicitly out of scope item 2}

## Acceptance Criteria

1. {Testable criterion with observable outcome}
2. {Testable criterion with observable outcome}
3. {Testable criterion with observable outcome}

## Progress

<!-- Task status legend: [ ] pending, [~] in-progress, [x] completed, [!] blocked -->

- [ ] {Phase 1 name} — {timestamp}
- [ ] {Phase 2 name}

## Current State

{1-2 paragraph briefing for the next session. THE key bridging mechanism.
Describe: what's done, what's next, any blockers, and the current build/test status.}

## Phases

### Phase 1: {Phase Name}

| Status | ID   | Task              | Path(s)         | Done When             | Notes |
|--------|------|-------------------|-----------------|-----------------------|-------|
| [ ]    | T001 | {Task description} | {/path/to/file} | {Success criteria}    |       |
| [ ]    | T002 | {Task description} | {/path/to/file} | {Success criteria}    |       |

### Phase 2: {Phase Name}

| Status | ID   | Task              | Path(s)         | Done When             | Notes |
|--------|------|-------------------|-----------------|-----------------------|-------|
| [ ]    | T003 | {Task description} | {/path/to/file} | {Success criteria}    |       |

## Testing Approach

{Standard / Lightweight / None — as determined during Clarify phase.
Reference the project's testing conventions per doctrine.}

## Architecture Decisions

| Decision | Rationale | Alternatives Considered |
|----------|-----------|------------------------|
| | | |

## Cross-Cutting Concerns

{Shared patterns, migration steps, configuration changes, or infrastructure updates that affect multiple phases.}

## Discovery Findings

{Minimum 10 findings from the Architect phase. Each with evidence.}

### Discovery D-01: {Title}

* **Category**: {Pattern | Integration | Convention | Constraint}
* **Impact**: {Critical | High | Medium | Low}
* **Evidence**: {file:line reference}
* **Description**: {What was found}
* **Why It Matters**: {Impact on implementation}
* **Action Required**: {What the implementation must do}

## Decision Log

| Date | Decision | Rationale |
|------|----------|-----------|
| | | |

## Surprises & Discoveries

{Unexpected behaviors, bugs, or insights with evidence. Populated during implementation.}

## Validation

{Exact commands to run per project doctrine. Do not hardcode tool-specific commands.
Reference the project's build command, test command, and coverage threshold from doctrine.}

## Affected Modules/Services

{Which modules, services, or packages are touched. List by name from the project's architecture documentation.}

## Files Modified

{Track all files touched during execution — enables diff review and rollback assessment.}
