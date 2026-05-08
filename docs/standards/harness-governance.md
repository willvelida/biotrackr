---
title: Harness Governance
description: Canonical reference for Biotrackr's development feedback loop, harness maturity model, complexity scoring, and effectiveness measurement
ms.date: 2026-05-08
---

## Overview

This document defines the governance framework for Biotrackr's development harness — the collection of feedforward guides, feedback sensors, verification loops, and observability mechanisms that support AI-assisted development. It serves as the single source of truth for the boot-interact-observe protocol, complexity scoring, maturity tracking, and effectiveness measurement.

## Boot-Interact-Observe Protocol

The Boot-Interact-Observe protocol defines the standard development feedback loop for working on any Biotrackr service. Follow these steps when making changes to ensure correctness before committing.

### Boot

Start the required infrastructure and build the affected service(s).

**Dev Container (recommended):**

The dev container auto-boots the Cosmos DB vNext emulator, restores packages, builds all in-scope services, seeds 30 sample documents, and trusts the emulator certificate. No manual boot step is needed — the container is ready to interact on open.

```text
bash scripts/start-local.sh    (start all APIs + Caddy gateway + UI)
```

**Manual setup (outside dev container):**

```text
cosmos-emulator.ps1 start → wait for health check
dotnet build --no-restore -v:q (each affected service)
```

- Run `cosmos-emulator.ps1 start` only when E2E tests are in scope.
- Use `dotnet build --no-restore -v:q` for iterative builds when packages have not changed.
- Use `dotnet build --no-restore --no-dependencies -v:q` for single-project builds.

### Interact

Run tests at the appropriate tier for the change being validated.

```text
dotnet test --no-build                                                    (unit tests, no filter)
dotnet test --no-build --filter "FullyQualifiedName~Contract"             (contract tests)
dotnet test --no-build --filter "FullyQualifiedName~E2E"                  (E2E tests, requires emulator)
```

- Unit tests run by default without a filter and should always pass before committing.
- Contract tests verify DI registrations, service startup, and endpoint accessibility.
- E2E tests require a running Cosmos DB Emulator and exercise full HTTP request-response cycles.

### Observe

Collect evidence that the change is correct.

```text
dotnet test --collect:"XPlat Code Coverage" --settings ../coverage.runsettings
reportgenerator -reports:"./TestResults/**/coverage.cobertura.xml" -targetdir:"./CoverageReport" -reporttypes:TextSummary
Get-Content ./CoverageReport/Summary.txt
```

- 70% minimum line coverage is enforced in CI. Verify locally before pushing.
- Review the coverage summary for uncovered paths introduced by the change.

### Validate

Decide whether the change is correct based on observed evidence:

- All targeted test tiers pass.
- Coverage meets or exceeds 70%.
- No regressions in adjacent test tiers.
- Build output is clean (no warnings in changed files).

## Harness Maturity Model

The maturity model tracks progression from fully manual workflows toward autonomous agent-driven development. Each level builds on the capabilities of the previous level.

| Level | Description | Biotrackr Status |
|-------|-------------|------------------|
| L0 | No harness — manual everything | Past this |
| L1 | Manual boot + test — human starts stack, runs tests | Past this |
| L1.5 | Auto boot + manual test — dev container auto-boots infra, human runs tests | Current (dev container) |
| L2 | Auto boot + test — agent boots Cosmos, runs full validation | Target |
| L3 | Auto boot + full interaction + evidence capture | Future |
| L4 | Self-healing — auto-detects and recovers from common failures | Future |

### Current State (L1.5 in Dev Container, L1 outside)

With the dev container, Cosmos DB Emulator starts automatically, all services are pre-built, and 30 seed documents are loaded across 5 containers. The Boot step is eliminated — agents can immediately run tests against a live database. Outside the dev container, a human still manually boots the emulator and builds services (L1).

### Target State (L2)

Agents autonomously identify affected services from changed files, run the full Boot-Interact-Observe protocol, and report results. Human involvement is limited to reviewing the agent's evidence and approving the change.

## Complexity Scoring Rubric

