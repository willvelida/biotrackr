---
description: "Deep design exploration for Workshop Opportunities identified in a specification"
argument-hint: "[slug=...] [topic=...]"
---

# SDD Phase 2c: Workshop

Create detailed design documents for complex topics identified in a specification's Workshop Opportunities table.

## Inputs

* ${input:slug}: (Optional) Slug from prior phases. Inferred from context if omitted.
* ${input:topic}: (Optional) Specific workshop topic to explore. If omitted, present the Workshop Opportunities table for user selection.

## Step 0: Doctrine Resolution

Before starting, resolve project conventions:

1. Check for project doctrine files:
   * `docs/project-rules/` (constitution.md, rules.md, idioms.md, architecture.md)
   * `copilot-instructions.md`, `AGENTS.md`, `CONTRIBUTING.md`, `README.md`
2. If no doctrine found, scan the codebase for dependency manifests, build systems, and directory patterns.
3. Extract: build command, test command, coverage threshold, naming conventions, CI platform.
4. Unknown values become explicit `[TODO]` markers — never assume silently.

## Step 1: Load Workshop Context

1. Read the specification from `.copilot-tracking/plans/{date}/{slug}/{slug}-spec.md`.
2. Locate the `## Workshop Opportunities` table.
3. If `{topic}` is provided, find the matching row. If not found, list available topics and ask the user to select one.
4. If `{topic}` is not provided, present the table and ask the user to select a topic.

## Step 2: Research the Topic

Launch a focused research investigation on the selected topic:

1. **Codebase Evidence** — search for existing implementations, patterns, or prior art related to the topic.
2. **Constraint Discovery** — identify technical constraints, platform limitations, or doctrine rules that affect the topic.
3. **Decision Record Scan** — check `docs/decision-records/` for existing ADRs that inform or constrain the topic.

## Step 3: Create Workshop Document

Create the workshop document at `.copilot-tracking/plans/{date}/{slug}/workshops/{topic-slug}.md` with these sections:

```markdown
# Workshop: {Topic Name}

**Date:** {today}
**Type:** {Type from Workshop Opportunities table}
**Priority:** {Priority from Workshop Opportunities table}
**Spec:** {slug}-spec.md

## Problem Statement

{Why this topic needs deeper design exploration — from the "Why It Matters" column}

## Current State

{What exists today in the codebase relevant to this topic, with file:line evidence}

## Options

### Option A: {Name}

{Description, trade-offs, constraints}

### Option B: {Name}

{Description, trade-offs, constraints}

### Option C: {Name} (if applicable)

{Description, trade-offs, constraints}

## Recommendation

{Which option is recommended and why}

## Decision

{Left blank for user to fill after review, or "Pending workshop discussion"}

## Impact on Specification

{How the chosen option would affect the spec's goals, acceptance criteria, or complexity score}
```

Include diagrams (Mermaid) where they clarify relationships, data flows, or state transitions.

## Step 4: Present for Review

Present the workshop document to the user. Note any findings that should feed back into the specification or that warrant an ADR.

---

> [!IMPORTANT]
> **STOP.** Present the workshop document to the user. The user decides whether to update the spec, create an ADR, or proceed to clarification.