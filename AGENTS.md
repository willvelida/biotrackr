<!-- markdownlint-disable-file -->
<!-- Surface: Codex, Claude Code, Copilot CLI, and VS Code agent mode. The Copilot
     surfaces that cannot read this file are served by .github/copilot-instructions.md.
     Both load on every turn, so keep them disjoint. Overlap is paid for twice. -->

Biotrackr ingests Fitbit and Withings data across activity, sleep, nutrition, and vitals, then answers questions about it through an AI agent. Fourteen .NET services deploy independently to Azure Container Apps.

## Bootstrap

```bash
bash scripts/init.sh
```

Installs the git hooks, checks the harness manifest, and restores every service. Run once per clone.

## Build and test

There is no root solution. Each service builds from its own directory.

```bash
cd src/Biotrackr.{Domain}.{Type}
dotnet build --no-restore
dotnet test --no-build
```

Verify only the services a change touched:

```bash
bash scripts/verify.sh Biotrackr.Activity.Api
```

Coverage runs from the service directory against that service's own settings file:

```bash
dotnet test --no-build --collect:"XPlat Code Coverage" --settings coverage.runsettings
```

`../coverage.runsettings` points at a file that does not exist. The run still succeeds and still reports a number, but the number is wrong. See [docs/testing.md](docs/testing.md).

Commit format, sign-off, and branch naming are enforced by `.githooks/commit-msg`. Read [docs/standards/commit-standards.md](docs/standards/commit-standards.md) when the hook rejects a message, not before.

## Review feedback promotion

When the same review feedback lands twice, encode it as a mechanical check: an instruction file rule, a test, or a hook. Do not explain it in chat a third time.

## Routing map

| Path | Topics |
|---|---|
| [docs/README.md](docs/README.md) | Documentation index and scope rules |
| [docs/product.md](docs/product.md) | Purpose, users, data domains, device integrations |
| [docs/architecture.md](docs/architecture.md) | Service topology, service types, APIM routing, storage |
| [docs/ai-architecture.md](docs/ai-architecture.md) | Agent, MCP server, reporting pipeline, middleware order |
| [docs/security.md](docs/security.md) | Agentic controls, identity, gateway enforcement, secrets |
| [docs/testing.md](docs/testing.md) | Test tiers, fixtures, coverage policy and commands |
| [docs/development.md](docs/development.md) | Local setup, emulator, running the stack, failure modes |
| [docs/infrastructure.md](docs/infrastructure.md) | Bicep layout, module domains, deployment pipeline |
| [docs/harness-guide.md](docs/harness-guide.md) | Agent, prompt, skill, and instruction inventories |
| [docs/quality-score.md](docs/quality-score.md) | Grade tracking and the Simplification Log |
| [docs/standards/commit-standards.md](docs/standards/commit-standards.md) | Commit format, scopes, sign-off, AI trailers |
| [docs/standards/harness-governance.md](docs/standards/harness-governance.md) | Complexity rubric, size budgets, measurement |
| [docs/decision-records/](docs/decision-records/) | Architecture Decision Records, append-only |

## Startup workflow

1. Read this file. Read nothing else until the task is scoped.
2. Changing code inside a service: read [docs/architecture.md](docs/architecture.md).
3. Touching the agent, MCP server, or reporting pipeline: read [docs/ai-architecture.md](docs/ai-architecture.md).
4. Writing or changing tests: read [docs/testing.md](docs/testing.md).
5. Touching auth, secrets, tool policy, or anything an agent can invoke: read [docs/security.md](docs/security.md).
6. Working in `infra/`: read [docs/infrastructure.md](docs/infrastructure.md).
7. Local setup failing: read [docs/development.md](docs/development.md).
8. Scoring complexity or planning multi-session work: read [docs/standards/harness-governance.md](docs/standards/harness-governance.md).
9. Making a decision that outlives the change: read [docs/decision-records/](docs/decision-records/), then add one.

Edit-time conventions live in `.github/instructions/`. VS Code loads them on its own when a matching file is opened, so do not read them ahead of time there. Codex and Cursor do not implement `applyTo` and never load them, so on those surfaces read the file matching what you are editing.

## Boundary rules

### Never

* Commit secrets, credentials, or connection strings. (why: unrecoverable once pushed, and the rotation cost is external; the gitleaks pre-commit scan is a net, not a guarantee. remove when: never)
* Push to `main`, force-push a shared branch, or merge a pull request yourself. (why: CI is the only thing proving all fourteen services still build, and a human owns the merge. remove when: branch protection enforces it server-side)
* Weaken, disable, or remove an ASI01 to ASI10 control. (source: [docs/security.md](docs/security.md). why: each maps to a demonstrated agentic attack and fails silently. remove when: replaced by a stronger control in that same document)
* Modify Key Vault references, managed identity configuration, or the prompts under `scripts/`. (why: those prompts are Key Vault backed and constrain agent behaviour, so changing them is a security review rather than a code change. remove when: never)
* Change a Cosmos DB partition key or container structure. (why: not reversible in place, and existing documents become unreachable rather than erroring. remove when: a migration path exists and is recorded as an ADR)
* Delete files or data without explicit confirmation. (why: an agent cannot tell in-progress work from dead code. remove when: never)

### Ask first

* Refactors spanning more than one service. (why: each service ships on its own pipeline, so one shared change can break fourteen builds at once. remove when: a single command builds and tests all fourteen in CI on every PR)
* New NuGet dependencies. (why: licensing and supply chain risk are owner decisions. remove when: an automated licence and CVE gate runs on restore)
* Middleware order in the Chat API. (source: [docs/ai-architecture.md](docs/ai-architecture.md). why: the order is load-bearing and reordering it fails at runtime, not at build. remove when: a contract test asserts the order)
* MCP tool schema changes. (why: every consuming agent binds to the schema, and breakage surfaces as wrong answers rather than errors. remove when: the schema is versioned and a contract test covers each tool)
* API route changes. (why: APIM configuration deploys separately and drifts out of sync. remove when: routes and APIM policy deploy from one source)
* Anything under `.github/workflows/` or `infra/`. (why: both deploy on merge, so a mistake reaches a live environment before review catches it. remove when: never, while merge triggers deployment)
* Cosmos DB document schema changes. (why: documents already written are never migrated. remove when: a migration path exists and is recorded as an ADR)
* Adding or removing a service. (why: fourteen is a load-bearing number across CI, infrastructure, and the docs. remove when: the count is derived at build time rather than hand-maintained)

