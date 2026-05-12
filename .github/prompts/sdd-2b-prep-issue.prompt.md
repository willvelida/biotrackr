---
description: "Generate structured GitHub Issue text from SDD spec and plan artifacts"
argument-hint: "[slug=...]"
---

# SDD Phase 2b: Prep Issue

Generate structured issue text from a completed specification for external tracking in GitHub Issues.

## Inputs

* ${input:slug}: (Optional) Slug from prior phases. Inferred from the most recent SDD plan directory if omitted.

## Step 0: Doctrine Resolution

Before starting, resolve project conventions:

1. Check for project doctrine files:
   * `docs/project-rules/` (constitution.md, rules.md, idioms.md, architecture.md)
   * `copilot-instructions.md`, `AGENTS.md`, `CONTRIBUTING.md`, `README.md`
2. If no doctrine found, scan the codebase for dependency manifests, build systems, and directory patterns.
3. Extract: build command, test command, coverage threshold, naming conventions, CI platform.
4. Unknown values become explicit `[TODO]` markers — never assume silently.

## Step 1: Load Spec Context

1. Read the specification from `.copilot-tracking/plans/{date}/{slug}/{slug}-spec.md`.
2. If clarifications exist, read the Clarifications section.
3. If a plan exists, read the plan's complexity score and phase count.

## Step 2: Generate Issue Text

Produce structured issue text with these sections:

### Title

Format: `feat({scope}): {summary from spec}`

Where `{scope}` is the primary affected module or service in kebab-case.

### Body

```markdown
## Summary

{One-paragraph summary from spec}

## Goals

{Bulleted goals from spec}

## Non-Goals

{Bulleted non-goals from spec}

## Acceptance Criteria

{Numbered acceptance criteria from spec}

## Complexity

{CS-N score with brief rationale}

## Affected Services

{List from spec's Affected Modules/Services}

## Links

- Spec: `.copilot-tracking/plans/{date}/{slug}/{slug}-spec.md`
- Plan: `.copilot-tracking/plans/{date}/{slug}/{slug}-plan.md` (if exists)
```

### Labels

Suggest GitHub labels based on the spec content:
- Type label: `enhancement`, `bug`, `infrastructure`, `documentation`
- Complexity label: `complexity:cs-{N}`
- Affected service labels from the spec

## Step 3: Output

Present the generated issue text in a fenced code block ready for copy-paste into GitHub Issues. Do not create the issue automatically — present the text for user review.

---

> [!IMPORTANT]
> **STOP.** Present the issue text to the user. Do not create the GitHub Issue — the user handles issue creation.