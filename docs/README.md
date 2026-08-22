---
title: Documentation Index
description: Routing map for Biotrackr documentation, listing the authoritative documents and stating what does not belong in this directory
ms.date: 2026-08-21
ms.topic: reference
---

Authoritative reference for how Biotrackr works. `AGENTS.md` at the repository root carries the commands and constraints needed on every task and links here for everything else.

The table is a routing map, not a reading order. When to read which document is stated in the Startup Workflow in `AGENTS.md`.

## Routing map

| Document | Purpose |
|----------|---------|
| [product.md](product.md) | What Biotrackr is, who it serves, the four data domains, device integrations |
| [architecture.md](architecture.md) | Service topology, service types, APIM routing, storage model, independent builds |
| [ai-architecture.md](ai-architecture.md) | Agent, MCP server, and reporting pipeline internals |
| [security.md](security.md) | Agentic security controls, identity, gateway enforcement, secrets |
| [testing.md](testing.md) | Test tiers, fixtures, coverage policy and commands |
| [development.md](development.md) | Local setup, emulator, running the stack, known failure modes |
| [infrastructure.md](infrastructure.md) | Bicep layout, module domains, deployment pipeline |
| [harness-guide.md](harness-guide.md) | Agent, prompt, skill, and instruction inventories, plus harness doctrine |
| [quality-score.md](quality-score.md) | Grade tracking and the Simplification Log |
| [standards/commit-standards.md](standards/commit-standards.md) | Commit format, scopes, sign-off, AI contribution trailers |
| [standards/harness-governance.md](standards/harness-governance.md) | Complexity rubric, size budgets, maturity model, measurement |
| [decision-records/](decision-records/) | Architecture Decision Records, append-only |

## Supporting references

Narrower documents reached from the ones above rather than listed as entry points:

* [devcontainer-setup.md](devcontainer-setup.md) and [cosmos-emulator-setup.md](cosmos-emulator-setup.md), both linked from [development.md](development.md)
* [bicep-modules-structure.md](bicep-modules-structure.md), linked from [infrastructure.md](infrastructure.md)
* [github-workflow-templates.md](github-workflow-templates.md), the reusable workflow template catalogue

## What does not belong here

This index is intentionally narrow. Adding to it has a cost, because an index that lists everything routes to nothing.

Keep out:

* Anything derivable from the repository itself. Directory trees, file inventories, service lists, and dependency lists go stale on the next commit and cost tokens without changing a decision.
* Edit-time conventions. Those belong in `.github/instructions/`, where they load automatically for the files they govern.
* Commands needed on every task. Those belong in `AGENTS.md`.
* Per-feature designs, plans, and progress tracking. Those belong in `.copilot-tracking/`.
* Decisions with lasting consequence. Those belong in `decision-records/` as an ADR, not as prose in a topic document.

Apply the deletion test before adding anything: if removing it would not change a decision, it should not exist.

## Not normative

Three directories under `docs/` hold narrative or historical content that is not a source of truth. Do not cite them as evidence of current behaviour:

* [blog-post-ideas/](blog-post-ideas/README.md) holds drafts written for a general audience.
* [plans/](plans/README.md) holds completed and abandoned plans kept for provenance.
* `presentation-notes/`, `reports/`, and `scratch/` hold working material with no maintenance guarantee.
