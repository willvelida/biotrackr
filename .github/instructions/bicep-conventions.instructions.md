---
description: "Bicep IaC conventions for Biotrackr infrastructure. Use when: writing or editing Bicep files, infrastructure modules, or deployment templates."
applyTo: "**/*.bicep"
---

# Bicep Conventions

## Three-Tier Layout

- `infra/core/main.bicep` — shared resources (Log Analytics, App Insights, Cosmos DB, APIM, ACR, Key Vault)
- `infra/apps/{service}/main.bicep` — per-service resources (Container App, APIM API)
- `infra/modules/{domain}/` — reusable modules (15 modules across 9 domains)

## Parameters

- Use camelCase for parameter names
- Add `@description()` decorator on every parameter
- Add `@allowed()` for constrained values (environments, SKUs)
- Standard parameters across all templates: `location`, `baseName`, `environment`, `tags`

## Naming Conventions

- Module symbolic names: camelCase (e.g., `logAnalytics`, `appInsights`)
- Module deployment names: kebab-case strings (e.g., `'log-analytics'`, `'app-insights'`)
- Resource naming pattern: `{baseName}-{component}-{environment}`
- Storage accounts: `st${replace(baseName, '-', '')}reports${environment}` (no dashes)

## Resource References

- Use `existing` keyword for cross-reference to resources in other modules
- Never hardcode resource IDs — pass as parameters or use `existing` lookups
- Use `resourceGroup().location` only when `location` parameter is not provided

## Module Design

- One resource type per module where practical
- Output only what consuming modules need
- Use `@secure()` for secrets passed as parameters

## Linting

- No custom `bicepconfig.json` — use default Bicep linter rules
- All templates must pass `az bicep build --file {template}` without errors
- Run `what-if` preview before any deployment

## Boundaries

- All infrastructure changes require explicit review
- Never deploy without `what-if` preview
- Never modify Key Vault secret references without security review

## CI/CD Trigger Awareness

- Per-service deploy workflows primarily trigger on `infra/apps/{service}/**` and `src/Biotrackr.{Service}/**` path changes (some also include their own workflow file, e.g., `.github/workflows/deploy-{service}.yml`)
- Changes to shared modules under `infra/modules/` only trigger `deploy-core-infra.yml` (shared resources — not individual Container Apps)
- When modifying shared Bicep modules, add a trigger comment to each affected `infra/apps/{service}/main.bicep` to ensure per-service pipelines fire. Always ask for human approval before doing this.
