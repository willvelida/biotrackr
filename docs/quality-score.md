---
title: Quality Score
description: Grade tracking for Biotrackr service groups and architectural layers, plus the Simplification Log recording what has been removed and whether anything degraded
ms.date: 2026-08-21
ms.topic: reference
---

Whether the repository is getting stronger or weaker over time. This is the counterpart to a code review, which asks whether one change was good. Update it when a service group or layer meaningfully changes, not on every commit.

## Grading scale

* `A`: verified, legible, stable, boundaries enforced
* `B`: working with minor gaps
* `C`: partially working, with notable confusion or instability
* `D`: broken, unsafe, or structurally unclear

A grade is only meaningful with evidence behind it. Leave a cell as `-` rather than guessing.

## Service groups

| Group                            | Grade | Verification | Agent legibility | Test stability | Key gaps                                          | Last updated |
|----------------------------------|-------|--------------|------------------|----------------|----------------------------------------------------|--------------|
| Domain (Activity, Food, Sleep, Vitals) | -     | -            | -                | -              | -                                                  | -            |
| AI components (Chat, MCP, Reporting)   | -     | -            | -                | -              | -                                                  | -            |
| Auth                             | -     | -            | -                | -              | One source project, two deployed jobs               | -            |
| UI                               | -     | -            | -                | -              | Unit tests only, no integration tier                | -            |

## Architectural layers

| Layer      | Grade | Boundary enforcement | Agent legibility | Key gaps                                              | Last updated |
|------------|-------|----------------------|------------------|--------------------------------------------------------|--------------|
| API        | -     | -                    | -                | Route prefixes enforced only by APIM configuration      | -            |
| Svc        | -     | -                    | -                | -                                                      | -            |
| Repository | -     | -                    | -                | Both domains share one Cosmos container                 | -            |
| Bicep      | -     | -                    | -                | Default linter rules only, no `bicepconfig.json`        | -            |

## Benchmark snapshots

| Date       | Harness variant | Completion rate | Retries | Defects before review | Notes |
|------------|-----------------|-----------------|---------|-----------------------|-------|
| 2026-08-21 | baseline        | -               | -       | -                     | Pre-refactor scorecard captured by `scripts/audit-harness.sh` |

## Simplification Log

Remove a component, re-run the audit and the affected service's tests, and record what happened. If nothing degraded, the component was not carrying its weight. If something degraded, restore it and record why it mattered.

An entry is only complete once the Outcome column reflects a re-run, not an expectation.

| Date       | Component removed                                       | Outcome              | Decision     |
|------------|----------------------------------------------------------|----------------------|--------------|
| 2026-08-21 | 12 SDD prompts under `.github/prompts/sdd-*.prompt.md`, near-verbatim duplicates of 12 SDD skills | audit 6/6 exit 0; all 12 phases still invokable | keep removed |
| 2026-08-21 | Orphan skill `create-github-pull-request-from-specification`, zero references | audit 6/6 exit 0; no live references remain | keep removed |
| 2026-08-21 | `harness-health` moved from a prompt to a skill so Copilot CLI can reach it | audit 6/6 exit 0; now reachable from CLI and Agent Host | keep removed |
| 2026-08-21 | ~283 lines of reference architecture relocated out of `.github/copilot-instructions.md` into `docs/` | always-loaded 1,012 to 207 lines; 43.2% of the Codex cap | keep removed |
| 2026-08-21 | `dotnet-best-practices` skill, proposed for removal as an orphan | **reverted** | keep |

The `dotnet-best-practices` reversion is the useful entry here. The deterministic audit
reported it as a zero-reference orphan, but it is pulled into `code-review` and
`dead-code-scan` through `{{#runtime-import}}` in `.github/workflows/shared/dotnet-knowledge.md`.
The audit was not scanning `.github/workflows/`, so a live dependency looked like dead
weight. The corpus was widened rather than the skill deleted.

## Related documents

* [harness-guide.md](harness-guide.md) for the harness inventory these entries prune
* [standards/harness-governance.md](standards/harness-governance.md) for the complexity rubric and size budgets
