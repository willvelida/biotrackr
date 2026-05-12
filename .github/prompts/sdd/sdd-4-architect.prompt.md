---
description: "Generate phased implementation blueprint with parallel research subagents"
argument-hint: "[slug=...] [spec=...] [dossier=...]"
---

# SDD Phase 4: Architect

> [!IMPORTANT]
> NO TIME ESTIMATES. Use CS-1 through CS-5 complexity scoring exclusively. Never predict hours, days, or sprints.

## Inputs

* ${input:slug}: (Optional) Slug from prior phases. Inferred from the most recent SDD plan directory if omitted.
* ${input:spec}: (Optional) Path to the specification file from Phase 2/3.
* ${input:dossier}: (Optional) Path to the research dossier from Phase 1.

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

## Step 1: Validate Entry Gate

Read the specification and verify readiness:

1. Confirm no unresolved `[NEEDS CLARIFICATION]` markers remain (warn if found but do not block).
2. Confirm a Testing Strategy section or a Clarifications section with a testing approach answer exists in the specification. If neither exists, ask the user for the testing approach before proceeding.
3. If architecture documentation exists in doctrine, validate the specification aligns with documented boundaries and patterns.
4. If critical gaps are found (no spec file, no acceptance criteria), stop and report them to the user.

## Step 2: Launch Parallel Research Subagents

Select research depth based on available artifacts.

### Optimized Mode (dossier provided)

Launch 2 parallel subagents when a research dossier already exists:

1. **Implementation Strategist** — analyze the specification against the dossier findings. Identify implementation order, phase boundaries, dependency chains, and risk-adjusted sequencing.
2. **Risk and Mitigation Planner** — cross-reference dossier constraints with specification acceptance criteria. Identify blockers, fallback strategies, and validation checkpoints.

### Full Research Mode (no dossier)

Launch 4 parallel subagents when no research dossier exists:

1. **Codebase Pattern Analyst** — use file search, grep, and read operations to map existing architecture, conventions, integration points, and established patterns relevant to the specification.
2. **Technical Investigator** — identify constraints, API limits, framework quirks, and dependency gotchas that affect implementation.
3. **Discovery Documenter** — catalog specification ambiguities, implications, edge cases, and existing tests or documentation that inform the plan.
4. **Dependency Mapper** — trace module dependencies, boundaries, shared contracts, and cross-cutting concerns for affected areas.

### Evidence-First Pattern

Each subagent follows this investigation sequence:

1. Gather evidence using glob, grep, and file read operations.
2. Analyze findings against the specification requirements.
3. Produce 5-8 numbered discoveries per subagent.

## Step 3: Synthesize Discoveries

Collect findings from all subagents, deduplicate, and order by impact. Produce a minimum of 10 final discoveries.

Each discovery follows this format:

```markdown
### Discovery D-01: {Title}

* **Category**: {Pattern | Integration | Convention | Constraint}
* **Impact**: {Critical | High | Medium | Low}
* **Evidence**: {file:line reference}
* **Description**: {What was found}
* **Why It Matters**: {Impact on implementation}
* **Example**:

  ❌ WRONG:

  {Incorrect pattern with brief explanation}

  ✅ CORRECT:

  {Correct pattern with brief explanation}

* **Action Required**: {What the implementation must do}
```

Discoveries without file:line evidence become `[UNVERIFIED]` markers.

## Step 4: Score Complexity

Assess complexity using the CS-1 through CS-5 rubric with six factors:

| Factor | Score (0-2) | Rationale |
|--------|-------------|-----------|
| Surface Area (S) | | |
| Integration (I) | | |
| Data/State (D) | | |
| Novelty (N) | | |
| Non-Functional (F) | | |
| Testing (T) | | |
| **Total** | | **CS-{N}** |

## Step 5: Generate Phased Plan

Create the plan file at `.copilot-tracking/plans/{date}/{slug}/{slug}-plan.md` using the template at `.copilot-tracking/templates/sdd-design-template.md`.

### Phase Decomposition

Break the implementation into ordered phases. Each phase should be independently buildable and testable when possible. Annotate phase dependencies explicitly.

### Task Tables

Each phase uses the 6-column task table format with 4-state checkboxes:

```markdown
| Status | ID   | Task              | Path(s)         | Done When             | Notes |
|--------|------|-------------------|-----------------|-----------------------|-------|
| [ ]    | T001 | {Task description} | {/path/to/file} | {Success criteria}    |       |
```

Task status legend: `[ ]` pending, `[~]` in-progress, `[x]` completed, `[!]` blocked.

### Validation Section

Reference the project's build command, test command, and coverage threshold from doctrine. Do not hardcode tool-specific commands. Use the format:

```markdown
## Validation

* Build: {per doctrine build command}
* Test: {per doctrine test command}
* Coverage: {per doctrine threshold, or [TODO] if unknown}
```

## Step 6: Present Plan for Review

Present the completed plan to the user with:

1. The complexity score and rationale.
2. Phase count and dependency annotations.
3. Total task count across all phases.
4. Key discoveries that most influence the plan structure.
5. Any `[TODO]` markers that require user input.

---

> [!IMPORTANT]
> **STOP.** Present the plan to the user and wait for approval before proceeding to Phase 5 (Implement). The user may request adjustments to phase boundaries, task ordering, or scope.
