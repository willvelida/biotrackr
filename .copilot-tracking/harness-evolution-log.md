<!-- markdownlint-disable-file -->
# Harness Evolution Log

<!--
FORMAT DOCUMENTATION — do not remove this comment block.

This log tracks every harness-evolve execution for provenance and metrics.
Each row records one SDD cycle's learning-encoding session.

Columns:
  Date            — YYYY-MM-DD of the evolution session
  PR              — PR number (if known), used for idempotency checking via grep
  Plan            — SDD plan slug (matches .copilot-tracking/plans/{date}/{slug}/)
  Proposed        — Number of learnings proposed by the agent
  Accepted        — Number of learnings approved by the user
  Severity (C/H/M/L) — Count of Critical/High/Medium/Low learnings proposed
  Files Modified  — Harness files changed (comma-separated basenames)
  Status          — complete | partial | skipped

Idempotency: Before posting an evolve reminder, check this log.
The PR column uses #NNN format when a PR exists (e.g., #375), or — (em dash) when
the evolution occurs before a PR is created. Grep for the plan slug to check:
  grep -q "| {slug} |" .copilot-tracking/harness-evolution-log.md

Metrics derived from this table:
  Evolution frequency    = row count per time period
  Acceptance rate        = sum(Accepted) / sum(Proposed) — healthy range: 40-80%
  Severity distribution  = aggregate C/H/M/L counts over time
  Growth tracking        = Files Modified column shows which files evolve most

Example row (commented out):
  | 2026-05-12 | #375 | mcp-server-redesign | 6 | 4 | 0/2/3/1 | csharp-conventions, testing-conventions | complete |
-->

| Date | PR | Plan | Proposed | Accepted | Severity (C/H/M/L) | Files Modified | Status |
|------|----|------|----------|----------|---------------------|----------------|--------|
| 2026-05-12 | #375 | container-apps-single-revision | 1 | 1 | 0/1/0/0 | bicep-conventions.instructions.md | complete |
| 2026-05-12 | #376 | copilot-cli-slash-commands | 1 | 1 | 0/0/0/1 | sdd-conventions.instructions.md | complete |
| 2026-05-12 | #376 | sdd-additional-phases | 2 | 2 | 0/1/1/0 | sdd-conventions.instructions.md | complete |
| 2026-05-12 | #376 | review-llm-as-judge | 2 | 2 | 0/0/2/0 | sdd-conventions.instructions.md | complete |
| 2026-05-12 | — | activity-api-dead-code | 1 | 1 | 0/0/1/0 | testing-conventions.instructions.md | complete |
