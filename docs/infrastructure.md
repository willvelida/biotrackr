---
title: Infrastructure
description: Bicep three-tier layout, module domains, resource naming conventions, and the ten-stage deployment pipeline for Biotrackr services
ms.date: 2026-08-21
ms.topic: concept
---

All Azure resources are defined in Bicep under `infra/` and deployed by GitHub Actions with OIDC authentication. No resource is created by hand, and no resource is created by application code.

## Three tiers

| Tier    | Path                        | Contains                                                             |
|---------|-----------------------------|----------------------------------------------------------------------|
| Core    | `infra/core/main.bicep`     | Shared resources every service depends on                            |
| Apps    | `infra/apps/{app}/main.bicep` | One deployment per deployed unit, referencing core resources by name |
| Modules | `infra/modules/{domain}/`   | Reusable resource definitions consumed by both tiers above           |

Core deploys first and owns the identity, the App Configuration store, the Cosmos DB account, the container registry, the Container Apps environment, and the monitoring stack. App deployments reference those with the `existing` keyword rather than redeclaring them. A change to core can therefore break every app deployment, while an app change is isolated.

`infra/apps/` holds 17 directories for 14 services, because Auth deploys once per provider and Reporting once per cadence. See [architecture.md](architecture.md).

## Module domains

Modules are grouped by Azure service domain rather than by consuming service, so one module serves many apps:

| Domain           | Modules                                                                                     |
|------------------|---------------------------------------------------------------------------------------------|
| `ai/`            | Foundry                                                                                     |
| `apim/`          | Consumption-tier API Management, named values, products                                      |
| `communication/` | Communication Services email                                                                 |
| `configuration/` | App Configuration                                                                            |
| `database/`      | Serverless Cosmos DB                                                                         |
| `host/`          | Container Apps environment, HTTP app, HTTP app with sidecar, jobs, container registry        |
| `identity/`      | User-assigned managed identity                                                               |
| `monitoring/`    | Log Analytics, Application Insights, agent alerts, budget                                    |
| `security/`      | Key Vault                                                                                    |
| `storage/`       | Storage account                                                                              |

Picking the wrong host module is the common mistake. Scheduled ingestion uses `container-app-jobs.bicep`, long-running APIs use `container-app-http.bicep`, and only components that need a Copilot sidecar use `container-app-http-sidecar.bicep`.

Modules expose outputs sparingly, and by convention an output is the name of the deployed resource so a downstream module can reference it as an existing resource. Resist adding outputs that leak resource properties; that couples modules to each other rather than to names.

## Conventions

Parameters are camelCase, carry a `@description()`, and use `@allowed()` where the value set is closed. Four parameters appear almost everywhere: `location`, `baseName`, `environment`, and `tags`.

Module symbolic names are camelCase and module deployment names are kebab-case, which means the same module appears twice under two spellings in every template:

```bicep
module logAnalytics '../modules/monitoring/log-analytics.bicep' = {
  name: 'log-analytics'
  params: {
    name: logAnalyticsName
    location: location
    tags: tags
  }
}
```

Resources are named `{baseName}-{component}-{environment}`.

There is no `bicepconfig.json`, so the default linter rule set applies. Full authoring rules are in `.github/instructions/bicep-conventions.instructions.md`, which loads automatically when you edit a `.bicep` file.

## Deployment pipeline

Each service has its own workflow, and each runs the same ten stages:

1. Environment setup, resolving the .NET version.
2. Unit tests with the 70% coverage gate.
3. Contract tests, in parallel with stage 2.
4. Container image build and push to ACR.
5. Retrieve the ACR login server.
6. Lint, via `az bicep build`.
7. Validate the template against the target scope.
8. Preview, via what-if.
9. Deploy to dev with `azure/bicep-deploy@v2` and OIDC.
10. E2E tests against the deployed service with the Cosmos emulator.

The what-if preview is not decorative. Infrastructure changes are an ASK FIRST item precisely because the preview is the only place a destructive change becomes visible before it happens. Never deploy infrastructure without reading it.

## Related documents

* [architecture.md](architecture.md) for what is being deployed and why 14 services need 17 deployments
* [security.md](security.md) for the identity and secret model these templates provision
* [bicep-modules-structure.md](bicep-modules-structure.md) for the original module rationale and links to the Bicep documentation
* `.github/instructions/bicep-conventions.instructions.md` for edit-time rules
