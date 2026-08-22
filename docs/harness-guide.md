---
title: Harness Guide
description: Inventory of Biotrackr's agents, prompts, skills, and instruction files, plus the doctrine governing how the harness is authored and maintained
ms.date: 2026-08-21
ms.topic: how-to
---

The harness is everything wrapping the model to raise the probability of correct output: instruction files that load automatically, agents with verification loops, prompts encoding multi-step workflows, and skills carrying domain knowledge. It activates on its own when you use GitHub Copilot in this repository.

This is the inventory. Build, test, and coverage commands live in `AGENTS.md` and [testing.md](testing.md). Local setup lives in [development.md](development.md).

## Automatic context loading

Editing a file loads the instruction files matching its path. Multiple matches all apply; there is no shadowing, so a test file in a repository class loads both `**/*.cs` files and the test conventions.

| You edit | Loads | Covers |
|----------|-------|--------|
| `*.cs` | csharp-conventions, dsa-awareness | Naming, DI, error handling, async, data structure selection |
| `*Tests*/**/*.cs` | testing-conventions | xUnit, AAA pattern, naming, coverage, agent-readable assertions |
| `*Repository*.cs`, `*Document*.cs`, `*Cosmos*.cs` | cosmos-conventions | Repository pattern, query safety, partition keys, lifetimes |
| `*.bicep` | bicep-conventions | Three-tier layout, parameter conventions, module naming |
| `*.razor` | razor-components | Component structure, Radzen patterns, accessibility |
| `*.razor.css` | css-conventions | CSS isolation, `bt-` prefix, Radzen theming |
| `*.yml` | github-actions-conventions | Action pinning, permissions, OIDC, concurrency |
| SDD plan artifacts | sdd-conventions | Plan, spec, and evolution log structure |

Two files load on every request regardless of path: `AGENTS.md` and `.github/copilot-instructions.md`. They exist separately because different Copilot surfaces read different files, not because one is a copy of the other. There are 9 instruction files.

## Agents

Select an agent from the chat mode dropdown at the top of the Copilot Chat panel. There are 11 custom agent definitions.

Four agents generate code and run a verification protocol before returning:

| Agent | File | Use for |
|-------|------|---------|
| C# Expert | `CSharpExpert.agent.md` | .NET implementation, SOLID design, test writing, performance |
| Front-End Designer | `front-end-designer.agent.md` | Blazor components, Radzen UI, CSS, accessibility, responsiveness |
| Bicep Specialist | `bicep-implement.agent.md` | Azure infrastructure templates and modules |
| GitHub Actions Expert | `github-actions-expert.agent.md` | CI/CD workflows, action security, OIDC configuration |

Four review and analyse without modifying files:

| Agent | File | Use for |
|-------|------|---------|
| Code Reviewer | `code-reviewer.agent.md` | Pre-push convention check, read-only |
| Vulnerability Scanner | `vulnerability-scanner.agent.md` | OWASP security audit across the applicable frameworks |
| DSA Mentor | `dsa-mentor.agent.md` | Data structure and algorithm learning, complexity analysis |
| Azure Principal Architect | `azure-principal-architect.agent.md` | Azure architecture decisions, Well-Architected evaluation |

Three drive workflows:

| Agent | File | Use for |
|-------|------|---------|
| SDD Workflow | `sdd-workflow.agent.md` | Dispatcher that detects which SDD phase runs next from existing artifacts |
| SDD Review Judge | `sdd-review-judge.agent.md` | SDD quality gate, running the review with an elevated model as judge |
| Agentic Workflows | `agentic-workflows.agent.md` | GitHub Agentic Workflow authoring and management |

All definitions live in `.github/agents/`.

The Code Reviewer produces a findings table and a verdict of APPROVE or REQUEST_CHANGES, capped at two review cycles:

| File | Line | Severity | Finding | Fix |
|------|------|----------|---------|-----|
| Handlers.cs | 42 | ERROR | Missing parameterized QueryDefinition | Use QueryDefinition with parameters per cosmos-conventions |

## Prompts

Invoke a prompt by typing `/` in the chat input, or attach one with `#prompt:name`. There are 12 prompt templates.

