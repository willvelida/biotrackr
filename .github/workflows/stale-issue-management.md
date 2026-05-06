---
on:
  schedule: weekly
  workflow_dispatch:
engine:
  id: copilot
permissions:
  contents: read
  issues: read
safe-outputs:
  add-comment:
    max: 5
  add-labels:
    allowed: [stale]
    max: 5
  close-issue:
    max: 3
    required-labels: [stale]
    state-reason: not_planned
timeout-minutes: 15
---

# Stale Issue Management

Review open issues for staleness and manage their lifecycle.

## Rules

1. **Identify stale issues**: Find issues with no activity (comments, label changes, or references) in the last 60 days.
2. **Exclude protected issues**: Never close issues with labels `security`, `bug`, `enhancement`, or `ai-agent`.
3. **First warning**: For newly stale issues, add the `stale` label and post a comment asking if the issue is still relevant.
4. **Close after warning**: For issues that already have the `stale` label and have had no activity for another 14 days, close with a polite comment explaining why.
5. **Never close manually-assigned issues**: If an issue has an assignee, only add the `stale` label and comment — do not close.

## Important

- Maximum 3 closures per run to avoid mass-closing issues
- Always explain why an issue is being marked stale or closed
- Include instructions for reopening if the issue is still relevant

If no stale issues are found, call `noop`.
