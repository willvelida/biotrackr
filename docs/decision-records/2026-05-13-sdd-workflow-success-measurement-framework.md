---
title: "SDD Workflow Success Measurement Framework"
date: 2026-05-13
status: accepted
---

# Decision Record: SDD Workflow Success Measurement Framework

- **Status**: Accepted
- **Deciders**: willvelida, GitHub Copilot
- **Date**: 13 May 2026
- **Related Docs**: `.copilot-tracking/plans/2026-05-13/sdd-metrics-measurement/sdd-metrics-measurement-spec.md`, GitHub Issue [#382](https://github.com/willvelida/biotrackr/issues/382), [jakkaj/tools README](https://github.com/jakkaj/tools)

## Context

The Biotrackr SDD (Spec-Driven Development) workflow is a 12-phase pipeline that produces structured artifacts at each stage — from research dossier through specification, planning, implementation, review, and harness evolution. The workflow currently measures execution (did tasks complete? did code pass review?) but does not measure success (did the spec achieve its intended outcome? is the workflow itself improving over time?).

The jakkaj/tools repository identifies this gap explicitly: "A natural next step would be adding the missing layer: how to measure success of the spec (and each stage) using ideas drawn from SPACE, ESSP, and Accelerate — without falling into the 'story points / LoC' trap."

Biotrackr already has a lightweight measurement framework called QITE (Quality, Iteration, Time, Efficiency) in `docs/standards/harness-governance.md`, plus a 4-dimension harness health audit. These are directional but disconnected from the SDD artifact chain and lack alignment with established research frameworks (SPACE, DORA/Accelerate, DX Core 4).

Three precedent-setting decisions emerged from workshops and clarification that will outlive this feature:

1. **How measurement vocabulary relates to external frameworks** — affects all future measurement work
2. **Where measurement data lives in the SDD artifact chain** — affects every future SDD cycle
3. **Whether measurement is retrospective-only or feedforward** — affects the Architect phase permanently

## Decision

### 1. Measurement Framework: Extend QITE with SPACE Mapping (not replace)

Keep QITE (Quality, Iteration, Time, Efficiency) as the Biotrackr-native measurement vocabulary. Add a mapping table showing how each QITE dimension aligns to SPACE (Satisfaction, Performance, Activity, Communication, Efficiency), DORA (Change Lead Time, Deployment Frequency, Change Failure Rate, MTTR), and DX Core 4 (Speed, Effectiveness, Quality, Impact). Add SDD-specific metrics under each QITE dimension.

**Rationale:** QITE is only referenced in one file today, but the developer already thinks in QITE terms. A mapping table provides framework literacy without forcing vocabulary changes. This is reversible — if the project grows to multiple contributors, renaming to SPACE is a one-file change.

### 2. Measurement Data Location: Embedded in Review Report + Evolution Log Columns

The primary Cycle Measurement Summary is a new section in the review report (`reviews/review.md`), placed after `## Next Steps`. This includes a QITE-aligned metrics table, mandatory self-reported scores (spec clarity 1-5, flow state 1-5), and a "So what?" interpretation paragraph.

Complementary: add 6 measurement columns to the evolution log table (Verdict, FixCycles, FindDensity, CycleTime, SpecClarity, FlowState) for cross-cycle trend aggregation.

**Rationale:** The review phase already reads all data sources (execution log, spec, plan) needed to compute metrics. No new artifact type is needed, which avoids SDD workflow agent routing changes. Downstream consumers (Evolve, harness health audit) already read review reports.

### 3. Measurement Direction: Bidirectional (Feedback + Feedforward)

Measurement is not retrospective-only. The Architect phase (SDD Phase 4) reads historical measurement summaries from prior cycles to inform planning — flagging complexity levels with historically high rework rates, using discovery density to inform phase sizing, and surfacing prior cycle times at similar CS levels.

**Rationale:** This closes the full loop — measurement improves future planning, not just retrospective analysis. It makes the SDD workflow self-improving rather than passively instrumented.

## Consequences

### Positive

- Every SDD cycle produces a structured, comparable measurement summary without additional manual steps — metrics are byproducts of existing phase execution.
- QITE mapping to SPACE/DORA provides external framework literacy without disrupting the existing mental model.
- Mandatory self-reported scores (spec clarity, flow state) capture the "satisfaction" dimension that system metrics alone cannot provide, per SPACE framework guidance.
- Feedforward to Architect creates a self-improving loop where historical data informs future planning.
- Evolution log with 6 measurement columns enables cross-cycle trend detection in a single scannable table.

### Negative

- Evolution log table grows from 8 to 14 columns — wider markdown tables are harder to read and may wrap in narrow viewports.
- Mandatory self-reported scores add a small amount of friction to every review cycle. Forced collection risks unreliable data if the developer treats it as a checkbox exercise (SPACE warns about this).
- Feedforward in Architect adds complexity to the Architect skill/prompt and requires the phase to scan prior review reports — a new cross-cycle dependency.
- 12+ files must be updated (skills, prompts, conventions, governance, health audit, templates, agentic workflow) — high surface area for a documentation-only change.

## Alternatives Considered

### 1. Replace QITE with SPACE Dimensions Entirely

Rename the measurement section to use SPACE's five dimensions (Satisfaction, Performance, Activity, Communication, Efficiency) directly.

**Rejected because:** Breaking change to existing mental model. "Communication & Collaboration" feels forced in a solo-developer context despite reinterpretation. QITE's four dimensions are sufficient with a mapping table. The developer already thinks in QITE terms.

### 2. Separate `measurement.md` Artifact per Cycle

Create a standalone measurement file in the plan directory rather than embedding in the review report.

**Rejected because:** Introduces a new artifact type requiring SDD conventions updates and potentially workflow agent routing changes. Disconnects measurement from the review that generated the verdict, requiring cross-referencing. Adds a new step that must be remembered to execute.

### 3. Measurement as Feedback Only (No Architect Feedforward)

Keep measurement purely retrospective — capture data but don't use it to inform future planning.

**Rejected because:** Misses the key value proposition. Per the jakkaj/tools README: "The bigger picture requires treating every spec as a measurable hypothesis with explicit outcome metrics, guardrails, and a feedback loop that extends into production." Feedback-only measurement is "process theatre" without the improvement loop.

### 4. Self-Reported Metrics as Optional

Default self-reported scores to "—" and let the developer fill them in when they feel like it.

**Rejected because:** SPACE emphasizes satisfaction data is critical for understanding productivity. Optional collection produces sparse, unreliable data. The developer chose mandatory to ensure consistent data for trend analysis, accepting the small friction trade-off.

## Follow-up Actions

1. Implement the measurement layer per the SDD plan (Phase 4: Architect → Phase 5: Implement → Phase 6: Review).
2. After 15-20 SDD cycles with measurement data, assess whether the mandatory self-reported scores produce useful trends or have become a checkbox exercise — revisit the mandatory vs. prompted decision.
3. After 15-20 cycles, evaluate whether QITE vocabulary should be migrated to SPACE terminology based on whether external contributors have joined the project.
4. Monitor evolution log table width — if 14 columns proves unwieldy, consider splitting into a companion `evolution-metrics.md` file.

## Notes

- The SPACE framework paper: "The SPACE of Developer Productivity: There's more to it than you think" (Forsgren, Storey, Madilla, Zimmerman, Houck, Butler — ACM Queue 2021).
- The DX Core 4 paper: "Measuring Developer Productivity with the DX Core 4" (Noda, Tacho, Storey, Greiler — 2023) encapsulates DORA + SPACE + DevEx.
- DORA's 2024 State of DevOps Report warns that AI adoption can reduce software delivery performance — relevant for measuring AI-agent effectiveness in Biotrackr's SDD workflow.
- The term "ESSP" in the jakkaj/tools README most likely refers to the broader Engineering Strategy and Software Process research lineage rather than a single named framework.
- Workshop documents with full option analysis: `workshops/qite-to-space-mapping.md` and `workshops/cycle-measurement-summary-format.md`.
