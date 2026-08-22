---
title: AI Architecture
description: How the Biotrackr conversational agent, MCP tool server, and reporting pipeline are built, including the middleware pipeline order and MCP tool naming rules
ms.date: 2026-08-21
ms.topic: concept
---

Three services make up the AI surface: `Biotrackr.Chat.Api` runs the conversational agent, `Biotrackr.Mcp.Server` exposes the health data as tools, and `Biotrackr.Reporting.Api` generates written reports. All three are constrained by the controls in [security.md](security.md).

## Chat API

The agent runs on Claude (Anthropic) through the Microsoft Agent Framework. Responses stream to the client over AGUI protocol streaming on HTTP server-sent events.

The agent is rebuilt per request rather than cached as a singleton. That is deliberate: the MCP tool set can change between requests, and a cached agent would keep a stale tool schema. Do not "optimise" this into a singleton without changing how tool discovery works.

### Middleware pipeline order

Three middleware components run in a fixed order:

1. `ToolPolicyMiddleware` caps tool calls at 20 per session.
2. `ConversationPersistenceMiddleware` reads and writes conversation state in Cosmos DB.
3. `GracefulDegradationMiddleware` catches Claude API errors and returns a usable response.

The order is load-bearing. Tool policy has to reject before persistence records anything, and graceful degradation has to sit outermost so it can catch failures from both of the others. Reordering them changes what gets persisted on a failed turn and whether a tool-limit breach is recorded as conversation history.

Changing this order is an ASK FIRST item in the boundary rules.

### Conversation storage

Conversations live in the `conversations` container in Cosmos DB, partitioned by `/sessionId`, with a 90-day TTL set at the container level rather than per document. Deleting a conversation is a real delete; expiry is automatic and needs no cleanup job.

Context is bounded on read: at most 50 messages are hydrated, with a 10,000-character limit and a hard cap of 100 messages. These limits are the memory-poisoning control (ASI06), not a performance tuning knob.

## MCP Server

Twelve tools: four domains multiplied by three access patterns (by date, by date range, and records). Transport is HTTP stateless. Rate limiting is 100 requests per minute per IP with a queue depth of 10, and requests are authenticated with an API key that is redacted before telemetry is written.

### Tool names are snake_case, not the C# method name

The MCP SDK converts C# PascalCase method names to snake_case when it publishes the tool schema. A method declared as `GetActivityByDateRange` is called by the model, and by `McpClient.CallToolAsync()`, as:

```text
get_activity_by_date_range
```

Calling it by its C# name fails at runtime with a tool-not-found error that names the method you passed, which reads like the tool is missing rather than misnamed. This catches people writing integration tests and anything that invokes tools programmatically.

Tool schema changes are an ASK FIRST item because every consuming agent binds to the published names.

### Tool caching

`CachingMcpToolWrapper` wraps every tool. Cache keys combine the tool name with the contextual parameters, and the time to live varies by tool type. A tool that returns stale data after an ingestion run is a cache-TTL question before it is a data question.

## Reporting API

Report generation runs in the background against a Copilot sidecar container, not in the request thread. The pipeline is:

1. Accept the request and validate it against the prompt injection blocklist. Task messages are capped at 5,000 characters.
2. Generate analysis code and scan it before execution. `ValidateGeneratedCode` rejects `os.system`, `subprocess`, `socket`, `eval`, and `exec`.
3. Execute in the sidecar, bounded by a 10-minute timeout and a maximum of three concurrent jobs.
4. Scan produced artifacts and reject anything over 50 MB.
5. Pass the draft to an independent reviewer agent that validates claims against the source data and flags concerns.
6. Return a SAS URL valid for 24 hours.

The reviewer agent runs with a fresh context and does not see the generation transcript. That separation is the point: a model asked to check its own work argues for it.

`ReportGenerationEnabled` is a kill switch. It disables the whole pipeline without a deployment.

The API also speaks the A2A protocol, and `Biotrackr.Reporting.Svc` is its scheduled caller rather than a separate implementation.

## Inter-service authentication

Chat API does not call downstream services with its managed identity token directly. It exchanges the managed identity for a federated credential and then for an agent identity token, and `AgentIdentityTokenHandler` attaches that as a bearer token. Downstream APIM policies validate the `azp` claim against the `ChatApiAgent` policy.

An inter-service call that returns 401 with a valid-looking token is usually an `azp` mismatch, not an expired token.

## Related documents

* [security.md](security.md) for the full ASI01 to ASI10 control matrix
* [architecture.md](architecture.md) for how these services sit in the wider topology
* `docs/decision-records/2026-04-08-health-advice-scope-boundary.md` for what the agent may and may not say
