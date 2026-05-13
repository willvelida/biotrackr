---
description: "Audit the harness engineering health of Biotrackr's agent infrastructure"
---

# Harness Health Audit

Perform a comprehensive health audit of Biotrackr's harness engineering infrastructure across four dimensions. Inspect the actual files, pipelines, and configurations rather than assuming correctness.

## Audit Protocol

Work through each dimension sequentially. For every check, read the relevant file or configuration and assess its current state.

---

## Dimension 1: Feedforward Guides

Evaluate the quality and currency of all feedforward context files that guide agent behaviour.

### Checks

1. **Ambient context accuracy**: Read `.github/copilot-instructions.md` and verify the Architecture table lists all 14 services with correct types and purposes. Confirm build/test commands match actual project structure.
2. **Instruction file coverage**: Verify all 8 `.instructions.md` files exist under `.github/instructions/` and their `applyTo` globs match actual file paths in the repository:
   - `csharp-conventions.instructions.md` → `**/*.cs`
   - `testing-conventions.instructions.md` → `**/*Tests*/**/*.cs`
   - `bicep-conventions.instructions.md` → `**/*.bicep`
   - `css-conventions.instructions.md` → `**/*.razor.css`
   - `razor-components.instructions.md` → `**/*.razor`
   - `dsa-awareness.instructions.md` → `**/*.cs`
   - `github-actions-conventions.instructions.md` → `**/*.yml`
   - `cosmos-conventions.instructions.md` → `**/*Repository*.cs,**/*Document*.cs,**/*Cosmos*.cs`
3. **Skill currency**: Spot-check 3 skills from `.github/skills/` and verify they reference current framework versions and OWASP revision dates.
4. **Agent verification steps**: Check each agent under `.github/agents/` for embedded verification steps (build, test, or validation commands). Flag agents without verification loops.

### Output

| Status | Finding | Recommended Fix |
|--------|---------|-----------------|
| PASS/WARN/FAIL | Specific finding | File and action to take |

---

## Dimension 2: Feedback Sensors

Evaluate CI/CD pipeline health, coverage enforcement, and automated drift detection.

### Checks

1. **CI pipeline health**: List all workflow files under `.github/workflows/` and verify each of the 14 services has a corresponding CI pipeline. Check for recent failures if pipeline status is accessible.
2. **Coverage thresholds**: Verify `coverage.runsettings` exists in each service directory under `src/` and confirms the 70% minimum threshold.
3. **Agentic workflow schedules**: Check `.github/workflows/` for agentic workflow `.md` files with `schedule:` in their frontmatter (e.g., `schedule: daily`, `schedule: weekly`). Verify each has a corresponding compiled `.lock.yml` file. Flag WARN if a `.md` exists without its `.lock.yml` counterpart.
4. **Documentation drift detection**: Check whether a doc-drift or staleness detection workflow exists. Flag if missing.
5. **SDD measurement health**: Check that the SDD workflow captures measurement data:
   - Read `.copilot-tracking/harness-evolution-log.md` and verify the table has 14 columns (including Verdict, FixCycles, FindDensity, CycleTime, SpecClarity, FlowState). Flag WARN if measurement columns are missing.
   - Check recent rows (last 5) for measurement column values. Flag WARN if all recent rows have `—` for measurement columns (indicates measurement is not being captured).
   - Verify `docs/standards/harness-governance.md` contains the `### Framework Alignment` table mapping QITE to SPACE/DORA. Flag FAIL if missing.
   - If ≥15 rows with measurement data exist, check whether directional trends (↑↓→) are reportable across Quality, Iteration, and Efficiency dimensions. Flag WARN if data exists but no trend analysis has been performed.

### Output

| Status | Finding | Recommended Fix |
|--------|---------|-----------------|
| PASS/WARN/FAIL | Specific finding | File and action to take |

---

## Dimension 3: Architecture Fitness

Evaluate structural integrity of the codebase and infrastructure.

### Checks

1. **Cross-service dependency isolation**: Verify no service project under `src/` directly references another service's project (each service should be independently deployable). Check `.csproj` files for cross-service `ProjectReference` entries.
2. **Bicep module health**: List all `.bicep` files under `infra/modules/` and verify the three-tier layout (core, apps, modules) is intact. Check for any modules referencing hardcoded values instead of parameters.
3. **Security scanning**: Verify CodeQL workflow exists under `.github/workflows/`. Check for dependency review or Dependabot configuration (`.github/dependabot.yml`).

### Output

| Status | Finding | Recommended Fix |
|--------|---------|-----------------|
| PASS/WARN/FAIL | Specific finding | File and action to take |

---

## Dimension 4: Behaviour

Evaluate runtime correctness through test infrastructure and tool health.

### Checks

1. **E2E test infrastructure**: Verify `cosmos-emulator.ps1` exists at the repository root and `docker-compose.cosmos.yml` is present. Check that at least one service has an `*.IntegrationTests` project with E2E tests.
2. **Contract test coverage**: Verify each service with an `*.IntegrationTests` project contains a `Contract/` directory with startup and DI registration tests.
3. **MCP Server tools**: Read the MCP Server project under `src/Biotrackr.Mcp.Server/` and verify tool definitions exist for all 4 domains (Activity, Food, Sleep, Weight) with 3 methods each (ByDate, ByDateRange, Records).
4. **Test tier separation**: Verify test projects use `[Collection]` attributes to separate unit, contract, and E2E test execution.

### Output

| Status | Finding | Recommended Fix |
|--------|---------|-----------------|
| PASS/WARN/FAIL | Specific finding | File and action to take |

---

## Final Summary

After completing all four dimensions, produce a summary:

| Dimension | PASS | WARN | FAIL | Overall |
|-----------|------|------|------|---------|
| Feedforward Guides | count | count | count | PASS/WARN/FAIL |
| Feedback Sensors | count | count | count | PASS/WARN/FAIL |
| Architecture Fitness | count | count | count | PASS/WARN/FAIL |
| Behaviour | count | count | count | PASS/WARN/FAIL |

List the top 3 priority items to address, ordered by impact.
