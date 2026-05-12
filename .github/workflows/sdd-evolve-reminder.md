---
on:
  pull_request:
    types: [closed]
engine:
  id: copilot
permissions:
  contents: read
  pull-requests: read
rate-limit:
  max: 3
  window: 60
safe-outputs:
  add-comment:
    max: 1
    target: "triggering"
timeout-minutes: 10
---

# SDD Evolve Reminder

After a PR is closed, check if it was merged with associated SDD plan artifacts and remind the developer to run the harness-evolve phase.

## Process

### 1. Verify Merge

Check if the PR was actually merged by reading `github.event.pull_request.merged`. If `false`, the PR was closed without merging. Call `noop` with message "PR closed without merge".

### 2. Check for SDD Artifacts

Search for SDD plan artifacts associated with this PR using two methods:

**Method A (Changed files):** Read the PR's changed files list. Look for files matching:
- `.copilot-tracking/plans/**/*-spec.md` (Phase 2 output)
- `.copilot-tracking/plans/**/*-plan.md` or `*-plan.instructions.md` (Phase 4 output)
- `.copilot-tracking/plans/**/reviews/review.md` (Phase 6 output)

**Method B (Labels):** Check if the PR has the `sdd-plan` label (added by the SDD Compliance Checker workflow).

If neither method finds SDD artifacts, call `noop` with message "No SDD artifacts found".

### 3. Check Idempotency

If SDD artifacts exist, check if harness-evolve was already run for this PR:

- Read `.copilot-tracking/harness-evolution-log.md` from the default branch
- Search for a row containing the current PR number
- If a matching entry exists, call `noop` with message "Evolution already logged for this PR"

If the file does not exist, proceed. This is expected for first-time use.

### 4. Post Reminder Comment

Post a single advisory comment on the merged PR:

> **SDD Evolve Reminder**
>
> This PR included SDD plan artifacts. Consider running Phase 7 (Evolve) to encode learnings from this implementation cycle into the agent harness.
>
> **How to run:**
> 1. Open VS Code Copilot Chat
> 2. Run the `sdd-7-evolve` prompt referencing this PR's plan artifacts
> 3. Review and approve any proposed harness updates
>
> This is an advisory reminder. No action is required if the implementation did not produce noteworthy learnings.

Include the list of SDD artifact files detected in the PR.

## Edge Cases

- If the PR has no changed files (empty merge), call `noop`
- If the evolution log exists but is malformed, treat as "no prior evolution" and post the reminder
- If the PR has both `sdd-plan` and `sdd-exempt` labels, prefer `sdd-plan` and post the reminder
