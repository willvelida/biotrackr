---
title: Security
description: OWASP Agentic Security control matrix (ASI01-ASI10), identity model, APIM enforcement, and secret handling for Biotrackr
ms.date: 2026-08-21
ms.topic: concept
---

Biotrackr implements the OWASP Agentic Security Initiative Top 10 across its three AI components. Every control below is live code, not a plan. Weakening or disabling any of them is listed under NEVER in the boundary rules.

## Agentic security controls

| Control | Category                           | Where it lives                                                                                              |
|---------|------------------------------------|-------------------------------------------------------------------------------------------------------------|
| ASI01   | Agent goal hijack                  | Prompt injection blocklist in `GenerateEndpoints`, 5,000-character cap on `taskMessage`, constrained system prompts |
| ASI02   | Tool misuse                        | Permission request logging in `CopilotService`; shell, read, and write are the only allowed tools             |
| ASI03   | Identity and privilege abuse       | Agent identity token for inter-service calls, `azp` claim validation, `ChatApiAgent` APIM policy               |
| ASI04   | Supply chain                       | Dependabot, CodeQL scanning, locked package versions                                                          |
| ASI05   | Unexpected code execution          | `ValidateGeneratedCode` rejects `os.system`, `subprocess`, `socket`, `eval`, `exec`                            |
| ASI06   | Memory and context poisoning       | `ConversationPersistenceMiddleware`: 50 hydrated messages, 10,000-character limit, 100-message cap             |
| ASI07   | Insecure inter-agent communication | `AgentIdentityTokenHandler` bearer token via managed identity, federated credential, then agent identity       |
| ASI08   | Cascading failures                 | 10-minute report timeout, maximum 3 concurrent jobs, circuit breaker on the Copilot sidecar                    |
| ASI09   | Human-agent trust exploitation     | Independent reviewer agent validates reports against source data, mandatory disclaimers, concerns surfaced     |
| ASI10   | Rogue agents                       | `ReportGenerationEnabled` kill switch, 50 MB artifact limit, job status tracking                               |

Each control has a numeric threshold in code. If you are changing a number in that table, you are changing a security control, and it needs review rather than a commit.

## Identity

All services authenticate to Azure with a single user-assigned managed identity, `uai-biotrackr-dev`. No service holds a client secret, and no connection string is stored in configuration.

Chat API layers an agent identity on top for inter-service calls. The chain runs managed identity to federated identity credential to agent identity token, and the `azp` claim on the resulting token is what APIM validates. Details of that flow are in [ai-architecture.md](ai-architecture.md).

Key Vault secret references and managed identity configuration are frozen. Both appear under NEVER in the boundary rules because a change there fails at runtime in an environment you cannot easily reproduce locally.

## Gateway enforcement

API Management is the only public ingress. It validates JWTs and requires a subscription key on every external endpoint. Services behind it do not re-validate, so a service reached directly is unauthenticated by design.

The MCP Server adds its own limit of 100 requests per minute per IP and authenticates with an API key that is redacted before any telemetry is emitted.

## Secrets

System prompts live in Azure Key Vault and are uploaded by the scripts under `scripts/`. They are not committed to the repository. The upload scripts are themselves protected: modifying them requires security review, because a prompt change is a behaviour change to a production agent with no code review trail.

OAuth tokens for Fitbit and Withings are written to Key Vault by `Biotrackr.Auth.Svc` and read from there by the ingestion jobs.

Nothing else belongs in the repository. No credentials, no API keys, no connection strings, in source or in test fixtures.

## Related documents

* [ai-architecture.md](ai-architecture.md) for the implementation of the agentic controls
* `SECURITY.md` at the repository root for vulnerability reporting
* `AI-TRANSPARENCY.md` for the public statement on models, data categories, and safety approach
* `.github/skills/agentic-vulnerabilities/SKILL.md` for the full OWASP ASI knowledge base used during audits
