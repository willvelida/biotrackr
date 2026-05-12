---
description: "Generate an Architecture Decision Record from SDD spec and clarifications"
argument-hint: "[slug=...] [title=...]"
---

# SDD Phase 3a: ADR

Generate an Architecture Decision Record when a feature requires decisions that outlive the feature itself.

## Inputs

* ${input:slug}: (Optional) Slug from prior phases. Inferred from context if omitted.
* ${input:title}: (Required) Title for the ADR. Used in the filename and document heading.

## Step 0: Doctrine Resolution

Before starting, resolve project conventions:

1. Check for project doctrine files:
   * `docs/project-rules/` (constitution.md, rules.md, idioms.md, architecture.md)
   * `copilot-instructions.md`, `AGENTS.md`, `CONTRIBUTING.md`, `README.md`
2. If no doctrine found, scan the codebase for dependency manifests, build systems, and directory patterns.
3. Extract: build command, test command, coverage threshold, naming conventions, CI platform.
4. Unknown values become explicit `[TODO]` markers — never assume silently.

## Step 1: Load Context

1. Read the specification from `.copilot-tracking/plans/{date}/{slug}/{slug}-spec.md`.
2. Read the Clarifications section if it exists.
3. Read any workshop documents in `.copilot-tracking/plans/{date}/{slug}/workshops/` if they exist.
4. Scan existing ADRs in `docs/decision-records/` to check for related prior decisions and to determine the naming pattern.

## Step 2: Research the Decision

1. **Codebase Impact** — identify which modules, services, or patterns are affected by this architectural decision.
2. **Constraint Analysis** — check doctrine boundaries, security controls, and existing architecture patterns that constrain the decision.
3. **Alternatives** — identify at least 2 alternative approaches with trade-offs.

## Step 3: Generate ADR

Create the ADR at `docs/decision-records/{date}-{title-slug}.md` using the project's existing template format:

```markdown
# Decision Record: {Decision Title}

- **Status**: Proposed
- **Deciders**: {User and any referenced stakeholders}
- **Date**: {DD MMMM YYYY}
- **Related Docs**: `.copilot-tracking/plans/{date}/{slug}/{slug}-spec.md`

## Context

{What situation or background information led to this decision? Reference the SDD spec and any workshop findings.}

## Decision

{What was decided and why? Be specific about the chosen approach.}

## Consequences

{What are the outcomes, impacts, or trade-offs of this decision? Include both positive and negative consequences.}

## Alternatives Considered

{What other options were evaluated and why were they not chosen? Include at least 2 alternatives with rejection reasons.}

## Follow-up Actions

{What needs to happen next to implement or revisit this decision?}

## Notes

{Any additional information, references, or discussion points.}
```

## Step 4: Cross-Link

1. If a plan file exists, note the ADR in the plan's Decision Log.
2. Note the ADR reference in the spec's Risks and Assumptions section if it resolves an assumption.

## Step 5: Present for Review

Present the ADR to the user. The Status is `Proposed` until the user explicitly changes it to `Accepted`.

---

> [!IMPORTANT]
> **STOP.** Present the ADR to the user. Do not change the Status from Proposed — the user decides when to accept the decision.