| Prompt | Purpose | Agent |
|--------|---------|-------|
| `/new-endpoint` | Create an API endpoint with build and test gates | C# Expert |
| `/cross-service-change` | Coordinate a change across several services | C# Expert |
| `/refactor` | Refactor with baseline capture and regression verification | C# Expert |
| `/new-component` | Scaffold a Blazor component with accessibility built in | Front-End Designer |
| `/sdd-6-review` | SDD quality gate review, routed to an elevated judge model | SDD Review Judge |
| `/accessibility-audit` | WCAG 2.2 AA compliance check | Front-End Designer |
| `/design-review` | UX quality, responsiveness, and performance review | Front-End Designer |
| `/perf-optimize` | Front-end performance optimisation | Front-End Designer |
| `/dsa-code-review` | Review code for algorithmic anti-patterns | DSA Mentor |
| `/dsa-concept-explain` | Explain a DSA concept using Biotrackr examples | DSA Mentor |
| `/dsa-algorithm-design` | Design an algorithm for a stated requirement | DSA Mentor |
| `/dsa-performance-analysis` | Profile time and space complexity, suggest optimisations | DSA Mentor |

## Skills

The 37 skills carry domain knowledge and load when the task matches their description. They fall into six groups:

* OWASP vulnerability knowledge bases, one per framework: agentic, CI/CD, Docker, infrastructure, LLM, MCP, ML, mobile, open source, serverless, and web.
* Design and UX: accessibility, Blazor design, mobile design, web design.
* Engineering practice: .NET best practices, front-end performance.
* Data structures and algorithms: foundations, linear structures, trees and heaps, graphs, algorithm paradigms, interview patterns, system design.
* SDD workflow: one skill per phase, providing the same slash commands in Copilot CLI.
* Harness maintenance: `harness-health`, invoked as `/harness-health`, which audits the harness itself and defers every structural finding to `scripts/audit-harness.sh`.

## Multi-session work

Score complexity before starting, using the CS-1 to CS-5 rubric in [standards/harness-governance.md](standards/harness-governance.md). CS-1 and CS-2 proceed directly on a single service with familiar patterns. CS-3 and above get an execution plan first, because they cross services, change schemas, or introduce new patterns.

Execution plans are copied from `.copilot-tracking/templates/exec-plan-template.md` into `.copilot-tracking/plans/{feature}-plan.md`. The template covers purpose, progress checkboxes, current state, a decision log, validation commands, and affected services. The Current State section is the bridging mechanism: a one or two paragraph briefing the next session reads to restore context. Without it a plan is only a checklist, and a checklist does not carry why a decision was made.

Progress files are the lighter option, copied from `.copilot-tracking/templates/progress-template.md` into `.copilot-tracking/tasks/{task-name}.md`. Start by reading the file and finding the next unchecked item, work by checking items off and updating Current State, and end by adding a session log entry. Cap them at 200 lines and archive on completion.

## SDD workflow

Spec-Driven Development separates WHAT and WHY from HOW before code is written. The workflow is stack-agnostic and never hardcodes a build or test command; it resolves project conventions from the repository's own rules files at the start of every phase. Each phase is useful standalone.

| Complexity | Approach |
|------------|----------|
| CS-1, CS-2 | Optional. Use the direct prompts instead |
| CS-3, CS-4 | Recommended. Simple mode uses lighter architecture research |
| CS-5 | Strongly recommended. Use Full mode with subagent research |

Seven core phases, with the five optional ones shown indented:

1. Explore (`/sdd-1-explore`) researches the codebase read-only and produces a research dossier of landscape, existing patterns, dependencies, and integration points.
2. Specify (`/sdd-2-specify`) writes a technology-free specification with acceptance criteria, a complexity score, and affected modules. Unknowns are marked `[NEEDS CLARIFICATION]`.
    * Prep Issue (`/sdd-2b-prep-issue`) turns the spec into copy-paste GitHub Issue text.
    * Workshop (`/sdd-2c-workshop`) explores a design topic in depth with options and trade-offs.
3. Clarify (`/sdd-3-clarify`) resolves ambiguities in eight questions or fewer, and sets the workflow mode and testing approach.
    * ADR (`/sdd-3a-adr`) records a decision that outlives the feature, in the existing `decision-records/` format.
4. Architect (`/sdd-4-architect`) produces a phased blueprint with task tables, backed by parallel research subagents that gather evidence before analysis.
    * Validate (`/sdd-4a-validate`) runs parallel validators and issues a READY or NOT READY verdict.
    * Did You Know (`/sdd-4b-didyouknow`) surfaces non-obvious insights one at a time and updates artifacts as each is discussed.
