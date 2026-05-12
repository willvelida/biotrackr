---
description: "Encode learnings from completed SDD cycles into the agent harness"
argument-hint: "[slug=...] [plan=...]"
---

# SDD Phase 7: Evolve

> [!CAUTION]
> **Safety Prohibitions:**
>
> * NEVER modify harness files without explicit user approval for EACH change.
> * NEVER modify immutable doctrine items (NEVER rules, security controls, boundary rules).
> * STOP if a target file would exceed its size budget (200 lines for `.instructions.md`, 500 lines for `copilot-instructions.md`).
> * Use external signals (code review feedback, test failures, rework evidence) as evidence, not solely self-assessment.
> * Separate harness commits from code commits. Harness changes use: `core(harness): encode learnings from {slug}`.

## Inputs

* ${input:slug}: (Optional) Short kebab-case identifier from prior phases. Inferred from the most recent SDD plan directory if omitted.
* ${input:plan}: (Optional) Path to the plan file from Phase 4. Auto-detected from `.copilot-tracking/plans/` if omitted.

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

## Step 1: Locate Plan Artifacts

1. If `slug` and `plan` are not provided, scan `.copilot-tracking/plans/` for the most recent SDD plan directory.
2. Confirm the plan directory contains completed artifacts (plan file, execution log, review report).
3. If no completed plan is found, STOP and inform the user.

## Phase A: Extract

Read the completed SDD plan artifacts and gather learning candidates:

1. **Decision log** — Read the plan file's Discoveries & Surprises section and Decision Log entries for rationale and trade-offs.
2. **Execution log** — Read `execution.log.md` for Implementation Notes, unexpected behaviors, and workarounds.
3. **Review report** — Read `review.md` for findings, convention violations, and the Doctrine Evolution section (Section E.4) if present.
4. **PR feedback** — Check for PR review comments and code review threads that surfaced corrections or patterns.

For each candidate learning, record:

* Title (concise description of the learning)
* Evidence (file path, line number, or artifact reference)
* Source (which artifact surfaced it: decision log, execution log, review, PR feedback)

## Phase B: Classify

For each extracted learning:

1. **Categorize** into one of:
   * Convention — naming, formatting, structural pattern
   * Anti-pattern — something to avoid (adds to NEVER or Patterns to Avoid sections)
   * Gotcha — non-obvious behavior or surprise that catches developers
   * Architecture — module boundaries, data flow, integration patterns
   * Testing — test structure, coverage, fixture patterns
   * Security — authentication, authorization, input validation, OWASP controls

2. **Assign severity:**
   * CRITICAL — caused build failure, test failure, security issue, or data loss
   * HIGH — caused rework, multiple fix cycles, or convention violation found in review
   * MEDIUM — improved efficiency or prevented potential future issues
   * LOW — documentation improvement, nice-to-have clarity

3. **Map to target file(s)** using this discovery-to-file logic:
   * C# coding patterns → `csharp-conventions.instructions.md`
   * Test patterns → `testing-conventions.instructions.md`
   * Cosmos DB patterns → `cosmos-conventions.instructions.md`
   * Bicep/IaC patterns → `bicep-conventions.instructions.md`
   * CSS/Blazor patterns → `css-conventions.instructions.md` or `razor-components.instructions.md`
   * GitHub Actions patterns → `github-actions-conventions.instructions.md`
   * DSA insights → `dsa-awareness.instructions.md`
   * Architecture, security, boundary rules → `copilot-instructions.md`
   * Build/test commands → `copilot-instructions.md` and `AGENTS.md`
   * Cross-cutting concerns → most relevant `.instructions.md` file

4. **De-duplicate** against existing content in the target file. If the learning is already documented, skip it.

5. **Size budget check** — count lines in each target file. If adding the learning would exceed the budget (200 lines for `.instructions.md`, 500 lines for `copilot-instructions.md`), flag for user decision: skip, replace an existing lower-priority rule, or split into a new file.

## Phase C: Draft

For each learning that passed classification and de-duplication:

1. Draft the specific text to add or modify, following the target file's existing style and structure.
2. Show before/after context for modifications (surrounding 3 lines minimum).
3. For new additions, show the insertion point with surrounding context.

Present all proposed changes in a structured approval table:

| # | Learning | Severity | Category | Target File | Action | Approved? |
|---|----------|----------|----------|-------------|--------|-----------|
| 1 | {title} | {severity} | {category} | {file} | Add / Modify | Pending |
| 2 | {title} | {severity} | {category} | {file} | Add / Modify | Pending |

For each row, include:

* The drafted text (in a fenced code block)
* Evidence reference (artifact path and line)
* Insertion point or modification context (before/after)

> [!IMPORTANT]
> **STOP.** Present the approval table and drafted changes to the user. Wait for explicit approval on each individual change before proceeding to Phase D.

## Phase D: Apply

For each change approved by the user:

1. Apply the change atomically to the target file.
2. Verify the file still renders correctly (no broken markdown, no orphaned list items).
3. If a change fails or conflicts, STOP and report the issue. Do not proceed with remaining changes.

After all approved changes are applied:

1. **Log the session** by appending a row to `.copilot-tracking/harness-evolution-log.md`:

   | Date | PR | Plan | Proposed | Accepted | Severity (C/H/M/L) | Files Modified | Status |
   |------|----|------|----------|----------|---------------------|----------------|--------|
   | {today} | {PR if known} | {slug} | {count proposed} | {count accepted} | {C}/{H}/{M}/{L} | {basenames} | {complete/partial/skipped} |

2. **Suggest a commit message** following project commit standards:

   ```text
   core(harness): encode learnings from {slug}
   ```

> [!IMPORTANT]
> **STOP.** Present the evolution log entry and suggested commit message. Do not commit or push. The user handles version control.
