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

Idempotency: Before posting an evolve reminder, check this log:
  grep -q "| {PR_NUMBER} |" .copilot-tracking/harness-evolution-log.md

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
