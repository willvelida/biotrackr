---
description: "SDD workflow conventions for plan artifacts, spec documents, and tracking files. Use when: creating or editing SDD plans, specs, reviews, or evolution logs."
applyTo: "**/.copilot-tracking/plans/**/*.md,**/.copilot-tracking/harness-evolution-log.md"
---

# SDD Workflow Conventions

## Artifact Directory Structure

Each SDD cycle produces artifacts under `.copilot-tracking/plans/{date}/{slug}/`:

```text
.copilot-tracking/plans/{YYYY-MM-DD}/{slug}/
├── research-dossier.md          # Phase 1: Explore output
├── {slug}-spec.md               # Phase 2: Specify output
├── {slug}-plan.md               # Phase 4: Architect output
├── execution.log.md             # Phase 5: Progress log
└── reviews/
    └── review.md                # Phase 6: Review output
```

## Slug Naming

- Use lowercase kebab-case: `mcp-server-redesign`, `activity-weekly-summary`
- Match the feature or change being developed
- Consistent across all artifacts in the same SDD cycle

## Spec Document Sections

Specs (`{slug}-spec.md`) use this canonical section order. Downstream phases depend on these section names.

1. Summary
2. Goals
3. Non-Goals
4. Acceptance Criteria (numbered, testable)
5. Complexity Score (CS-1 through CS-5)
6. Risks & Assumptions
7. Open Questions
8. Affected Modules/Services
9. Testing Strategy (populated during Clarify phase)
10. Workshop Opportunities
11. Clarifications (appended by Phase 3: Clarify — optional, not present until Clarify runs)

Mark unknowns with `[NEEDS CLARIFICATION]`. Do not include technology or framework choices in specs.

## Task Table Format

Plans and task files use a 6-column table with 4-state checkboxes:

| Status | ID   | Task              | Path(s)         | Done When             | Notes |
|--------|------|-------------------|-----------------|-----------------------|-------|
| [ ]    | T001 | Task description  | /path/to/file   | Success criteria      |       |

Status values:
- `[ ]` pending
- `[~]` in-progress
- `[x]` completed
- `[!]` blocked

Update task status after each task, not at the end of a phase.

## Review Verdicts

Reviews produce one of two verdicts:
- **APPROVE** when no CRITICAL or HIGH findings exist
- **REQUEST_CHANGES** when CRITICAL or HIGH findings exist (loops back to Implement)

Doctrine Evolution findings in the review are advisory and do not affect the verdict.

## Evolution Log Format

The evolution log at `.copilot-tracking/harness-evolution-log.md` uses a structured table:

| Date | PR | Plan | Proposed | Accepted | Severity (C/H/M/L) | Files Modified | Status |

- Status values: `complete`, `partial`, `skipped`
- Check for prior entries by PR number before adding new rows
- Each row represents one harness-evolve execution

## Discovery Categories

Discoveries logged during implementation use typed categories:
- `gotcha` — unexpected behavior or edge case
- `research-needed` — topic requiring further investigation
- `unexpected-behavior` — code behaving differently than expected
- `workaround` — temporary fix for a known issue
- `decision` — design or implementation choice made
- `debt` — technical debt identified for future resolution
- `insight` — pattern or convention worth remembering
