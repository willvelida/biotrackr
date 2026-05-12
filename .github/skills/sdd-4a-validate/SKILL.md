---
name: sdd-4a-validate
description: "Validate plan readiness before implementation with parallel quality gates. Use when: a plan has been created by the Architect phase and you want to verify completeness, doctrine compliance, and dependency ordering before starting implementation."
---

# SDD Phase 4a: Validate Plan

> [!CAUTION]
> Do NOT modify any source files during this phase. This is a READ-ONLY validation command.

Validate that a plan is ready for implementation by running parallel quality gates.

## Inputs

* **slug** (Optional): Slug from prior phases. Inferred from context if omitted.
* **plan** (Optional): Path to the plan file from Phase 4 (Architect).

## Step 0: Doctrine Resolution

Before starting, resolve project conventions:

1. Check for project doctrine files:
   * `docs/project-rules/` (constitution.md, rules.md, idioms.md, architecture.md)
   * `copilot-instructions.md`, `AGENTS.md`, `CONTRIBUTING.md`, `README.md`
2. If no doctrine found, scan the codebase for dependency manifests, build systems, and directory patterns.
3. Extract: build command, test command, coverage threshold, naming conventions, CI platform.
4. Unknown values become explicit `[TODO]` markers — never assume silently.

## Step 1: Load Plan Context

1. Read the plan file from `{plan}` or `.copilot-tracking/plans/{date}/{slug}/{slug}-plan.md`.
2. Read the specification for cross-reference.
3. Read the research dossier if it exists.
4. Read any workshop documents or ADRs created during this cycle.

## Step 2: Launch Parallel Validators

Run these validators concurrently:

1. **Structure Validator** — verify the plan has all required sections: Purpose, Complexity Score, Goals, Non-Goals, Acceptance Criteria, Progress, Phases with task tables, Validation.
2. **Completeness Validator** — verify every acceptance criterion from the spec maps to at least one task in the plan. Flag unmapped criteria.
3. **Doctrine Validator** — verify the plan respects doctrine boundaries (naming conventions, module topology, service lifetimes, API patterns).
4. **Dependency Validator** — verify task dependencies are acyclic and phase ordering is logical. Flag tasks that reference files in later phases.

Each validator produces findings with severity (CRITICAL, HIGH, MEDIUM, LOW).

## Step 3: Synthesize Verdict

Aggregate all findings and issue a verdict:

* **READY** — zero CRITICAL or HIGH findings. Proceed to implementation.
* **NOT READY** — one or more CRITICAL or HIGH findings. List specific fixes needed.

Present the findings table:

```markdown
## Validation Report

### Verdict: {READY | NOT READY}

| # | Severity | Validator | Finding | Fix Required |
|---|----------|-----------|---------|--------------|
| 1 | {severity} | {validator name} | {description} | {specific fix} |
```

## Step 4: Present Verdict

Present the verdict and findings to the user. If NOT READY, the user can:
1. Fix the issues and re-run validation.
2. Override with explicit acknowledgment (proceed despite issues).

---

> [!IMPORTANT]
> **STOP.** Present the validation verdict. Do not proceed to implementation. The user decides whether to fix issues, override, or proceed.