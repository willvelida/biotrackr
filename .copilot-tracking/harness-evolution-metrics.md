<!-- markdownlint-disable-file -->
# Harness Runtime Metrics

<!--
FORMAT DOCUMENTATION — do not remove this comment block.

Companion to harness-evolution-log.md. That file records what each SDD cycle
DECIDED; this one records what the harness actually DID while the cycle ran.
The two are kept apart deliberately: the evolution log's column list is
restated in prose across six files with no shared definition, so widening it
means coordinating six edits and risking a silent misread. Adding a companion
costs nothing by comparison, and it is the split anticipated by follow-up
action 4 of the SDD Workflow Success Measurement Framework decision record.

Rows are appended by the promotion step, which distils the raw event store at
.copilot-tracking/observability/ into one row per ref per promotion. A batch
spanning two branches therefore produces two rows rather than one row labelled
with whichever branch happened to be checked out. The raw store is gitignored,
high-volume and disposable; this file is committed, small and reviewable. Raw
records are never committed, because the pre-commit hook writes them during the
very commit that would carry them.

Columns:
  Date      — YYYY-MM-DD the row was promoted
  Cycle     — the branch the records were recorded on. Mapping this to the SDD
              plan slug is an open question: a branch is what the runtime can
              observe, a slug is what the evolution log is keyed by, and the
              two coincide only by convention.
  Sessions  — distinct correlation ids observed in the promoted window
  Events    — total records promoted
  Pass      — records with outcome=pass
  Fail      — records with outcome=fail      (a check ran and the subject was broken)
  Error     — records with outcome=error     (a check could not run; environment)
  Skip      — records with outcome=skip      (a check was bypassed or unavailable)
  Degraded  — records with outcome=degraded  (a check ran in a reduced mode)
  Timeout   — records with outcome=timeout   (a check was killed by its budget)
  TotalMs   — summed elapsed milliseconds across the promoted records

Mapping to the QITE dimensions defined in docs/standards/harness-governance.md:
  Quality    — Pass / (Pass + Fail): how often verification succeeded
  Iteration  — Fail + Error: how much rework and environment friction occurred
  Time       — TotalMs: how long verification cost
  Efficiency — Skip + Degraded + Timeout: how often the harness ran at less
               than full strength, which is the number that decays quietly

Skip and Degraded are the point of this file. A bypassed or reduced check
leaves no trace in the evolution log — it looks identical to a check that
passed. Counting them is what makes "the gate did not really run" visible.

The header row is guarded by check C7 in scripts/audit-harness.sh, which holds
the authoritative column list. Change one and the other fails.
-->

| Date | Cycle | Sessions | Events | Pass | Fail | Error | Skip | Degraded | Timeout | TotalMs |
|------|-------|----------|--------|------|------|-------|------|----------|---------|---------|