Assign a complexity score (CS-1 through CS-5) before starting work on any task. The score determines the required planning overhead.

| Score | Surface Area | Integration | Data/State | Novelty | Non-Functional | Testing |
|-------|-------------|-------------|------------|---------|----------------|---------|
| CS-1 | Single file, one service | None | Read-only | Familiar pattern | None | Unit only |
| CS-2 | Multi-file, one service | Internal APIs | Simple CRUD | Minor variations | None | Unit + Contract |
| CS-3 | Cross-service | External APIs | Schema changes | New patterns | Performance | Unit + Contract + E2E |
| CS-4 | New service | Multi-service | New containers | New domain | Security + Perf | Full tier |
| CS-5 | Multi-service + infra | External + infra | Migration | Novel architecture | All | Full + Manual |

### Decision Guide

- **CS-1 or CS-2**: Proceed directly with implementation. No execution plan required.
- **CS-3 and above**: Create an execution plan at `.copilot-tracking/plans/` before starting implementation. Use the template at `.copilot-tracking/templates/exec-plan-template.md`.

### Scoring Process

1. Evaluate each of the 6 factors independently against the rubric.
2. The overall score is the **highest** individual factor score (not an average).
3. Document the score and rationale in the task's progress file or plan.

## Harness Health Dimensions

Four dimensions measure the overall health of the development harness. Use the harness-health audit prompt (`.github/prompts/harness-health.prompt.md`) for periodic assessment.

### Feedforward Coverage

Percentage of file types with scoped `.instructions.md` files. Target: every file type that agents regularly create or modify has a corresponding instruction file.

| File Type | Instruction File | Status |
|-----------|-----------------|--------|
| `*.cs` | csharp-conventions.instructions.md | Covered |
| `*Tests*/*.cs` | testing-conventions.instructions.md | Covered |
| `*.bicep` | bicep-conventions.instructions.md | Covered |
| `*.razor.css` | css-conventions.instructions.md | Covered |
| `*.razor` | razor-components.instructions.md | Covered |
| `*.cs` (DSA) | dsa-awareness.instructions.md | Covered |
| `*.yml` | github-actions-conventions.instructions.md | Covered |
| `*Repository*.cs`, `*Document*.cs` | cosmos-conventions.instructions.md | Covered |

### Feedback Sensor Coverage

CI stages per service and test tier coverage across the 14-service architecture.

- Each service pipeline should include: unit tests, contract tests, container build, Bicep lint/validate, deployment, and E2E tests.
- Coverage thresholds are enforced at 70% minimum, 80% healthy.

### Drift Detection

Agentic workflows running on schedule detect documentation staleness, dependency drift, and convention violations. Monitor workflow run history for failures or missed schedules.

### Architecture Fitness

Cross-service consistency and module boundary enforcement:

- No direct project references between services.
- Bicep modules pass lint and validation.
- Security scanning (CodeQL, dependency review) runs on every PR.

## QITE Measurement Guidelines

QITE (Quality, Iteration, Time, Efficiency) provides a lightweight, directional framework for measuring harness effectiveness. These metrics are not precision instruments — they track trends over time.

### Quality

- **First-pass CI success rate**: Percentage of pushes that pass CI on the first attempt.
- **Convention compliance**: Monthly spot-check of recently generated code against `.instructions.md` conventions.

### Iteration

- **Fix cycle count**: Number of edit-test-fix cycles per task before the change is correct.
- **Conversation depth**: Number of turns in an agent conversation to complete a task.

### Time

- **Wall-clock task time**: Elapsed time from task start to completion. Measured directionally — track whether tasks are trending faster or slower, not absolute precision.

### Efficiency

- **Human intervention rate**: Percentage of agent-generated changes that require human correction before merge.

### Tracking Methods

- Session journaling in `.copilot-tracking/tasks/` progress files.
- Git history analysis (commit frequency, fix-up commits, revert rate).
- CI signals (pipeline pass rate, coverage trends, build times).

### Measurement Window

For a solo-developer project like Biotrackr, collect data across 15-20 tasks per measurement period to establish meaningful trends. Shorter windows produce too much noise for directional conclusions.
