---
title: Architecture
description: Service topology, service type classification, APIM routing model, data flow, and the independent-build model for the 14 Biotrackr services
ms.date: 2026-08-21
ms.topic: concept
---

Biotrackr is 14 independently deployable .NET services on Azure Container Apps, backed by a single serverless Cosmos DB account and fronted by Azure API Management.

## There is no root solution file

Each service owns its own solution file, Dockerfile, test projects, and CI workflow. No solution spans the repository, and no build command run from the repository root builds anything meaningful.

Every build and test command must start with a `cd` into a service directory:

```bash
cd src/Biotrackr.Activity.Api
dotnet build --no-restore
```

Five services use the newer `.slnx` format (Chat API, MCP Server, Reporting API, Reporting Svc, UI); the other nine use `.sln`. Tooling that assumes `.sln` will silently skip those five.

There are also no project references across service boundaries. Two services that need the same behaviour duplicate it. That is intentional, and a pull request that introduces a cross-service project reference is a boundary violation rather than a refactor.

## Services

The service name tells you the domain. The type tells you the runtime shape, and it is not derivable from the filename:

| Service                    | Type           | Purpose                                                        |
|----------------------------|----------------|----------------------------------------------------------------|
| `Biotrackr.Activity.Api`   | Domain API     | Activity data queries                                          |
| `Biotrackr.Activity.Svc`   | Domain Service | Fitbit activity data ingestion                                 |
| `Biotrackr.Auth.Svc`       | Domain Service | Fitbit and Withings OAuth token management                     |
| `Biotrackr.Chat.Api`       | AI Component   | Conversational AI agent (Claude via MAF)                       |
| `Biotrackr.Food.Api`       | Domain API     | Nutrition data queries                                         |
| `Biotrackr.Food.Svc`       | Domain Service | Fitbit food data ingestion                                     |
| `Biotrackr.Mcp.Server`     | AI Component   | Model Context Protocol tool server                             |
| `Biotrackr.Reporting.Api`  | AI Component   | AI-generated health reports with A2A protocol support          |
| `Biotrackr.Reporting.Svc`  | Domain Service | Scheduled summary cadence and email; calls into Reporting.Api  |
| `Biotrackr.Sleep.Api`      | Domain API     | Sleep data queries                                             |
| `Biotrackr.Sleep.Svc`      | Domain Service | Fitbit sleep data ingestion                                    |
| `Biotrackr.UI`             | Frontend       | Blazor Server dashboard                                        |
| `Biotrackr.Vitals.Api`     | Domain API     | Vitals data queries (weight, body composition)                 |
| `Biotrackr.Vitals.Svc`     | Domain Service | Withings vitals data ingestion                                 |

Type determines hosting and lifecycle:

* Domain APIs are long-running HTTP Container Apps behind APIM.
* Domain Services are Container Apps Jobs on a schedule. They have no inbound HTTP surface, so a change that adds an endpoint to a `.Svc` project will build and never be reachable.
* AI Components are long-running HTTP Container Apps with additional identity and sidecar requirements. See [ai-architecture.md](ai-architecture.md).
* The Frontend is a Blazor Server app holding a persistent circuit per browser session.

### 14 services, 17 deployed units

Two services fan out into multiple deployments, so the source tree and the infrastructure tree do not line up one to one:

* `Biotrackr.Auth.Svc` deploys twice, as `auth-fitbit-service` and `auth-withings-service`, one job per provider.
* `Biotrackr.Reporting.Svc` deploys three times, as `reporting-service-weekly`, `-monthly`, and `-yearly`, one job per cadence.

A change to either source project therefore affects several deployment templates and several workflows. Editing one and not the others produces a partial rollout that passes CI.

## APIM routing

Services mount their routes at the root: `/`, `/{date}`, `/range/{startDate}/{endDate}`. The domain prefix is added by API Management, not by the service.

A request for activity on a date reaches APIM as `/activity/2026-08-21` and reaches the Activity API as `/2026-08-21`. Adding `/activity` to the service route produces `/activity/activity/{date}` externally.

This is the most common source of "the endpoint works locally but 404s through the gateway" and its inverse. Route changes therefore require a matching APIM configuration change, which is why API endpoint changes sit under ASK FIRST in the boundary rules.

Other cross-cutting API conventions:

* List endpoints return `PaginationResponse<T>`.
* Dates use `yyyy-MM-dd` everywhere, in routes, documents, and query parameters.
* APIM validates JWTs and requires subscription keys. Services do not re-validate. A service reached directly is unauthenticated, which is why direct ingress is not exposed.

Route structure rationale is recorded in `docs/decision-records/2025-10-28-backend-api-route-structure.md`.

## Data flow

```text
Fitbit / Withings
      │  (OAuth tokens refreshed by Auth.Svc, stored in Key Vault)
      ▼
Domain Services (scheduled jobs)  ──writes──▶  Cosmos DB (serverless)
                                                    │
                                                reads
                                                    ▼
                                             Domain APIs
                                                    │
                                     ┌──────────────┴──────────────┐
                                     ▼                             ▼
                                   APIM ──▶ UI              MCP Server ──▶ Chat API
```

Ingestion is one-directional. Domain APIs are read-only against Cosmos DB; nothing in the query path writes health data. The AI surfaces read through the MCP Server rather than touching Cosmos DB directly, which is what keeps tool policy and rate limiting in one place.

## Storage model

A single serverless Cosmos DB account with exactly two containers, not one per domain:

| Container       | Partition key    | Holds                                                    |
|-----------------|------------------|-----------------------------------------------------------|
| `records`       | `/documentType`  | All four health domains, discriminated by `documentType`  |
| `conversations` | `/sessionId`     | Chat history, 90-day TTL applied at the container level   |

Activity, Sleep, Food, and Vitals documents all live in the same physical container. A query that omits `documentType` is a cross-partition fan-out over every domain, and a query that omits it while filtering on date will silently return other domains' documents.

Containers and partition keys are provisioned by Bicep, not by application code. Changing either is a data migration rather than a schema edit, and both are listed under NEVER in the boundary rules. Query and repository conventions live in `.github/instructions/cosmos-conventions.instructions.md`.

## Tech stack

* .NET 10.0, C# 14, ASP.NET Core minimal APIs
* Blazor Server with the Radzen component library
* Azure Container Apps for hosting, Container Apps Jobs for scheduled ingestion
* Azure Cosmos DB, serverless, NoSQL API
* Azure API Management on the consumption tier
* Azure App Configuration for centralised settings, Azure Key Vault for secrets
* Bicep for infrastructure

Configuration reaches every service through App Configuration, resolved with a user-assigned managed identity. Two environment variables drive that lookup: `azureappconfigendpoint` and `managedidentityclientid`. A service missing either will start and then fail on first configuration read, which reads as an unrelated null reference further downstream.

## Related documents

* [product.md](product.md) for what the platform is for
* [ai-architecture.md](ai-architecture.md) for the Chat, MCP, and Reporting components
* [infrastructure.md](infrastructure.md) for how these services are provisioned and deployed
* [testing.md](testing.md) for how they are verified
