---
title: Product
description: What Biotrackr is, who it serves, the four health data domains, and the external device integrations it depends on
ms.date: 2026-08-21
ms.topic: overview
---

Biotrackr is a personal health and fitness tracking platform for a single owner. It ingests data from wearable devices, stores it as a queryable history, and exposes that history through both a dashboard and a conversational AI agent.

## Who it serves

One person: the repository owner. Every design decision follows from that. There is no tenancy model, no user table, and no per-user partitioning. Data is partitioned by date, not by identity. Anything that looks like it should be multi-user is not, and adding a user dimension is an architecture change, not a feature.

The single-owner assumption is why the platform can afford a serverless Cosmos DB account, a consumption-tier API Management instance, and an agent that is allowed to read the entire dataset without row-level authorisation.

## Data domains

Four domains, each with an ingestion service and a query API:

| Domain   | Captures                                                    | Source   |
|----------|-------------------------------------------------------------|----------|
| Activity | Daily activity summaries, steps, distance, active minutes   | Fitbit   |
| Sleep    | Sleep sessions, stages, duration, efficiency                | Fitbit   |
| Food     | Nutrition logs, calories, macronutrients                    | Fitbit   |
| Vitals   | Weight, body composition, blood pressure                    | Withings |

The domains are deliberately parallel. An Activity change usually has a Sleep, Food, and Vitals equivalent, which is why cross-service changes are a named workflow rather than an exception.

## External integrations

Fitbit supplies activity, sleep, and food. Withings supplies vitals. Both use OAuth, and both refresh tokens expire. Token lifecycle for both providers is owned by a single service, `Biotrackr.Auth.Svc`, which refreshes tokens on a schedule and writes them to Key Vault. No ingestion service refreshes its own token.

That centralisation matters: if a domain service starts failing with authorisation errors, the fault is almost always in the token refresh path, not in the domain service.

## AI capabilities

Two AI surfaces sit on top of the stored history:

* A conversational agent that answers natural language questions about the data by calling MCP tools against the domain APIs.
* A reporting service that generates periodic health summaries and emails them.

Neither surface gives medical advice. Scope boundaries for health advice are recorded in `docs/decision-records/2026-04-08-health-advice-scope-boundary.md`, and reports carry mandatory disclaimers. Treat that boundary as a product constraint rather than a prompt-tuning preference.

## Related documents

* [architecture.md](architecture.md) for the service topology
* [ai-architecture.md](ai-architecture.md) for how the agent and reporting surfaces are built
* [security.md](security.md) for the agentic security controls that constrain them
