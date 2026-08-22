---
name: harness-health
description: "Agent drifts, ignores conventions, or misses stale guidance. Audits Biotrackr harness health across four dimensions."
---

# Harness Health Audit

Assess Biotrackr's harness engineering infrastructure across four dimensions. Inspect the actual files, pipelines, and configurations rather than assuming correctness.

This is the inferential audit. It answers questions a script cannot: whether guidance is still accurate, whether coverage is meaningful, whether an agent following the harness would reach the right conclusion.

## Start From the Deterministic Result

Run the structural audit first and treat its output as given:

```bash
bash scripts/audit-harness.sh
```

That script measures context budgets, instruction file sizes, duplication between always-loaded files, colliding `applyTo` globs, orphaned artifacts, and inventory count drift. Do not re-derive any of those findings by hand. Carry them into Dimension 1 as established facts and spend the review on judgement calls the script cannot make.

## Audit Protocol

Work through each dimension sequentially. For every check, read the relevant file or configuration and assess its current state. Each dimension reports a table:

| Status | Finding | Recommended Fix |
|--------|---------|-----------------|
| PASS/WARN/FAIL | Specific finding | File and action to take |

## Dimension 1: Feedforward Guides

Evaluate the quality and currency of the context files that guide agent behaviour.

1. Ambient context accuracy: read [docs/architecture.md](../../../docs/architecture.md) and verify the Services table lists all 14 services with correct types and purposes. Confirm the build and test commands in `AGENTS.md` match the actual project structure.
2. Instruction file coverage: verify every file under `.github/instructions/` has an `applyTo` glob that matches real paths in the repository, and that each glob still describes the concern the file covers.
3. Skill currency: spot-check three skills from `.github/skills/` and verify they reference current framework versions and OWASP revision dates.
4. Agent verification steps: check each agent under `.github/agents/` for a `## Verification Protocol` section. Flag agents that modify files without one.

## Dimension 2: Feedback Sensors

Evaluate CI/CD pipeline health, coverage enforcement, and automated drift detection.

1. CI pipeline health: list workflow files under `.github/workflows/` and verify each of the 14 services has a corresponding CI pipeline. Check for recent failures if pipeline status is accessible.
2. Coverage thresholds: verify `coverage.runsettings` exists for each service under `src/` and confirms the 70% minimum threshold.
3. Agentic workflow schedules: check `.github/workflows/` for agentic workflow `.md` files with `schedule:` in their frontmatter. Verify each has a corresponding compiled `.lock.yml`. Flag WARN where one is missing.
4. Documentation drift detection: check whether a doc-drift or staleness detection workflow exists. Flag if missing.
5. SDD measurement health: read `.copilot-tracking/harness-evolution-log.md` and verify the table has 14 columns including Verdict, FixCycles, FindDensity, CycleTime, SpecClarity, and FlowState. Flag WARN if the last five rows all carry `—` for measurement columns. Verify `docs/standards/harness-governance.md` contains the Framework Alignment table mapping QITE to SPACE and DORA; flag FAIL if missing. If 15 or more measured rows exist, check whether directional trends are reportable across Quality, Iteration, and Efficiency.

## Dimension 3: Architecture Fitness

Evaluate structural integrity of the codebase and infrastructure.

1. Cross-service dependency isolation: verify no service project under `src/` references another service's project. Check `.csproj` files for cross-service `ProjectReference` entries.
2. Bicep module health: list `.bicep` files under `infra/` and verify the three-tier layout of core, apps, and modules is intact. Check for modules using hardcoded values instead of parameters.
3. Security scanning: verify a CodeQL workflow exists under `.github/workflows/` and that dependency review or Dependabot configuration is present.

## Dimension 4: Behaviour

Evaluate runtime correctness through test infrastructure and tool health.

1. E2E test infrastructure: verify `cosmos-emulator.ps1` and `docker-compose.cosmos.yml` exist at the repository root, and that at least one service has an `*.IntegrationTests` project with E2E tests.
2. Contract test coverage: verify each service with an `*.IntegrationTests` project contains a `Contract/` directory with startup and DI registration tests.
3. MCP Server tools: read `src/Biotrackr.Mcp.Server/` and verify tool definitions exist for all four domains with three methods each.
4. Test tier separation: verify test projects use `[Collection]` attributes to separate unit, contract, and E2E execution.

## Final Summary

After completing all four dimensions, produce a summary:

| Dimension | PASS | WARN | FAIL | Overall |
|-----------|------|------|------|---------|
| Feedforward Guides | count | count | count | PASS/WARN/FAIL |
| Feedback Sensors | count | count | count | PASS/WARN/FAIL |
| Architecture Fitness | count | count | count | PASS/WARN/FAIL |
| Behaviour | count | count | count | PASS/WARN/FAIL |

List the top three priority items to address, ordered by impact. State which came from the deterministic script and which came from inspection, so the reader knows what a re-run of the script will and will not confirm.
