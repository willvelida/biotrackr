---
description: "SDD workflow conventions for plan artifacts, spec documents, and tracking files. Use when: creating or editing SDD plans, specs, reviews, or evolution logs."
applyTo: "**/.copilot-tracking/plans/**/*.md,**/.copilot-tracking/harness-evolution-log.md"
---

# SDD Workflow Conventions

## Artifact Directory Structure

Each SDD cycle produces artifacts under `.copilot-tracking/plans/{YYYY-MM-DD}/{slug}/`:

```text
research-dossier.md      # Phase 1: Explore output
{slug}-spec.md           # Phase 2: Specify output
{slug}-plan.md           # Phase 4: Architect output
execution.log.md         # Phase 5: Progress log
reviews/review.md        # Phase 6: Review output
```

Slugs are lowercase kebab-case, match the feature being developed, and stay identical across every artifact in the cycle.

`.copilot-tracking/plans/` is gitignored and unavailable in CI. Agentic workflows and GitHub Actions read only committed artifacts such as `.copilot-tracking/harness-evolution-log.md`.

## Phase Definition Format

Each SDD phase is defined once, as an Agent Skill at `.github/skills/sdd-{N}-{phase}/SKILL.md`. Do not create a parallel prompt file. Skills are the only phase-definition format that reaches every surface this repo targets: GitHub Copilot CLI supports skills and does not support prompt files, VS Code Copilot Chat surfaces skills as `/` slash commands, and VS Code Agent Host sessions ignore prompt files entirely.

The single exception is `.github/prompts/sdd-6-review.prompt.md`, an 11-line shim that exists only to declare `agent: "SDD Review Judge"`, which skill frontmatter cannot express. It carries no phase content and must not accumulate any.

Skill authoring rules:

- `name` must exactly match the parent directory name, or the skill loads as nothing with no error.
- `description` states what the phase does and appends a `Use when:` clause. Discovery is description-matched, so the clause is load-bearing.
- `argument-hint` carries the input signature, for example `"[slug=...] [spec=...]"`.
- Declare inputs in the body as `**name** (Required|Optional): description`. The `${input:x}` syntax belongs to prompt files only.
- Route to external references through a `## When to Read References` block, one line per reference prefixed by the problem it solves. Do not inline reference content.

Phase-to-phase routing lives in `.github/agents/sdd-workflow.agent.md` and emits `/sdd-{N}-{phase}`, which resolves to the skill directory name in both VS Code and Copilot CLI.

## Spec Document Sections

Specs use this canonical section order; downstream phases depend on the names. Summary, Goals, Non-Goals, Acceptance Criteria (numbered and testable), Complexity Score (CS-1 through CS-5), Risks & Assumptions, Open Questions, Affected Modules/Services, Testing Strategy (populated during Clarify), Workshop Opportunities, Clarifications (appended by Phase 3, absent until then).

Mark unknowns with `[NEEDS CLARIFICATION]`. Do not name technologies or frameworks in specs.

Do not combine a strict literal clause with a qualifier clause inside one acceptance criterion. Reviewers read strictly, so an AC like *"all four checks report PASS, with zero findings attributable to this cycle"* blocks APPROVE whenever the literal clause fails even though the qualified clause passes. Split it into one AC for the strict invariant and one for attribution-scoped satisfaction.

## Task Table Format

Plans and task files use a 6-column table with 4-state checkboxes:

| Status | ID   | Task              | Path(s)         | Done When             | Notes |
|--------|------|-------------------|-----------------|-----------------------|-------|
| [ ]    | T001 | Task description  | /path/to/file   | Success criteria      |       |

Status values are `[ ]` pending, `[~]` in-progress, `[x]` completed, `[!]` blocked. Update status after each task, not at the end of a phase.

## Execution Log Format

Each task entry needs an explicit HTML anchor before its heading so the plan's Notes column can deep-link to `execution.log.md#task-{id}`. Heading-ID derivation varies across renderers and must not be relied on.

```text
<a id="task-{ID}"></a>
## Task {ID}: {Task Description}
```

Each entry carries a measurement bullet after Verification: `* **Measurement**: Verification: {pass|fail}, Discoveries: {N}`.

Discoveries use typed categories: `gotcha`, `research-needed`, `unexpected-behavior`, `workaround`, `decision`, `debt`, `insight`.

## Review Verdicts

Reviews return APPROVE when no CRITICAL or HIGH findings exist, and REQUEST_CHANGES otherwise, which loops back to Implement. Doctrine Evolution findings are advisory and do not affect the verdict.

When a Phase 5 audit surfaces a scope-adjacent defect the cycle can close cheaply, prefer in-cycle fix-loop absorption over a separate micro-cycle: add fix tasks to the existing Phase 5 table, append `Phase 5 fix-loop end-of-phase verification` to the execution log, then re-review. Defer only when the fix expands scope through new dependencies, schema or API surface changes, or multi-service touches.

Phase 6 runs under `sdd-review-judge.agent.md`, pinned to a different model family than Phase 5 implementation to reduce self-enhancement bias. The review still functions on the default session model if that agent is unavailable.

The review report ends with a `## Cycle Measurement Summary` section after `## Next Steps`, containing a Cycle Metadata table, a QITE-aligned Metrics table with trend arrows, a mandatory Self-Reported table (Spec Clarity and Flow State, 1-5), and a short interpretation. See `docs/standards/harness-governance.md` for metric definitions.

## Evolution Log Format

`.copilot-tracking/harness-evolution-log.md` uses a 14-column table: Date, PR, Plan, Proposed, Accepted, Severity (C/H/M/L), Files Modified, Status, Verdict, FixCycles, FindDensity, CycleTime, SpecClarity, FlowState.

Status is `complete`, `partial`, or `skipped`. FixCycles counts REQUEST_CHANGES loops before APPROVE. FindDensity is findings per task. CycleTime runs from the plan directory date to review completion. Use `—` where data is unavailable. Check for an existing row by PR number before adding one.

## New Phase Checklist

- Create `.github/skills/sdd-{slug}/SKILL.md` with `name` matching the directory
- Add a routing entry to the `.github/agents/sdd-workflow.agent.md` state detection table
- Update the Skills table and count in `.github/copilot-instructions.md`
- Update the counts in `AGENTS.md` and the diagram, phase descriptions, and quick reference in `docs/harness-guide.md`

## Design Decision Alignment

When a Did You Know or Workshop insight changes the approach after the spec is written, update the affected acceptance criteria to match. Spec-implementation drift causes false review findings.
