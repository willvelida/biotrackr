---
on:
  schedule: weekly
  workflow_dispatch:
engine:
  id: copilot
permissions:
  contents: read
  actions: read
  issues: read
  pull-requests: read
safe-outputs:
  mentions: false
  allowed-github-references: []
  create-issue:
    title-prefix: "[ci-coach] "
    labels: [infrastructure, automated]
    close-older-issues: true
    max: 1
tools:
  github:
    toolsets: [default, actions]
  agentic-workflows:
timeout-minutes: 30
---

# CI Optimization Coach

Analyze the Biotrackr CI/CD deployment pipelines and reusable workflow templates for optimization opportunities. Post a ranked summary issue with specific recommendations.

{{#runtime-import .github/workflows/shared/reporting.md}}

## Scope

Focus on these workflow files:

- **Deployment pipelines**: All `.github/workflows/deploy-*.yml` files (18 pipelines)
- **Reusable templates**: All `.github/workflows/template-*.yml` files (10 templates)
- **Conventions reference**: `.github/instructions/github-actions-conventions.instructions.md`

Do NOT analyze agentic workflow `.lock.yml` files (auto-generated), security/quality workflows, or utility workflows.

## Analysis Steps

### 1. Static Analysis

Read all deployment pipeline and template YAML files. For each file, check against the conventions in `.github/instructions/github-actions-conventions.instructions.md`.

### 2. Run History Analysis

Use the `logs` tool to pull workflow run data from the last 7 days for deployment pipelines. Identify:

- Slowest pipelines and stages by execution time
- Failure patterns — which stages fail most frequently
- Retry rates — runs with `run_attempt > 1`

Use the `audit` tool to deep-dive into the 2-3 slowest or most failure-prone runs.

### 3. Convention Compliance

Check every deployment pipeline for:

- **Concurrency groups**: Must have a `concurrency` block with a group name following the pattern `deploy-{service}-{branch-ref}` and `cancel-in-progress: false`
- **Timeout values**: Must have `timeout-minutes` set on jobs (default 360 min is too high)
- **Path filter self-reference**: The workflow file itself must be in the `paths:` filter
- **Action pinning**: Third-party actions pinned to full commit SHA
- **Template reference style**: Consistent `willvelida/biotrackr/.github/workflows/template-*.yml@main` format

### 4. Optimization Detection

Check for these optimization opportunities:

#### High Impact

- **Missing NuGet caching**: Check if any test template uses `actions/cache` for `~/.nuget/packages`. Currently 3 independent `dotnet restore` calls per pipeline (48 total across all pipelines).
- **Missing Docker layer caching**: Check if container build templates use `cache-from: type=gha` / `cache-to: type=gha,mode=max`. Both templates set up Buildx but may not use its cache backend.
- **Missing concurrency groups**: Any deploy pipeline without a `concurrency` block.

#### Medium Impact

- **Missing timeout-minutes**: Any pipeline or template job without `timeout-minutes`.
- **Missing path filter self-reference**: Any deploy pipeline that doesn't include its own file in `paths:`.
- **Missing pipeline stages**: Any deploy pipeline missing the `preview` (what-if) stage.
- **Stages exceeding expected duration**: Use run history to flag stages consistently taking longer than expected.

#### Low Impact

- **Redundant ACR server lookup**: `retrieve-container-image-dev` stage duplicating a lookup already done in the build template.
- **Duplicate parameter injection**: Identical bash scripts across multiple Bicep templates.
- **`env-setup` job overhead**: Separate runner job just to propagate an environment variable.
- **Inconsistent coverage thresholds**: Different `coverage-threshold` values across pipelines.
- **Inconsistent template references**: Mixed local (`./`) vs fully qualified references.
- **Orphaned resources**: Templates not referenced by any deployment pipeline, unused workflow inputs that are defined but never consumed, and unused steps or jobs that could be removed.
- **Sequential Bicep stages**: `lint` and `validate` could potentially run in parallel.

### 5. Parallelization Gaps

Identify sequential jobs in deployment pipelines that have no data dependency and could run in parallel. Note: some sequential ordering is intentional (e.g., tests before deploy).

## Output

Create an issue with findings ranked by estimated impact.

### Report Structure

Use `###` headers (never `#` or `##`). Use `<details>` for verbose sections.

Structure:
1. **Summary** — total findings count with High/Medium/Low breakdown
2. **High Impact** — findings with the largest potential savings, each with Category, Affected files, Current state, Recommended fix, and Estimated savings
3. **Medium Impact** — convention violations and moderate optimizations
4. **Low Impact** — minor inconsistencies and cleanup opportunities
5. **Run Performance** (collapsible) — table of pipeline execution times, success rates, and slowest stages from the last 7 days
6. **Conventions Compliance** (collapsible) — table of convention checks with pass/fail status
7. **References** — up to 3 workflow run URLs in `[§{id}](url)` format

Use emoji indicators: ✅ for passing checks, ⚠️ for warnings, ❌ for violations.

If no optimization opportunities are found and all conventions pass, call `noop` with a brief confirmation that all pipelines are healthy.
