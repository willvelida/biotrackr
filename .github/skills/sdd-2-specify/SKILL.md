---
name: sdd-2-specify
description: "Create technology-free feature specification — WHAT and WHY, not HOW. Use when: writing a feature specification after the Explore phase, defining acceptance criteria and complexity scores, documenting goals and non-goals."
---

# SDD Phase 2: Specify

Focus on user value. Do NOT include stack, framework, or technology choices in the specification.

## Inputs

* **slug** (Optional): Slug from the Explore phase. Inferred from context if omitted.
* **dossier** (Optional): Path to research dossier from SDD Phase 1.

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

## Step 1: Load Research Context

If a research dossier exists (at `{dossier}` or `.copilot-tracking/plans/{date}/{slug}/research-dossier.md`), read it and extract relevant findings. If no dossier is available, proceed with the information provided in the user's topic description.

## Step 2: Create Feature Specification

Create `{slug}-spec.md` in `.copilot-tracking/plans/{date}/{slug}/` with the following canonical sections in strict order:

### Summary

One-paragraph description of the feature in terms of user value and business outcome.

### Goals

Bulleted list of what this feature achieves. Each goal is outcome-oriented and measurable.

### Non-Goals

Explicitly scoped-out items. Prevents scope creep by naming what this feature does NOT do.

### Acceptance Criteria

Numbered, testable scenarios that define "done." Each criterion follows the pattern: "Given [context], when [action], then [outcome]."

### Complexity Score

Assign CS-1 through CS-5 using the project's complexity rubric from doctrine. Apply the 6-factor scoring model (Surface Area, Integration, Data/State, Novelty, NFR, Testing) if the rubric is documented. If no rubric is found in doctrine, use this default mapping:

* CS-1: Single file, no integration, no new data.
* CS-2: Few files in one module, minimal integration.
* CS-3: Multiple modules, cross-service integration, or new data models.
* CS-4: Architectural change, new external dependencies, or security implications.
* CS-5: Platform-level change, new service, or fundamental pattern shift.

### Risks and Assumptions

Known risks with likelihood and impact. Assumptions that underpin the specification. Mark uncertain items with `[NEEDS CLARIFICATION]`.

### Open Questions

Unresolved decisions or information gaps. Each question identifies who can answer it and the impact of leaving it unresolved.

### Affected Modules/Services

List of modules, services, or components that this feature touches or depends on, based on research findings or codebase analysis.

### Testing Strategy

Placeholder for Phase 3 (Clarify) to populate. The testing approach (Standard, Lightweight, or None) is decided during clarification.

`[NEEDS CLARIFICATION: Testing approach not yet decided — run SDD Phase 3 (Clarify)]`

### Workshop Opportunities

Table of topics that would benefit from deeper design exploration before implementation:

```markdown
| Topic | Type | Why It Matters | Priority |
|-------|------|----------------|----------|
```

Types include: Data Model, API Contract, State Machine, Integration Pattern, Storage Design, or other design-specific categories.

## Step 3: Gate Check

Scan the completed specification for `[NEEDS CLARIFICATION]` markers. If any exist, note them in the output summary. These do not block completion but signal that SDD Phase 3 (Clarify) should address them.