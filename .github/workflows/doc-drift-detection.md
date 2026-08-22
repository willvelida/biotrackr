---
on:
  schedule: weekly
  workflow_dispatch:
engine:
  id: copilot
permissions:
  contents: read
safe-outputs:
  create-issue:
    title-prefix: "[doc-drift] "
    labels: [documentation, automated]
    close-older-issues: true
    max: 1
timeout-minutes: 15
---

# Documentation Drift Detection

Analyze the Biotrackr repository for documentation that has drifted from the actual codebase.

Ownership of the reference content moved in the 2026-08-21 harness refactor. `AGENTS.md` and `.github/copilot-instructions.md` no longer carry service tables, inventory tables, or a repository structure tree, and their section structure is no longer a documented figure. Do not assert counts, table contents, or heading counts against either file. Those two files are measured deterministically by `scripts/audit-harness.sh`, so Check 1 delegates to that script instead of re-deriving its logic here. Re-deriving it in this prompt would recreate exactly the coupling this workflow exists to catch.

Every other check reads its expected values from the document that now owns them, under `docs/`.

## Checks

### 1. Always-loaded context health

Run `bash scripts/audit-harness.sh` from the repository root and capture both its output and its exit code. The script measures the always-loaded context budget, the per-instruction-file budget, duplication across the always-loaded files, `applyTo` glob collisions, orphaned harness artifacts, and stale hand-maintained inventory counts.

Report every check the script marks FAIL, reproducing its WHAT, WHY, and HOW lines rather than paraphrasing them. A non-zero exit means a CRITICAL budget gate has been breached. Do not re-derive any figure the script already reports, and do not assert line counts or section counts against `AGENTS.md` or `.github/copilot-instructions.md` yourself.

If the script is missing or fails to execute, report that as the finding.

### 2. Service inventory

Compare the `src/Biotrackr.*` directories against the Services table in `docs/architecture.md`. There are 14 service directories and the table carries one row each. Flag any service present in `src/` but absent from the table, any table row with no matching directory, and any row whose Type column contradicts the project shape, given that a Domain Service has no inbound HTTP surface.

### 3. Deployed unit fan-out

Reconcile `.github/workflows/deploy-*.yml` and the `infra/apps/` directories against the "14 services, 17 deployed units" section of `docs/architecture.md`. That section records the two fan-outs: `Biotrackr.Auth.Svc` deploys as `auth-fitbit-service` and `auth-withings-service`, and `Biotrackr.Reporting.Svc` deploys as `reporting-service-weekly`, `-monthly`, and `-yearly`. Flag a deployment workflow with no `infra/apps/` counterpart, an `infra/apps/` directory with no deployment workflow, and any fan-out that has gained or lost a target without the section being updated.

### 4. Harness inventory

`docs/harness-guide.md` is the sole home of the agent, prompt, skill, and instruction inventories. Verify that every path in the File column of the three agent tables resolves to a real file under `.github/agents/`, that every prompt named in the prompt table resolves under `.github/prompts/`, and that the six skill groups account for every directory under `.github/skills/`.

The numeric claims in that file are already covered by Check 1, so report a count mismatch only if `scripts/audit-harness.sh` did not surface it.

### 5. Agentic workflow inventory

Every agentic workflow in `.github/workflows/` is a `.md` prompt body paired with a compiled `.lock.yml` sibling. There are currently 20 such pairs. Flag any `.md` body with no `.lock.yml` sibling, any `.lock.yml` with no `.md` body, and any pair where the `.md` frontmatter has changed since the lock was compiled, which needs `gh aw compile` rather than a hand edit.

No document currently states this count in prose. If one begins to, verify it against the filesystem.

### 6. Bicep module domains

Compare the subdirectories of `infra/modules/` against the Module domains table in `docs/infrastructure.md`. There are 10 domains and 19 module files. Flag a domain directory with no table row, a table row naming a module file that does not exist, and any new `.bicep` module absent from its domain row.

### 7. Agentic security controls

The ASI01 to ASI10 matrix in `docs/security.md` names the code symbol implementing each control. Verify that each named symbol still exists in `src/`, including `GenerateEndpoints`, `CopilotService`, `ValidateGeneratedCode`, `ConversationPersistenceMiddleware`, `AgentIdentityTokenHandler`, and `ReportGenerationEnabled`.

Report a renamed or deleted symbol as drift. Do not report a changed numeric threshold as drift: a threshold change is a security control change and belongs in review, not in a documentation issue.

### 8. Cross-reference resolution

Check that every relative link resolves to a file that exists, across the routing map in `AGENTS.md`, the links in `.github/copilot-instructions.md`, and the Related documents section of each file under `docs/`. A broken routing link sends an agent to a file that is not there, which is the most damaging drift in the set because it fails silently.

## Output

If drift is found, create one issue listing each discrepancy with:

- Which check found it, and what is documented against what exists
- The specific file paths that need updating, naming the document that owns the content rather than `AGENTS.md` or `.github/copilot-instructions.md`
- A suggested correction

Report findings from Check 1 using the script's own WHAT, WHY, and HOW wording, and state its exit code.

If no drift is detected, call `noop` with a confirmation message.
