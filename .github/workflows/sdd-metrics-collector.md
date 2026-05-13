---
on:
  schedule: weekly on monday
engine:
  id: copilot
permissions:
  contents: read
  issues: write
safe-outputs:
  create-issue:
    title-prefix: "[sdd-metrics] "
    labels: [report, sdd-metrics]
    close-older-issues: true
    max: 1
timeout-minutes: 15
---

# SDD Metrics Trend Report

Analyze the harness evolution log to produce a weekly SDD measurement trend report as a GitHub issue.

## Data Source

Read `.copilot-tracking/harness-evolution-log.md` and parse the markdown table. The table has 14 columns — the last 6 are measurement columns:

- **Verdict**: APPROVE or REQUEST_CHANGES (final review verdict)
- **FixCycles**: Number of REQUEST_CHANGES loops before APPROVE
- **FindDensity**: Review findings per task ratio
- **CycleTime**: Days from plan start to review completion
- **SpecClarity**: Self-reported spec clarity score (1-5)
- **FlowState**: Self-reported flow state score (1-5)

Rows with `—` in measurement columns predate the measurement system — skip them for trend analysis.

## Analysis

1. **Filter**: Only include rows where at least Verdict is not `—` (measured cycles).
2. **If fewer than 3 measured cycles exist**, report the raw data without trend analysis and note that trends require more data.
3. **If 3+ measured cycles exist**, compute:
   - **Quality**: Review pass rate (% APPROVE), average finding density. Trend: ↑↓→ comparing last 5 cycles vs prior 5.
   - **Iteration**: Average fix cycles, average finding density. Trend indicator.
   - **Time**: Average cycle time. Trend indicator.
   - **Efficiency**: Implied from task completion (not directly in evolution log — note as N/A).
   - **Satisfaction**: Average SpecClarity, average FlowState. Trend indicator.

## Report Format

```markdown
## SDD Measurement Trends — {date}

### Summary
{1-2 sentences: overall trajectory and key observation}

### QITE Dimension Trends

| Dimension | Key Metric | Current (last 5) | Prior (5 before) | Trend |
|-----------|-----------|-------------------|------------------|-------|
| Quality | Review pass rate | {%} | {%} | {↑↓→} |
| Quality | Avg finding density | {ratio} | {ratio} | {↑↓→} |
| Iteration | Avg fix cycles | {N} | {N} | {↑↓→} |
| Time | Avg cycle time | {days} | {days} | {↑↓→} |
| Satisfaction | Avg spec clarity | {score} | {score} | {↑↓→} |
| Satisfaction | Avg flow state | {score} | {score} | {↑↓→} |

### Observations
{2-3 actionable observations: what's improving, what's degrading, what needs attention}

### Data Coverage
- Total evolution log rows: {N}
- Measured cycles (non-dash): {N}
- Measurement window: {earliest date} → {latest date}
```

## Behavior

- If no measured cycles exist yet, call `noop` with a message noting the measurement system is new and no data is available.
- Use ↑ for improving (lower fix cycles, higher pass rate, higher satisfaction), ↓ for degrading, → for stable (< 10% change).
- Keep the report concise — aim for 1 screen length.
