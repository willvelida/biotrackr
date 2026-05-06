---
on:
  schedule: daily on weekdays
engine:
  id: copilot
permissions:
  contents: read
  issues: read
  pull-requests: read
  actions: read
safe-outputs:
  create-issue:
    title-prefix: "[daily-status] "
    labels: [report, daily-status]
    close-older-issues: true
    max: 1
timeout-minutes: 15
---

# Daily Repository Status Report

Create a comprehensive daily status report for the Biotrackr repository as a GitHub issue.

## What to include

- **Recent activity**: Issues opened/closed, PRs merged/opened in the last 24 hours
- **CI/CD health**: Recent workflow run statuses across all 14 services — highlight failures
- **Open items**: Count of open issues and PRs by label category
- **Dependabot**: Unmerged dependency update PRs and any security advisories
- **Actionable next steps**: Top 3 priorities for the maintainer today

## Style

- Use markdown tables and emoji indicators (✅ ⚠️ ❌) for status
- Keep the report concise — aim for 1 screen length
- Link to specific issues, PRs, and workflow runs where relevant

If the repository has had no activity in the last 24 hours, call `noop` with a brief confirmation.
