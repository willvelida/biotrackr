---
name: sdd-3-clarify
description: "Resolve specification ambiguities through targeted questions. Use when: a specification has NEEDS CLARIFICATION markers, open questions need resolution, or workflow mode and testing approach need to be decided."
---

# SDD Phase 3: Clarify

Resolve ambiguities and unknowns in the feature specification through a maximum of 8 targeted questions.

## Inputs

* **slug** (Optional): Slug from prior phases. Inferred from context if omitted.
* **spec** (Optional): Path to the spec file from SDD Phase 2.

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

## Step 1: Read the Specification

Load the spec from `{spec}` or `.copilot-tracking/plans/{date}/{slug}/{slug}-spec.md`. If the spec cannot be found, ask the user for the path.

## Step 2: Identify Ambiguities

Analyze the specification for:

* Explicit `[NEEDS CLARIFICATION]` markers from Phase 2.
* Vague acceptance criteria that lack testable outcomes.
* Missing scope boundaries between Goals and Non-Goals.
* Unstated assumptions in Risks and Assumptions.
* Open Questions that block downstream architecture decisions.
* Complexity Score factors that could shift based on design choices.

## Step 3: Generate Questions

Produce a maximum of 8 questions, prioritized by impact on downstream phases. The first two questions are mandatory and always appear in this order:

1. **Workflow Mode** — "Should this feature follow Simple mode (lighter architecture phase with fewer subagents) or Full mode (complete subagent research with 4 parallel investigators)?" Options: Simple, Full. Simple mode is typical for straightforward features; Full mode is recommended for cross-cutting or architecturally complex changes.
2. **Testing Approach** — "What testing approach should this feature follow?" Options: Standard (full test coverage per doctrine), Lightweight (critical paths only), None (skip testing per doctrine allowance).

Questions 3 through 8 are generated from the ambiguity analysis, ordered by descending impact on architecture and implementation decisions. Each question includes:

* A clear, specific question phrased for a yes/no or multiple-choice answer where possible.
* Context explaining why the answer matters for downstream phases.
* Suggested options when applicable.

## Step 4: Present Questions

Present questions one at a time in the conversation. Wait for each answer before presenting the next question. Skip remaining questions if the user indicates they want to proceed without further clarification.

## Step 5: Update Specification

Append a `## Clarifications` section to the spec file recording each question and answer with today's date:

```markdown
## Clarifications

| Date | Question | Answer |
|------|----------|--------|
| {date} | Workflow Mode | {answer} |
| {date} | Testing Approach | {answer} |
```

Update any `[NEEDS CLARIFICATION]` markers in the spec body that were resolved by the answers. Leave unresolved markers intact for future passes.