5. Implement (`/sdd-5-implement`) executes one phase at a time, delegating to the agent matching the technology being modified, and verifies after each task.
6. Review (`/sdd-6-review`) checks spec compliance, conventions, coverage, and cross-module consistency, then issues APPROVE or REQUEST_CHANGES. Learning candidates are advisory and do not affect the verdict.
7. Evolve (`/sdd-7-evolve`) proposes harness updates from the completed cycle. Every change needs human approval.

Phases are skippable. Specify has the most standalone value, because it forces WHAT apart from HOW before coding.

The SDD Workflow agent is the alternative entry point: it detects which phase runs next from the artifacts that already exist.

### Doctrine resolution

Every phase begins by resolving project conventions rather than assuming them. It reads the repository rules files, falls back to scanning dependency manifests and build systems when none are found, extracts the build command, test command, coverage threshold, and naming conventions, and turns anything it cannot determine into an explicit TODO. Silent assumptions are the failure this protocol exists to prevent.

### Artifact chain

Each phase produces a specific artifact that the next phase consumes:

| Phase | Produces | Consumed By |
|-------|----------|-------------|
| 1. Explore | `research-dossier.md` | 2. Specify (optional), 4. Architect |
| 2. Specify | `{slug}-spec.md` | 2b. Prep Issue, 2c. Workshop, 3. Clarify, 4. Architect |
| 2b. Prep Issue | GitHub Issue text (copy-paste) | External tracker |
| 2c. Workshop | `workshops/{topic-slug}.md` | 3. Clarify, 3a. ADR |
| 3. Clarify | Updated spec with Clarifications | 3a. ADR, 4. Architect |
| 3a. ADR | `docs/decision-records/{YYYY-MM-DD}-{title-slug}.md` | 4. Architect |
| 4. Architect | `{slug}-plan.md` (with task tables) | 4a. Validate, 4b. Did You Know, 5. Implement |
| 4a. Validate | READY/NOT READY verdict | 5. Implement |
| 4b. Did You Know | Updated artifacts with insights | 5. Implement |
| 5. Implement | Code + `execution.log.md` | 6. Review |
| 6. Review | `review.md` (APPROVE/REQUEST_CHANGES) | 7. Evolve, or 5. Implement |
| 7. Evolve | Harness updates + evolution log entry | Instruction files |

Artifacts are stored under `.copilot-tracking/plans/{date}/{slug}/`, which is what lets a different session or agent pick the work back up.

### Technology-appropriate delegation

During implementation the workflow detects the technology of each file being modified and delegates to a specialist agent when one exists, proceeding directly when none does. No agent names are hardcoded, so delegation adapts to whatever agents a repository provides.

### Harness evolution

Most AI tooling fixes individual tasks. The Evolve phase modifies the governing instructions themselves, so discoveries from implementation become permanent conventions.

Four constraints keep that from drifting into bloat:

* Human approval on every proposed change.
* Size budgets on instruction files: 200 lines for path-scoped files, and no more than 20 added lines per evolution session for `copilot-instructions.md`.
* De-duplication checks against existing instructions.
* Separate commits for harness changes, distinct from code changes.

The evolution log at `.copilot-tracking/harness-evolution-log.md` records which plan produced each learning, what evidence supports it, and which files changed. It is committed, so the improvement history is visible.

### SDD file locations

* SDD skills, providing the slash commands in Copilot CLI: `.github/skills/sdd-*/`
* Design template: `.copilot-tracking/templates/sdd-design-template.md`
* Evolution log: `.copilot-tracking/harness-evolution-log.md`
* Dispatcher agent: `.github/agents/sdd-workflow.agent.md`

## The verification protocol

Code-generating agents follow the same loop: generate, build, fix and retry on failure, then test, then present the result. A maximum of two retry cycles applies at each gate.

When the second retry fails the agent stops and reports the exact error, what it tried, and its root cause assessment. It does not try a third time. Unbounded retry loops burn tokens and converge on nothing, which is why the bound is part of the protocol rather than a suggestion.

The commands behind each gate are in `AGENTS.md` and [testing.md](testing.md), not here. Duplicating them was how four copies of the coverage command came to exist.

### What the check actually matches

