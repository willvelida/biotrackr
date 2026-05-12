---
on:
  pull_request:
    types: [opened, synchronize]
engine:
  id: copilot
permissions:
  contents: read
  pull-requests: read
rate-limit:
  max: 5
  window: 60
safe-outputs:
  add-comment:
    max: 1
    target: "triggering"
  add-labels:
    allowed: [sdd-plan, sdd-exempt]
    max: 2
timeout-minutes: 10
---

# SDD Compliance Checker

Analyze the pull request to determine if it should have an SDD plan and whether the plan artifacts are complete. This is an advisory check that does not block merging.

## Process

### 1. Estimate Change Complexity

Read the PR diff and classify the change complexity:

| Score | Label | Criteria |
|-------|-------|----------|
| CS-1 | Trivial | Single-file typo fix, dependency bump, config tweak, README update |
| CS-2 | Simple | Bug fix or small enhancement within one service, fewer than 50 lines of logic changed |
| CS-3 | Moderate | New endpoint, new component, or changes spanning 2-3 files with non-trivial logic |
| CS-4 | Complex | Cross-service change, new service feature, schema change, new infrastructure module |
| CS-5 | Major | New service, architectural change, breaking API change, new AI agent capability |

Count the number of files changed, lines added/removed, and services affected. Changes spanning 2+ services automatically score CS-4 or higher.

### 2. Check for SDD Artifacts

Search the PR's changed files for SDD plan artifacts:

- `.copilot-tracking/plans/**/*-spec.md` (Phase 2: Specify)
- `.copilot-tracking/plans/**/*-plan.md` or `*-plan.instructions.md` (Phase 4: Architect)
- `.copilot-tracking/plans/**/*-review.md` (Phase 6: Review)
- `.copilot-tracking/research/` files referencing the feature (Phase 1: Explore)

Also check the PR description for references to SDD plans.

### 3. Determine Compliance

| Complexity | SDD Required? | Action When Missing |
|------------|---------------|---------------------|
| CS-1 / CS-2 | No | Add `sdd-exempt` label, post brief informational note |
| CS-3 | Recommended | Post advisory comment suggesting SDD workflow |
| CS-4 / CS-5 | Strongly recommended | Post advisory comment with SDD getting-started steps |

If SDD artifacts are found at any complexity level, add `sdd-plan` label.

### 4. Validate Plan Completeness (If Plan Exists)

If SDD artifacts are found, assess which phases have artifacts:

- Phase 1 (Explore): Research dossier in `.copilot-tracking/research/`
- Phase 2 (Specify): Spec document (`*-spec.md`)
- Phase 4 (Architect): Plan document (`*-plan.md`)
- Phase 6 (Review): Review document with verdict

Missing phases are reported as informational notes, not failures.

### 5. Post Comment

Post a single structured comment on the PR with the estimated complexity, SDD status, and plan completeness (if applicable). Add the appropriate label (`sdd-plan` or `sdd-exempt`).

If the PR only changes files in `.copilot-tracking/` (meta-changes to plans or research), classify as CS-1 and add `sdd-exempt`. Dependabot PRs are always CS-1 with `sdd-exempt`.

If no labels or comments apply, call `noop`.
