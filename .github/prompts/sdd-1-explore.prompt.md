---
description: "Deep codebase research before feature specification — read-only exploration"
argument-hint: "topic=... [slug=...]"
---

# SDD Phase 1: Explore

> [!CAUTION]
> Do NOT modify any source files during this phase. This is a READ-ONLY research command.

## Inputs

* ${input:topic}: (Required) Feature or change to research.
* ${input:slug}: (Optional) Short kebab-case identifier for the artifact directory. Inferred from topic if omitted.

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

## Step 1: Create Artifact Directory

Create the artifact directory at `.copilot-tracking/plans/{date}/{slug}/` where `{date}` is today's date in `YYYY-MM-DD` format and `{slug}` is the kebab-case identifier.

## Step 2: Launch Parallel Research Subagents

Launch these parallel research subagents using evidence-first investigation. Each subagent uses file search, grep, and read operations to gather evidence before analysis.

1. **Codebase Pattern Analyst** — map existing architecture, module boundaries, naming conventions, and established patterns relevant to the topic.
2. **Technical Investigator** — identify technical constraints, dependencies, integration points, and potential blockers for the proposed feature or change.
3. **Discovery Documenter** — catalog related prior work, decision records, existing tests, and documentation that informs the topic.
4. **Dependency Mapper** — trace upstream and downstream dependencies, shared contracts, and cross-module coupling related to the topic.

If codebase-memory-mcp tools are available (detected via tool listing), additionally use `get_architecture` and `search_graph` to supplement evidence gathering.

Each subagent produces numbered discoveries with: Title, Category, Impact (Critical/High/Medium/Low), Evidence (file path and line), Description, and Action Required.

## Step 3: Synthesize Research Dossier

Collect findings from all subagents, deduplicate, and synthesize into `.copilot-tracking/plans/{date}/{slug}/research-dossier.md` with these canonical sections in order:

1. **Executive Summary** — one-paragraph overview of research findings.
2. **Codebase Landscape** — architecture overview relevant to the topic, module boundaries, and key abstractions.
3. **Existing Patterns** — conventions and patterns already established that the feature should follow.
4. **Dependencies** — upstream/downstream dependencies, shared contracts, and coupling points.
5. **Technical Constraints** — limitations, platform requirements, and non-negotiable boundaries.
6. **Integration Points** — where the feature connects to existing systems, APIs, or data flows.
7. **External Research Opportunities** — knowledge gaps that would benefit from deeper investigation beyond the codebase.
8. **Affected Modules/Services** — list of modules or services that the feature would touch or depend on.

All findings must include evidence references (file paths and line numbers). Unsupported claims become `[UNVERIFIED]` markers.

---

> [!IMPORTANT]
> **STOP.** This is a READ-ONLY research command. Do NOT proceed to specification, implementation, or any other phase. Do NOT modify any source files. Present the research dossier to the user and wait for further instructions.