File-modifying agents under `.github/agents/*.agent.md` must carry a `## Verification Protocol` section. The `harness-health` skill matches that heading by **exact regex** (`^## Verification Protocol$`), so a semantically equivalent heading such as `## Testing & validation` fails the gate even when the body is correct.

Five structural elements are expected: the exact heading; an imperative preamble; two to five numbered checks using domain-appropriate commands (`dotnet build` and `dotnet test` for C# agents, `bicep build` for Bicep agents, since the linter runs inside `bicep build` and no separate lint command exists); the wording `Maximum 2 retry attempts` on each remediable check; and an explicit escalation step exposing the exact error, what was tried, and a root-cause assessment.

Two suffix variants are accepted for agents that do not modify files: `## Verification Protocol — Not Applicable` for dispatchers and routers whose edits are delegated, and `## Verification Protocol — CI-Validated` for agents whose output is checked server-side. Mixed-mode agents open with a conditional sentence stating that verification applies only when the invocation produced file modifications. Read-only agents whose description and body both say "read-only" are exempt entirely.

## The steering loop

When the same problem recurs across sessions, strengthen the harness instead of fixing it again:

1. A convention violation recurs, so add a rule to the relevant instruction file.
2. An agent ignores a pattern, so add an example, or a structural test whose assertion message tells the agent how to fix it.
3. A complex workflow fails, so write a prompt encoding the correct sequence.
4. Review catches the same issue repeatedly, so promote it to a CI check.

Repeated review feedback should become a mechanical rule, check, or linter rather than another explanation in chat. That promotion is what makes the harness compound instead of accumulate. Encode, do not document.

## Authoring rules

Four failure modes worth designing against.

Exclude derivable content by design. Directory listings, file inventories, and architecture overviews go stale immediately and cost reasoning tokens without improving results. Keep pitfalls, rationale, and non-default conventions. Apply the deletion test to every paragraph: if removing it would not change a decision, remove it.

Byte caps fire silently. Codex concatenates `AGENTS.md` files up to 32,768 bytes and truncates past that without warning. Nothing reports the loss. Keep one-line hooks in the always-loaded files and put the detail in topic documents.

Skill descriptions are capped at roughly 150 characters when listed. Front-load the distinctive trigger language, because whatever falls past the cap is invisible at selection time. Lead with the symptom, not the file name.

Hook trust is all or nothing. An untrusted workspace skips every hook rather than only the suspicious ones, so a post-edit verification hook silently no-ops instead of failing loudly. Never make a hook the only line of defence.

## Periodic maintenance

| Task | Frequency | How |
|------|-----------|-----|
| Deterministic harness audit | Per harness change | `scripts/audit-harness.sh` |
| Harness health audit | Monthly | `/harness-health` |
| Convention spot-check | Monthly | Sample three recent files against their instruction files |
| Simplification pass | Quarterly | Remove a component, re-audit, record in [quality-score.md](quality-score.md) |
| OWASP audit | Quarterly | Vulnerability Scanner agent |
| Security scan | Per PR, automated | CodeQL and dependency review in CI |

## Quick reference

| I want to | Do this |
|-----------|---------|
| Write a new endpoint | `/new-endpoint` |
| Change several services | `/cross-service-change` |
| Refactor safely | `/refactor` |
| Build a Blazor component | `/new-component` |
| Review before pushing | Code Reviewer agent |
| Check accessibility | `/accessibility-audit` |
| Audit harness health | `/harness-health` |
| Research before building | `/sdd-1-explore` |
| Write a feature spec | `/sdd-2-specify` |
| Record an architecture decision | `/sdd-3a-adr` |
| Plan implementation phases | `/sdd-4-architect` |
| See which SDD phase is next | SDD Workflow agent |
| Plan a complex feature | Copy `.copilot-tracking/templates/exec-plan-template.md` |
| Track multi-session work | Copy `.copilot-tracking/templates/progress-template.md` |
| Learn DSA concepts | DSA Mentor agent |
| Run a security scan | Vulnerability Scanner agent |

## Related documents

* [standards/harness-governance.md](standards/harness-governance.md) for governance, the maturity model, complexity scoring, and QITE measurement
* [quality-score.md](quality-score.md) for grade tracking and the Simplification Log
* [testing.md](testing.md) for test tiers, filters, and coverage
* [development.md](development.md) for local setup and running the stack
* `AGENTS.md` for build commands and boundary rules
