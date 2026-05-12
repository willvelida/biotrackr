---
name: sdd-5-implement
description: "Execute one implementation phase with progress tracking and verification. Use when: implementing tasks from an SDD plan, logging discoveries, updating task tables, and running build/test verification per phase."
---

# SDD Phase 5: Implement

> [!CAUTION]
> LOG DISCOVERIES IMMEDIATELY. After each task, update the task table and append to the execution log. Do not batch updates at the end.

> [!IMPORTANT]
> UPDATE PROGRESS PER TASK. Every task requires a 3-step update: (1) task table checkbox, (2) notes column, (3) execution log entry.

## Inputs

* **slug** (Optional): Slug from prior phases. Inferred from the most recent SDD plan directory if omitted.
* **phase** (Optional, default 1): Phase number to implement.
* **plan** (Optional): Path to the plan file from Phase 4.

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

## Step 1: Load Phase Context

1. Read the plan file and locate the specified phase's task table.
2. Read the specification for acceptance criteria relevant to this phase.
3. Read any prior execution logs for completed phases (context continuity).
4. Create or append to the execution log at `.copilot-tracking/plans/{date}/{slug}/execution.log.md`.

## Step 2: Technology-Appropriate Delegation

For each task, before implementation:

1. Detect the primary technology from the task's affected file paths (file extensions, directory patterns).
2. Search `.github/agents/` for a specialized agent matching that technology.
3. If a matching agent exists, delegate the task to it.
4. If no specialized agent exists, proceed with direct implementation.

Do not hardcode agent names or technology mappings. Detect both dynamically from the workspace.

## Step 3: Execute Tasks

For each task in the phase's task table, execute in order:

### 3a: Mark In-Progress

Update the task status from `[ ]` to `[~]` in the plan file's task table.

### 3b: Implement

Execute the task (delegated or direct). Follow conventions and patterns identified during Doctrine Resolution.

### 3c: Verify

Run the project's build command per doctrine. Run the project's test command per doctrine. Record pass/fail results.

### 3d: Mark Completed

Update the task status from `[~]` to `[x]` in the plan file's task table. Add a direct execution log link in the Notes column using this exact format: `execution.log.md#task-{id}`.

### 3e: Log

Append to `execution.log.md`:

```markdown
<a id="task-{ID}"></a>
## Task {ID}: {Task Description}

* **Status**: Completed
* **Changes Made**: {List of files created, modified, or removed}
* **Verification**: {Build result, test result, coverage if applicable}
* **Discoveries**: {Any findings during implementation, or "None"}
```

### 3f: Handle Blocked Tasks

If a task cannot proceed due to an unresolved dependency or ambiguity:

1. Update the task status to `[!]` (blocked).
2. Log the blocker in the execution log with the specific reason.
3. **STOP execution and ask the user for guidance.** Do not skip blocked tasks or continue with dependent tasks.

Independent subsequent tasks (no dependency on the blocked task) may continue only if the user explicitly approves.

## Step 4: Log Discoveries

Log discoveries immediately when encountered, not at the end. Each discovery includes a typed category:

| Category | Use When |
|----------|----------|
| gotcha | Surprising behavior that could trip up future work |
| research-needed | Knowledge gap requiring deeper investigation |
| unexpected-behavior | Runtime or build behavior differing from expectations |
| workaround | Temporary fix applied due to a known limitation |
| decision | Design choice made during implementation with rationale |
| debt | Technical debt introduced or identified for future resolution |
| insight | Reusable learning applicable beyond this task |

Format:

```markdown
### Discovery: {Title}

* **Category**: {gotcha | research-needed | unexpected-behavior | workaround | decision | debt | insight}
* **Task**: {Task ID}
* **Evidence**: {file:line or command output}
* **Description**: {What happened and why it matters}
```

## Step 5: Phase Completion Summary

When all tasks in the phase are `[x]` (or `[!]` with user acknowledgment):

1. Update the phase status in the plan's Progress section.
2. Update the Current State section in the plan with a briefing for the next session.
3. Summarize to the user:
   * Tasks completed count.
   * Tasks blocked count (if any).
   * Discoveries logged.
   * Build and test status.
   * Recommended next step (Phase 6: Review, or next implementation phase).

---

> [!IMPORTANT]
> **STOP.** Present the phase completion summary and wait for user direction. Do not automatically proceed to the next phase or to review.