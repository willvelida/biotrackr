---
description: "Quality gate review with structured findings and verdict"
argument-hint: "[slug=...] [plan=...] [phase=1]"
---

# SDD Phase 6: Review

> [!CAUTION]
> Do NOT modify any source files during this phase. This is a READ-ONLY review command.

## Inputs

* ${input:slug}: (Optional) Slug from prior phases. Inferred from the most recent SDD plan directory if omitted.
* ${input:plan}: (Optional) Path to the plan file.
* ${input:phase}: (Optional, default 1) Phase number to review.

## Step 0: Doctrine Resolution

Before starting, resolve project conventions:

1. Check for project doctrine files:
   * `docs/project-rules/` (constitution.md, rules.md, idioms.md, architecture.md)
   * `copilot-instructions.md`, `AGENTS.md`, `CONTRIBUTING.md`, `README.md`
2. If no doctrine found, scan the codebase:
   * Dependency manifests (package.json, *.csproj, Cargo.toml, pyproject.toml, go.mod)
   * Build system (Makefile, Justfile, Taskfile.yml, scripts/)
   * Test framework (test directories, test configuration files)
   * Directory patterns and naming conventions
3. Extract and document:
   * Build command, test command, coverage threshold (if any)
   * Naming conventions, module/service topology, CI platform
4. Unknown values become explicit `[TODO]` markers — never assume silently.

## Step 1: Load Review Context

1. Read the plan file, specification, and execution log for the specified phase.
2. Identify all files modified during the phase from the execution log's "Changes Made" entries.
3. Read the acceptance criteria relevant to this phase from the specification.

## Step 2: Scope Guard

Review only the specified phase's changes. Do not expand scope to the entire codebase.

1. Build the list of affected files from the execution log.
2. If the execution log is missing or incomplete, fall back to the plan's task table Path(s) column.
3. Ignore files outside this scope unless a cross-module consistency issue surfaces.

## Step 3: Run Review Checks

### 3a: Spec Compliance

Verify each acceptance criterion relevant to this phase is satisfied by the implemented changes. Map acceptance criteria to specific code changes.

### 3b: Convention Adherence

Check that changes follow project conventions per doctrine:

* Naming conventions (variables, files, classes, methods).
* Code organization patterns (module structure, file placement).
* Error handling patterns.
* Documentation requirements.

If a code review agent exists in `.github/agents/` (search for agents with "review" in their name or description), delegate convention checks to it. Do not hardcode agent names.

### 3c: Test Coverage

Verify test coverage meets the doctrine threshold:

* New code paths have corresponding tests.
* Coverage meets or exceeds the threshold defined in doctrine.
* Test naming follows project conventions.

### 3d: Cross-Module Consistency

When changes span multiple modules or services:

* Shared contracts and interfaces remain consistent.
* Naming and patterns are uniform across touched modules.
* No conflicting approaches introduced between modules.

### 3e: Security Awareness

Flag obvious security concerns (advisory, does not block verdict by itself unless severity is CRITICAL):

* Hardcoded credentials or secrets.
* Unvalidated inputs at system boundaries.
* Missing authentication or authorization checks.
* Unsafe deserialization or injection vectors.

## Step 4: Classify Findings

Assign severity to each finding:

| Severity | Definition | Verdict Impact |
|----------|------------|----------------|
| CRITICAL | Blocks deployment or introduces a security vulnerability | Blocks APPROVE |
| HIGH | Violates acceptance criteria or breaks established patterns | Blocks APPROVE |
| MEDIUM | Convention deviation or minor inconsistency | Does not block |
| LOW | Stylistic preference or minor improvement opportunity | Does not block |

## Step 5: Advisory Doctrine Evolution Analysis

Identify candidates for encoding into the project's instruction files or documentation. This section is explicitly advisory and does not affect the verdict.

For each candidate, note:

* The learning (convention violation, new pattern, gotcha).
* The target file where it could be encoded (instruction file, copilot-instructions, ADR).
* The rationale for encoding.

Label this section clearly: "Advisory: these findings inform future harness evolution but do not affect the review verdict."

## Step 6: Issue Verdict

* **APPROVE**: No CRITICAL or HIGH findings. The phase meets acceptance criteria and follows project conventions.
* **REQUEST_CHANGES**: One or more CRITICAL or HIGH findings exist. List specific fix tasks for the next implementation run.

## Step 7: Write Review Report

Write the report to `.copilot-tracking/plans/{date}/{slug}/reviews/review.md` with these sections:

```markdown
# Review: Phase {N}

## Verdict

{APPROVE | REQUEST_CHANGES}

## Summary

{Overview of what was reviewed and key observations, 10 lines maximum.}

## Findings Table

| # | Severity | Category | File | Finding | Recommendation |
|---|----------|----------|------|---------|----------------|
| 1 | {CRITICAL|HIGH|MEDIUM|LOW} | {spec|convention|coverage|consistency|security} | {path} | {Description} | {Fix action} |

## Detailed Findings

### Spec Compliance

{Assessment of acceptance criteria coverage with evidence.}

### Convention Adherence

{Assessment of convention compliance per doctrine.}

### Test Coverage

{Assessment of test coverage against doctrine threshold.}

### Cross-Module Consistency

{Assessment of consistency across affected modules. "N/A" for single-module phases.}

### Security Awareness

{Advisory security observations. "No concerns identified" when clean.}

## Doctrine Evolution Candidates (Advisory)

{Learnings that could be encoded into instruction files. Explicitly non-blocking.}

| # | Learning | Target File | Rationale |
|---|----------|-------------|-----------|
| 1 | {Description} | {instruction file path} | {Why encode this} |

## Next Steps

{If APPROVE: recommended next phase or completion actions.}
{If REQUEST_CHANGES: specific fix tasks with file paths and descriptions for the next sdd-5-implement run.}
```

---

> [!IMPORTANT]
> **STOP.** Present the verdict and review report to the user. If REQUEST_CHANGES, wait for user confirmation before the next implementation cycle.
