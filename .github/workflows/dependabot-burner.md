---
on:
  pull_request:
    types: [opened, synchronize]
  pull_request_review:
    types: [submitted]
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
  max: 5
  window: 60
safe-outputs:
  add-comment:
    max: 3
    target: "triggering"
tools:
  github:
    toolsets: [all]
---

# Dependabot PR Triage

Analyze Dependabot pull requests and post advisory comments based on version bump severity and CI status. This workflow is advisory-only and does not approve or merge PRs.

## Instructions

1. **Verify author identity**: Check if the PR author is `dependabot[bot]`. If the PR is NOT from Dependabot, call `noop` immediately — this workflow only processes Dependabot PRs.
2. **Parse version bump type**: Read the PR title to determine if this is a major, minor, or patch version bump. Dependabot titles follow the pattern `Bump {package} from {old} to {new}`.
3. **Handle major bumps**: For major version bumps, post a comment stating that major version bumps require manual review due to potential breaking changes. Include a recommendation to check the changelog for breaking changes. Stop processing.
4. **Handle CI failures**: Check the status of all CI checks on the PR. If any required checks have failed, post a comment noting the CI failure and that the PR needs attention. Stop processing.
5. **Handle minor and patch bumps**: For minor and patch version bumps where all CI checks have passed:
   a. Check for merge conflicts with the base branch.
   b. Post a comment summarizing the dependency update: package name, version change (old → new), bump type (minor/patch), CI status, and a link to the changelog if available from the PR body.
   c. If merge conflicts exist, include a note asking Dependabot to rebase.
   d. If all checks pass and no conflicts exist, include a recommendation that the PR is ready for manual review and merge.

## Security

- **Actor verification**: Only process PRs where the author is exactly `dependabot[bot]`. Reject all other authors.
- **Advisory only**: This workflow posts comments only. It does NOT approve, merge, or enable auto-merge on any PR. All merge decisions require human action.
- **Audit trail**: Every triage decision (ready, needs review, has conflicts, CI failing) is logged via the PR comment.

## Important

- This workflow is strictly advisory. It provides triage information to help maintainers prioritize Dependabot PRs but never takes merge actions.
- If no Dependabot PRs match the trigger, call `noop`.
