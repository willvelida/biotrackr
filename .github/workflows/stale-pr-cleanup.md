---
on:
  schedule: weekly
permissions:
  contents: read
  pull-requests: read
  actions: read
  discussions: read
  issues: read
  security-events: read
engine: copilot
timeout-minutes: 15
rate-limit:
  max: 1
  window: 180
safe-outputs:
  add-comment:
    max: 10
    target: "*"
tools:
  github:
    toolsets: [all]
---

# Stale PR Cleanup

Review open pull requests for staleness and post advisory comments.

## Rules

1. **List open PRs**: Use the github toolset to list all open pull requests in this repository.
2. **Skip protected PRs**: Never process PRs with labels `do-not-close` or `work-in-progress`. Skip draft PRs entirely — they are expected to be long-lived.
3. **Identify stale PRs (14+ days)**: Find PRs with no activity (commits, comments, or reviews) for 14 or more days. Post a polite reminder comment asking the author for a status update. Include the last activity date and a summary of the staleness assessment.
4. **Identify very stale PRs (30+ days)**: For PRs with no activity for 30 or more days, post a comment noting the PR has been inactive for over 30 days and recommending the `stale` label be added. Include a suggestion to close if the work is no longer needed.

## Important

- **Do NOT auto-close PRs** — advisory comments only. This aligns with the repository boundary rule against automated actions on shared resources.
- **Do NOT add labels** — only post comments. Mention recommended labels in comment text instead.
- Maximum 10 comments per run to avoid notification spam. Prioritize very stale PRs (30+ days) over stale PRs (14+ days) if the limit would be exceeded.
- Always include the last activity date in reminder comments.
- Include instructions for how to dismiss the stale notice (e.g., push a commit or post a comment).

If no stale PRs are found, call `noop`.
