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

### Acceptance Criteria Authoring

Do not combine a strict literal clause with a qualifier clause inside a single acceptance criterion. Reviewers apply strict literal reading; an AC like *"all four checks report PASS, with zero findings attributable to the artifacts modified in this cycle"* creates a dual interpretation (strict-vs-qualified) that blocks an APPROVE verdict whenever the literal clause fails — even when the qualifier-clause test passes. Split such requirements into two separate ACs: one for the strict invariant and one for attribution-scoped satisfaction. Each AC must answer pass/fail without negotiation.

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

### Fix-Loop Scope

When a Phase 5 audit surfaces a scope-adjacent defect the cycle could close cheaply (e.g. one additional file edit with no expansion of cross-cutting work), prefer in-cycle fix-loop absorption over deferral to a separate micro-cycle. Add fix tasks to the existing Phase 5 task table, append `Phase 5 fix-loop end-of-phase verification` to the execution log, and proceed to Phase 6 re-review. Defer to a separate cycle only when the fix expands scope (new dependencies, schema or API surface change, multi-service touches).

Phase 6 Review uses a dedicated judge agent (`sdd-review-judge.agent.md`) pinned to a different model family than Phase 5 implementation. This cross-model pattern reduces self-enhancement bias — the review model has no tendency to rate its own output favorably. If the judge agent is unavailable, the review still functions with the default session model.

## Evolution Log Format

The evolution log at `.copilot-tracking/harness-evolution-log.md` uses a structured table:

| Date | PR | Plan | Proposed | Accepted | Severity (C/H/M/L) | Files Modified | Status | Verdict | FixCycles | FindDensity | CycleTime | SpecClarity | FlowState |

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

## Execution Log Anchors

Each task entry in `execution.log.md` must include an explicit HTML anchor tag before the heading for stable deep-linking from the plan's Notes column:

```text
<a id="task-{ID}"></a>
## Task {ID}: {Task Description}
```

The plan's Notes column links to the anchor using: `execution.log.md#task-{id}`. Do not use `log#task-{id}` or rely on Markdown heading ID derivation — heading IDs vary across renderers.

## Dual-Format Maintenance (Prompts and Skills)

Each SDD phase prompt (`.github/prompts/sdd-{N}-{phase}.prompt.md`) has a corresponding skill mirror (`.github/skills/sdd-{N}-{phase}/SKILL.md`) for GitHub Copilot CLI compatibility. When editing an SDD prompt:

- Update the corresponding `SKILL.md` with the same content changes
- The Inputs section differs: prompts use `${input:variable}` syntax; skills use `**variable**` bold text with plain-text descriptions
- YAML frontmatter differs: prompts use `description` and `argument-hint`, and may also include `agent` when the prompt is intended to route to a specific agent; skills use `name` and `description`
- Prompt `agent` metadata is prompt-only unless the target skill format explicitly supports an equivalent field; do not mirror `agent` into `SKILL.md` frontmatter by default
- Skills may expand the `description` field with "Use when:" context for CLI discoverability — this divergence is intentional
- All other body content (steps, gates, outputs) must remain identical

## New Phase Checklist

When adding a new SDD phase (prompt + skill pair):

- Create both `.github/prompts/sdd-{slug}.prompt.md` and `.github/skills/sdd-{slug}/SKILL.md` simultaneously
- Add a routing entry in `.github/agents/sdd-workflow.agent.md` state detection table
- Update the Prompts table and Skills table in `copilot-instructions.md` with correct counts
- Update the directory structure counts in `AGENTS.md`
- Update the harness guide (`docs/harness-guide.md`) — Mermaid diagram, phase descriptions, quick reference table, artifact chain
- Verify all path references in agent and documentation files point to the correct prompt location (`.github/prompts/`, not stale subdirectory paths)

## Measurement Conventions

### Execution Log Measurement Fields

Each task entry in `execution.log.md` includes a measurement bullet after Verification:

```text
* **Measurement**: Verification: {pass|fail}, Discoveries: {N}
```

### Cycle Measurement Summary (Review Report)

The review report (`reviews/review.md`) includes a `## Cycle Measurement Summary` section as the final section, after `## Next Steps`. It contains:

1. **Cycle Metadata** table: Slug, Complexity (CS-N), Phases, Total Tasks
2. **Metrics (QITE-Aligned)** table with columns: Dimension, Metric, Value, Trend (↑↓→)
3. **Self-Reported** table (mandatory): Spec Clarity (1-5), Flow State (1-5)
4. **Interpretation**: 1-3 sentences explaining what the numbers mean for this cycle

### Evolution Log Measurement Columns

The evolution log extends the base 8 columns with 6 measurement columns:

| Base Columns (existing) | Measurement Columns (new) |
|------------------------|--------------------------|
| Date, PR, Plan, Proposed, Accepted, Severity, Files Modified, Status | Verdict, FixCycles, FindDensity, CycleTime, SpecClarity, FlowState |

- `Verdict`: APPROVE or REQUEST_CHANGES (final review verdict)
- `FixCycles`: Number of REQUEST_CHANGES loops before APPROVE
- `FindDensity`: Findings per task ratio (e.g., `0.5`)
- `CycleTime`: Days from plan directory date to review completion
- `SpecClarity`: Self-reported score (1-5)
- `FlowState`: Self-reported score (1-5)
- Use `—` for rows where measurement data is unavailable (backward compatibility)

### CycleTime Calculation

CycleTime uses the plan directory date (`YYYY-MM-DD` from `.copilot-tracking/plans/{date}/{slug}/`) as the start point and the review completion date as the end point.

### Design Decision Alignment

When a Did You Know or Workshop insight changes the implementation approach after the spec is written, update the affected spec acceptance criteria to match. Spec-implementation drift causes false review findings.

### Artifact Accessibility

`.copilot-tracking/plans/` is gitignored and not available in CI. Agentic workflows and GitHub Actions must read only committed artifacts (e.g., `.copilot-tracking/harness-evolution-log.md`, not plan-directory files).
