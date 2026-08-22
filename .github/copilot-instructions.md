<!-- markdownlint-disable-file -->
<!-- Surface: Copilot code review, Copilot Chat on github.com, Visual Studio, JetBrains,
     Eclipse, and Xcode. None of those read AGENTS.md, and only VS Code loads
     .github/instructions/. This file carries the subset those surfaces would otherwise
     never see. VS Code loads it alongside AGENTS.md, so keep overlap deliberate: the
     overview, the commands, and the hard limits are duplicated on purpose because a
     reviewer on a blind surface needs them; everything else should live in one file
     only. Overlap beyond that is paid for twice. -->

Biotrackr is fourteen independently deployed .NET services on Azure Container Apps. They ingest Fitbit and Withings health data, serve it through domain APIs and a Blazor Server dashboard, and answer questions about it through an AI agent backed by Claude and a Model Context Protocol tool server.

## Working commands

There is no root solution. Every service builds and tests from its own directory under `src/`.

```text
dotnet build --no-restore
dotnet test --no-build
dotnet test --no-build --collect:"XPlat Code Coverage" --settings coverage.runsettings
```

Run coverage from the service directory. A leading `../` on that settings path resolves to a file that does not exist, and the run then reports a wrong number instead of failing. CI enforces a 70% line-coverage floor.

## C# conventions

* Private fields use `_camelCase`. Test classes are named `{ClassUnderTest}Should`, test methods `{Method}_Should{Behavior}_When{Condition}`.
* Prefer `ArgumentNullException.ThrowIfNull(x)`. Throw precise exception types; never the base `Exception`.
* Validate at system boundaries, not inside internal methods.
* Register stateless services, HTTP client factories, and Cosmos clients as singletons; request-bound services and repositories as scoped; lightweight disposables as transient.
* Cosmos queries use a parameterised `QueryDefinition`. Never build a query by string concatenation.
* No formatter is enforced yet, so match the surrounding file rather than reformatting it.

## API conventions

* Endpoints mount at the root: `/`, `/{date}`, `/range/{startDate}/{endDate}`. API Management prepends the domain prefix, so the service itself never declares one.
* List endpoints return `PaginationResponse<T>`.
* Dates are `yyyy-MM-dd` throughout.

## Blazor conventions

* Component parameters are PascalCase and carry `[Parameter]`.
* Component file names are PascalCase and match the component name.
* CSS lives in isolation files, uses kebab-case, and is prefixed `bt-`.

## Testing conventions

* Stack: xUnit, FluentAssertions, Moq, AutoFixture, `WebApplicationFactory`, and the Cosmos DB emulator for end-to-end tests.
* Every test follows strict Arrange, Act, Assert order with those three words as comments.
* Three tiers, selected by filter. Unit tests live in `*.UnitTests/` and run by default against mocks. Contract tests live in `*.IntegrationTests/Contract/`, select with `FullyQualifiedName~Contract`, and verify startup and DI registration with no database. End-to-end tests live in `*.IntegrationTests/E2E/`, select with `FullyQualifiedName~E2E`, and need the emulator.
* Coverage is collected on the unit tier only. 70% is the floor, 80% is healthy. Mark generated code `[ExcludeFromCodeCoverage]`.
* An assertion message an agent cannot act on is a defect. Name the expected value in the failure.

## Commit and pull request rules

Commits follow Conventional Commits with a DCO sign-off, and `.githooks/commit-msg` enforces the full rule set locally. When it rejects a message, read `docs/standards/commit-standards.md`.

* Types are `feat`, `fix`, `core`, `docs`, `refactor`, `test`. Scopes are lowercase with dashes.
* Subject line is at most 50 characters, imperative, no trailing period.
* Sign off every commit with `-s`. Bot accounts are exempt.
* Agent contributions carry the `agent`, `model`, and `contribution` trailers together or not at all.

Pull requests are validated by CI before merge: unit tests at the coverage floor, contract tests, container image build, Bicep lint and what-if, dev deployment, then end-to-end tests. Never auto-merge. A human merges after the pipeline is green.

## Infrastructure

Bicep is organised in three tiers: `infra/core/main.bicep` for shared resources, `infra/apps/{service}/main.bicep` per service, and `infra/modules/{domain}/` for reusable modules. Parameters are camelCase and carry `@description()`. Resources are named `{baseName}-{component}-{environment}`. Module symbolic names are camelCase, deployment names kebab-case.

Every Bicep change needs review and a `what-if` preview before deployment.

## Security constraints

The agent surface is governed by the OWASP Agentic Security controls ASI01 to ASI10, implemented across the Chat API, MCP Server, and Reporting API. The load-bearing ones for anyone editing this code:

* Report generation screens the incoming task for prompt injection and caps it at 5,000 characters.
* Generated Python is scanned for `os.system`, `subprocess`, `socket`, `eval`, and `exec` before it can run.
* Conversation hydration is capped at 50 messages, 10,000 characters, and 100 messages stored.
* Inter-service calls carry an agent identity token, and the receiving side validates the `azp` claim.
* Reports are validated by an independent reviewer agent and carry mandatory disclaimers.
* Report generation has a kill switch, a 10-minute timeout, at most 3 concurrent jobs, and a 50 MB artifact limit.

System prompts live in Azure Key Vault and are never committed. API keys are redacted in telemetry. All Azure access goes through the user-assigned managed identity. API Management validates JWTs and requires a subscription key on every external endpoint.

## AI subsystem

The Chat API runs Claude through the Microsoft Agent Framework and streams over AGUI on HTTP SSE, rebuilding the agent whenever the MCP tool set changes.

Its middleware runs in a fixed order, and the order is load-bearing: tool policy first, capping a session at 20 tool calls; then conversation persistence into Cosmos DB; then graceful degradation, which catches Claude API errors. Reordering it fails at runtime, not at build.

The MCP Server exposes 12 tools, four domains times three methods. The SDK converts C# PascalCase method names to snake_case, so call `get_activity_by_date_range`, not `GetActivityByDateRange`. Transport is stateless HTTP, rate limited to 100 requests per minute per IP.

Conversations are stored in the `conversations` container partitioned on `/sessionId` with a 90-day TTL. MCP tool results are cached by `CachingMcpToolWrapper`, keyed on tool name plus contextual parameters.

## Hard limits

Treat a change as needing owner sign-off, and flag it in review, when it does any of the following:

* Adds a secret, credential, or connection string to a tracked file.
* Weakens or removes one of the ASI controls described above.
* Alters Key Vault references, managed identity configuration, or the prompts under `scripts/`.
* Alters a Cosmos DB partition key, container structure, or stored document schema.
* Reorders the Chat API middleware pipeline, or changes an MCP tool schema that other agents bind to.
* Changes an API route without a matching API Management update.
* Touches `.github/workflows/` or `infra/`, both of which deploy on merge.

Architecture Decision Records under `docs/decision-records/` are append-only historical documents. Do not edit them.

## Where the rest lives

`AGENTS.md` at the repository root carries the bootstrap command, the routing map, the startup workflow, and the full boundary rules, for the tools that can read it. `docs/README.md` indexes the topic documents covering architecture, AI architecture, security, testing, local development, infrastructure, and the harness inventories. Edit-time conventions live in `.github/instructions/` and load automatically in VS Code for the paths they govern.

