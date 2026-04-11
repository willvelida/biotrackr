<!-- markdownlint-disable-file -->
# Copilot Instructions

> For Blazor component conventions, see `.github/instructions/razor-components.instructions.md`. For CSS isolation conventions, see `.github/instructions/css-conventions.instructions.md`.

## Project Overview

Biotrackr is a personal health and fitness tracking platform that integrates with Fitbit and Withings devices to collect activity, sleep, nutrition, and vitals data. The platform is built as a microservices architecture deployed on Azure Container Apps, with an AI conversational agent powered by Claude (Anthropic) via the Microsoft Agent Framework for natural language health data querying and report generation.

### Architecture

The system comprises 13 independently-deployable services:

| Service | Type | Purpose |
|---------|------|---------|
| `Biotrackr.Activity.Api` | Domain API | Activity data queries |
| `Biotrackr.Activity.Svc` | Domain Service | Fitbit activity data ingestion |
| `Biotrackr.Auth.Svc` | Domain Service | Fitbit OAuth token management |
| `Biotrackr.Chat.Api` | AI Component | Conversational AI agent (Claude via MAF) |
| `Biotrackr.Food.Api` | Domain API | Nutrition data queries |
| `Biotrackr.Food.Svc` | Domain Service | Fitbit food data ingestion |
| `Biotrackr.Mcp.Server` | AI Component | Model Context Protocol tool server |
| `Biotrackr.Reporting.Api` | AI Component | AI-generated health reports |
| `Biotrackr.Sleep.Api` | Domain API | Sleep data queries |
| `Biotrackr.Sleep.Svc` | Domain Service | Fitbit sleep data ingestion |
| `Biotrackr.UI` | Frontend | Blazor Server dashboard |
| `Biotrackr.Vitals.Api` | Domain API | Vitals data queries (weight, body composition) |
| `Biotrackr.Vitals.Svc` | Domain Service | Withings vitals data ingestion |

### Tech Stack

- **.NET 10.0** / **C# 14** / **ASP.NET Core**
- **Blazor Server** with **Radzen UI** component library
- **Azure Container Apps** (hosting)
- **Azure Cosmos DB** (serverless, NoSQL)
- **Azure API Management** (gateway, JWT validation)
- **Azure App Configuration** (centralized config)
- **Bicep** (Infrastructure as Code)

Each service has its own solution file (`.sln` or `.slnx`), Dockerfile, test projects, and CI workflow. There is no root solution file — each service builds independently.

## Repository Structure

```text
├── AGENTS.md                          # Standalone agent guide (cross-tool)
├── CLAUDE.md                          # @AGENTS.md import for Claude Code
├── .github/
│   ├── copilot-instructions.md        # Comprehensive Copilot guide (14 sections)
│   ├── instructions/                  # Path-scoped .instructions.md files
│   ├── agents/                        # 6 custom agent definitions
│   ├── prompts/                       # 4 prompt templates
│   ├── skills/                        # 18 skills (OWASP, accessibility, etc.)
│   └── workflows/                     # 28 GitHub Actions workflows
├── docs/
│   ├── standards/                     # Commit standards, conventions
│   └── decision-records/              # Architecture Decision Records
├── infra/
│   ├── core/main.bicep                # Shared infrastructure
│   ├── apps/{service}/main.bicep      # Per-service infrastructure
│   └── modules/{domain}/              # 15 reusable Bicep modules
├── scripts/                           # System prompt upload, identity scripts
└── src/
    └── Biotrackr.{Domain}.{Type}/     # 13 service directories
        ├── Biotrackr.{Domain}.{Type}/ # Application project
        ├── *.UnitTests/               # Unit tests
        ├── *.IntegrationTests/        # Integration tests (optional)
        ├── Dockerfile                 # Container image definition
        └── *.sln or *.slnx           # Solution file
```

## Dev Environment Setup

### Prerequisites

- **.NET 10.0 SDK**
- **Docker Desktop** (for Cosmos DB Emulator and container builds)
- **Azure CLI** (for infrastructure deployments)
- **PowerShell 7+** (for helper scripts)
- **Azure subscription** with App Configuration and Cosmos DB (for integration/E2E tests)

### Local Cosmos DB Emulator

```bash
# Start Cosmos DB Emulator (Docker)
./cosmos-emulator.ps1 start

# Check emulator status
./cosmos-emulator.ps1 status

# Trust emulator certificate (required for HTTPS)
./cosmos-emulator.ps1 cert

# Stop emulator
./cosmos-emulator.ps1 stop
```

Docker Compose alternative:

```bash
docker compose -f docker-compose.cosmos.yml up -d
```

### Configuration

Each service connects to Azure App Configuration via the `azureappconfigendpoint` environment variable. The managed identity client ID is configured via the `managedidentityclientid` environment variable. For local development, configure both environment variables to point to your Azure App Configuration instance and identity.

## Build & Test Commands

### Per-Service Build Pattern

```bash
cd src/Biotrackr.{Domain}.{Type}
dotnet restore
dotnet build --no-restore
dotnet test --no-build --collect:"XPlat Code Coverage" --settings ../coverage.runsettings
```

### Iterative Build (skip restore when packages haven't changed)

```bash
dotnet build --no-restore -v:q
```

### Single-Project Build (skip dependencies)

```bash
dotnet build --no-restore --no-dependencies -v:q
```

### Service Reference

| Service | Directory | Solution | Unit Tests | Integration Tests |
|---------|-----------|----------|------------|-------------------|
| Activity API | `src/Biotrackr.Activity.Api` | `.sln` | Yes | Yes (E2E + Contract) |
| Activity Svc | `src/Biotrackr.Activity.Svc` | `.sln` | Yes | Yes (E2E + Contract) |
| Auth Svc | `src/Biotrackr.Auth.Svc` | `.sln` | Yes | Yes (E2E + Contract) |
| Chat API | `src/Biotrackr.Chat.Api` | `.slnx` | Yes | Yes (E2E + Contract + Evaluation) |
| Food API | `src/Biotrackr.Food.Api` | `.sln` | Yes | Yes (E2E + Contract) |
| Food Svc | `src/Biotrackr.Food.Svc` | `.sln` | Yes | Yes (E2E + Contract) |
| MCP Server | `src/Biotrackr.Mcp.Server` | `.slnx` | Yes | Yes (E2E + Contract) |
| Reporting API | `src/Biotrackr.Reporting.Api` | `.slnx` | Yes | Yes (E2E + Contract) |
| Sleep API | `src/Biotrackr.Sleep.Api` | `.sln` | Yes | Yes (E2E + Contract) |
| Sleep Svc | `src/Biotrackr.Sleep.Svc` | `.sln` | Yes | Yes (E2E + Contract) |
| UI | `src/Biotrackr.UI` | `.slnx` | Yes | No |
| Vitals API | `src/Biotrackr.Vitals.Api` | `.sln` | Yes | Yes (E2E + Contract) |
| Vitals Svc | `src/Biotrackr.Vitals.Svc` | `.sln` | Yes | Yes (E2E + Contract) |

### Test Tiers

```bash
# Unit tests only (default — no filter needed)
dotnet test --no-build

# Contract tests only
dotnet test --no-build --filter "FullyQualifiedName~Contract"

# E2E tests only (requires Cosmos DB Emulator running)
dotnet test --no-build --filter "FullyQualifiedName~E2E"
```

### Coverage

70% minimum threshold enforced in CI. Coverage settings are defined in `coverage.runsettings` per service.

### Docker Build (per service)

```bash
cd src/Biotrackr.{Domain}.{Type}
docker build -t biotrackr-{domain}-{type}:local .
```

## Code Style & Conventions

### Naming

- **Private fields:** `_camelCase`
- **Blazor parameters:** PascalCase with `[Parameter]` attribute
- **CSS classes:** kebab-case with `bt-` prefix (component CSS isolation)
- **Razor components:** PascalCase file names matching component name
- **Test classes:** `{ClassUnderTest}Should`
- **Test methods:** `{Method}_Should{Behavior}_When{Condition}`

### Error Handling

- Use `ArgumentNullException.ThrowIfNull(x)` — never throw base `Exception`
- Use precise exception types only
- Validate at system boundaries, not internal methods

### Service Lifetimes

- **Singleton:** stateless services, HTTP client factories, Cosmos DB clients
- **Scoped:** request-bound services, repository implementations
- **Transient:** lightweight, disposable services

### API Patterns

- Root-mounted paths: `/`, `/{date}`, `/range/{startDate}/{endDate}`
- APIM adds domain prefix (e.g., `/activity/`, `/food/`)
- All list endpoints return `PaginationResponse<T>`
- Date format: `yyyy-MM-dd`

### Formatting

- No formatting tool currently enforced
- CSharpier recommended for consistent formatting (not yet adopted)
- For CSS and Blazor conventions, see `.github/instructions/css-conventions.instructions.md` and `.github/instructions/razor-components.instructions.md`

## Testing Conventions

### Framework Stack

- **xUnit** — test runner
- **FluentAssertions** — assertion library
- **Moq** — mocking framework
- **AutoFixture** — test data generation
- **WebApplicationFactory** — ASP.NET integration testing
- **Cosmos DB Emulator** — local database for E2E tests

### Test Naming

- **Class:** `{ClassUnderTest}Should` (e.g., `ActivityHandlersShould`)
- **Method:** `{Method}_Should{Behavior}_When{Condition}` (e.g., `GetActivityByDate_ShouldReturnOk_WhenActivityIsFound`)

### AAA Pattern

Follow strict Arrange/Act/Assert with comments:

```csharp
[Fact]
public async Task GetActivityByDate_ShouldReturnOk_WhenActivityIsFound()
{
    // Arrange
    var date = "2022-01-01";
    var fixture = new Fixture();
    var activityDocument = fixture.Create<ActivityDocument>();
    activityDocument.Date = date;
    _cosmosRepositoryMock.Setup(x => x.GetActivitySummaryByDate(date)).ReturnsAsync(activityDocument);

    // Act
    var result = await ActivityHandlers.GetActivityByDate(_cosmosRepositoryMock.Object, date);

    // Assert
    result.Result.Should().BeOfType<Ok<ActivityDocument>>();
}
```

### Three Test Tiers

| Tier     | Location                         | Filter                         | Database        | Coverage         |
|----------|----------------------------------|--------------------------------|-----------------|------------------|
| Unit     | `*.UnitTests/`                   | Default (no filter)            | Mocked          | 70% threshold    |
| Contract | `*.IntegrationTests/Contract/`   | `FullyQualifiedName~Contract`  | None (DI only)  | Not collected    |
| E2E      | `*.IntegrationTests/E2E/`        | `FullyQualifiedName~E2E`       | Cosmos Emulator | Not collected    |

### Contract Test Fixtures

Contract tests verify service startup, DI registration, and basic endpoint accessibility without database dependencies.

**Fixture hierarchy:**

- `IntegrationTestFixture` — base fixture with `WebApplicationFactory`, optional database initialization
- `ContractTestFixture : IntegrationTestFixture` — overrides `InitializeDatabase => false`

Contract tests use `[Collection(nameof(ContractTestCollection))]` for fixture sharing. Categories include:

- **ApiSmokeTests** — verify app starts, health endpoint returns OK, OpenAPI doc is valid
- **ProgramStartupTests** — verify DI registrations (CosmosClient is singleton, Repository is scoped)
- **ServiceRegistrationTests** — verify service lifetime (scoped vs transient vs singleton)

### E2E Test Pattern

E2E tests use `[Collection(nameof(IntegrationTestCollection))]` and run against Cosmos DB Emulator:

- Implement `IAsyncLifetime` for setup/teardown
- `InitializeAsync()` clears container, seeds test data
- `DisposeAsync()` cleans up test documents
- Full HTTP request-response cycle via `HttpClient`
- `ConnectionMode.Gateway` forced to avoid TCP+SSL issues with emulator

### Coverage

- **70%** minimum warning threshold, **80%** healthy (CI enforced)
- `coverage.runsettings` per service (identical content, excludes OpenAPI source-generated code)
- `[ExcludeFromCodeCoverage]` attribute for generated code
- Coverage summary posted as sticky PR comment via `irongut/CodeCoverageSummary`

## Commit Standards

### Conventional Commits Format

```text
{type}[optional scope]: {description}

[optional body]

[optional footer(s)]
```

### Commit Types

| Type | Purpose |
|------|---------|
| `feat` | New feature or capability |
| `fix` | Bug fix or defect resolution |
| `core` | Infrastructure or tooling changes |
| `docs` | Documentation updates |
| `refactor` | Code restructuring without behavior change |
| `test` | Test additions or modifications |

### Rules

- **Subject:** max 50 characters, imperative mood, lowercase after colon, no period
- **Body:** max 72 characters per line
- **Scopes:** lowercase, alphanumeric with dashes (e.g., `activity-api`, `infra`, `bicep`)
- **DCO sign-off required:** use `-s` flag on all commits

### Branch Naming

```text
^(feat|fix|core|docs|refactor|test)/[a-z0-9][a-z0-9-]*$
```

### AI Contribution Trailers

When AI coding agents contribute to a commit, all three trailers must be present together — if any trailer is included, all three are required:

```bash
git commit -s -m "feat(vitals-api): add blood pressure validation" \
  --trailer "agent: github-copilot" \
  --trailer "model: Claude Sonnet 4.6" \
  --trailer "contribution: code-generation"
```

**Agent names:** `github-copilot`, `cursor`, `claude-code`, `codeium`, `tabnine`, `cline`

**Contribution types:** `code-generation`, `refactoring`, `documentation`, `test-generation`

Full standard: `docs/standards/commit-standards.md`

## PR Guidelines

### Pipeline Pattern

1. Create PR from feature branch
2. CI/CD pipeline runs automatically on PR
3. Wait for pipeline completion
4. Code review (human or Copilot code review)
5. User merges manually — **NEVER auto-merge**

### PR Validation

PR pipelines validate the following before merge:

- **Unit tests** with 70% coverage threshold
- **Contract tests** (DI/startup verification)
- **Container image build** (Docker build + ACR push)
- **Bicep lint + validate + what-if** (infrastructure changes)
- **Deployment to dev environment**
- **E2E tests** against deployed service with Cosmos DB Emulator

No `pull_request_template.md` exists — PRs rely on CI validation and code review.

## Infrastructure

### Three-Tier Layout

```text
infra/
  core/main.bicep              — Shared resources
  apps/{service}/main.bicep    — Per-service resources
  modules/{domain}/            — Reusable Bicep modules
```

### Module Domains

| Domain           | Modules                                                                  |
|------------------|--------------------------------------------------------------------------|
| `ai/`            | Foundry                                                                  |
| `apim/`          | API Management (consumption, named values, products)                     |
| `configuration/` | App Configuration                                                        |
| `database/`      | Cosmos DB (serverless)                                                   |
| `host/`          | Container Apps (HTTP, sidecar, jobs), Container Registry                 |
| `identity/`      | User-assigned managed identity                                           |
| `monitoring/`    | Agent alerts, App Insights, budget, Log Analytics                        |
| `security/`      | Key Vault                                                                |
| `storage/`       | Storage account                                                          |

### Bicep Naming Conventions

- **Parameters:** camelCase, `@description()` on every param, `@allowed()` for constrained values
- **Standard parameters:** `location`, `baseName`, `environment`, `tags`
- **Module symbolic names:** camelCase (e.g., `logAnalytics`, `appInsights`)
- **Module deployment names:** kebab-case (e.g., `'log-analytics'`, `'app-insights'`)
- **Resource naming:** `{baseName}-{component}-{environment}`
- **Existing resources:** use `existing` keyword for cross-reference
- No custom `bicepconfig.json` — uses default linter rules

### Deployment Pipeline (10 stages per service)

| Stage                    | Purpose                                          |
|--------------------------|--------------------------------------------------|
| 1. env-setup             | Set .NET version                                 |
| 2. run-unit-tests        | 70% coverage threshold                           |
| 3. run-contract-tests    | `FullyQualifiedName~Contract` (parallel with #2) |
| 4. build-container-image | Docker build + ACR push                          |
| 5. retrieve-container-image | Get ACR login server                          |
| 6. lint                  | `az bicep build --file {template}`               |
| 7. validate              | Bicep template validation                        |
| 8. preview               | What-if deployment                               |
| 9. deploy-dev            | `azure/bicep-deploy@v2` with OIDC auth           |
| 10. run-e2e-tests        | Cosmos Emulator + E2E tests                      |

### Infrastructure Boundary

All Bicep changes require explicit review and approval. Never deploy infrastructure changes without `what-if` preview.

## Security Model

### OWASP Agentic Security Controls (ASI01-ASI10)

| Control | OWASP Category                      | Implementation                                                                                          |
|---------|-------------------------------------|---------------------------------------------------------------------------------------------------------|
| ASI01   | Agent Goal Hijack                   | Prompt injection blocklist in GenerateEndpoints, taskMessage 5000 char limit, constrained system prompts |
| ASI02   | Tool Misuse                         | Permission request logging in CopilotService, allowed: shell/read/write only                            |
| ASI03   | Identity & Privilege Abuse          | Agent identity token for inter-service auth, "azp" claim validation, ChatApiAgent policy                |
| ASI04   | Supply Chain                        | Dependabot, CodeQL scanning, locked package versions                                                    |
| ASI05   | Unexpected Code Execution           | ValidateGeneratedCode scans Python for os.system, subprocess, socket, eval, exec                        |
| ASI06   | Memory/Context Poisoning            | ConversationPersistenceMiddleware: 50 hydrated msgs, 10K char limit, 100 msg cap                       |
| ASI07   | Insecure Inter-Agent Communication  | AgentIdentityTokenHandler bearer token, managed identity → FIC → agent identity                        |
| ASI08   | Cascading Failures                  | 10-min report timeout, max 3 concurrent jobs, circuit breaker on Copilot sidecar                        |
| ASI09   | Human-Agent Trust Exploitation      | Reviewer agent validates reports, mandatory disclaimers, concerns flagged to user                        |
| ASI10   | Rogue Agents                        | ReportGenerationEnabled kill switch, 50MB artifact limit, job status tracking                           |

### Additional Security Controls

- **Prompts in Key Vault:** system prompts stored in Azure Key Vault, not committed to git
- **Redacted traces:** API keys redacted in telemetry traces
- **Managed identity:** user-assigned managed identity (`uai-biotrackr-dev`) for all Azure service access
- **APIM JWT validation:** JWT validation on all external endpoints, subscription keys required
- **MCP rate limiting:** 100 req/min per IP on MCP Server

## AI/Agent Architecture

### Chat API

- **LLM:** Claude (Anthropic) via Microsoft Agent Framework (MAF)
- **Transport:** AGUI (Protocol Streaming) over HTTP SSE
- **Agent pattern:** dynamic — rebuilds when MCP tools change
- **Session management:** per-request agent building

### Middleware Pipeline

Order matters — these execute in sequence:

1. **ToolPolicyMiddleware** — max 20 tool calls per session
2. **ConversationPersistenceMiddleware** — Cosmos DB conversation persistence
3. **GracefulDegradationMiddleware** — catches Claude API errors

### MCP Server

- **12 tools:** 4 domains × 3 methods (ByDate, ByDateRange, Records)
- **Transport:** HTTP Stateless
- **Rate limiting:** 100 req/min per IP, 10 queue
- **Authentication:** API key validation (redacted in traces)

### Reporting API

- Background generation via Copilot sidecar
- Code validation gate (ASI05: ValidateGeneratedCode)
- Artifact scan + size validation (50MB limit)
- Reviewer agent validates against source data (ASI09)
- SAS URL generation (24hr expiry)

### Tool Caching

- `CachingMcpToolWrapper` wraps all MCP tools
- Cache keys: tool name + contextual params
- TTL varies by tool type

### Conversation Storage

- Cosmos DB with 90-day TTL
- Container: `conversations` (partition key: `/sessionId`)
- History endpoints: list, get, delete

## Agent Configuration Inventory

### Agents (6)

| Agent                    | File                                                    | Purpose                          |
|--------------------------|---------------------------------------------------------|----------------------------------|
| Azure Principal Architect | `.github/agents/azure-principal-architect.agent.md`   | Azure architecture guidance      |
| Bicep Specialist         | `.github/agents/bicep-implement.agent.md`               | Bicep IaC implementation         |
| C# Expert                | `.github/agents/CSharpExpert.agent.md`                  | C#/.NET coding guidance          |
| Front-End Designer       | `.github/agents/front-end-designer.agent.md`            | Blazor/UI design                 |
| GitHub Actions Expert    | `.github/agents/github-actions-expert.agent.md`         | CI/CD workflow authoring         |
| Vulnerability Scanner    | `.github/agents/vulnerability-scanner.agent.md`         | Security vulnerability analysis  |

### Prompts (4)

| Prompt              | File                                            | Purpose                      |
|---------------------|-------------------------------------------------|------------------------------|
| Accessibility Audit | `.github/prompts/accessibility-audit.prompt.md` | WCAG compliance review       |
| Design Review       | `.github/prompts/design-review.prompt.md`       | UI/UX design evaluation      |
| New Component       | `.github/prompts/new-component.prompt.md`       | Scaffold new Blazor component |
| Perf Optimize       | `.github/prompts/perf-optimize.prompt.md`       | Performance optimization     |

### Instructions (5)

| Instruction          | File                                                            | Trigger              |
|----------------------|-----------------------------------------------------------------|----------------------|
| C# Conventions       | `.github/instructions/csharp-conventions.instructions.md`       | `**/*.cs`            |
| CSS Conventions      | `.github/instructions/css-conventions.instructions.md`          | `**/*.razor.css`     |
| Razor Components     | `.github/instructions/razor-components.instructions.md`         | `**/*.razor`         |
| Bicep Conventions    | `.github/instructions/bicep-conventions.instructions.md`        | `**/*.bicep`         |
| Testing Conventions  | `.github/instructions/testing-conventions.instructions.md`      | `**/*Tests*/**/*.cs` |

### Skills (18)

- **OWASP vulnerability frameworks (11):** agentic, CICD, Docker, infrastructure, LLM, MCP, ML, mobile, OSS, serverless, web
- **Design/UX (4):** accessibility, Blazor design, mobile design, web design
- **.NET/DevOps (2):** dotnet best practices, front-end performance
- **GitHub (1):** create PR from specification

## Decision Records Reference

Architecture Decision Records are stored in `docs/decision-records/`. Consult these for rationale behind key technical decisions including:

- API route structure
- Service lifetime registration patterns
- Integration test project structure
- APIM authentication patterns
- Coverlet coverage anomalies
- Conditional tenant ID injection
- Vitals data model consolidation

Do not modify decision records — they are append-only historical documents.

## Boundary Rules

### NEVER

- Commit secrets, credentials, API keys, or connection strings to the repository
- Push directly to `main` — all changes go through pull requests
- Force push (`--force`) to any shared branch
- Auto-merge pull requests — wait for CI/CD pipeline completion and user approval
- Modify system prompts in `scripts/` directories (stored in Key Vault; changes need security review)
- Weaken or disable any ASI01-ASI10 security control
- Modify Key Vault secret references or managed identity configuration
- Change Cosmos DB partition keys or container structure
- Delete files or data without explicit user confirmation
- Use base `Exception` class — use precise exception types
- Skip DCO sign-off on commits

### ASK FIRST

- Cross-cutting refactors affecting multiple services
- New NuGet package dependencies (assess compatibility and licensing)
- Changes to middleware pipeline order in Chat API (ToolPolicy → ConversationPersistence → GracefulDegradation)
- MCP tool schema changes (affects all consuming agents)
- API endpoint changes (affects APIM configuration)
- GitHub Actions workflow modifications
- Infrastructure/Bicep changes (deployment configs require review)
- Schema or data model changes affecting Cosmos DB documents
- Changes to the AI agent architecture (MAF, AGUI, MCP patterns)
- Adding or removing services from the architecture

## Cross-References

* `AGENTS.md` — Standalone agent guide (9 sections, cross-tool compatible)
* `CLAUDE.md` — Claude Code import file (`@AGENTS.md`)
* `.github/instructions/csharp-conventions.instructions.md` — C# coding conventions (auto-loaded for `**/*.cs`)
* `.github/instructions/css-conventions.instructions.md` — CSS isolation conventions (auto-loaded for `**/*.razor.css`)
* `.github/instructions/razor-components.instructions.md` — Razor component conventions (auto-loaded for `**/*.razor`)
* `.github/instructions/bicep-conventions.instructions.md` — Bicep IaC conventions (auto-loaded for `**/*.bicep`)
* `.github/instructions/testing-conventions.instructions.md` — Testing conventions (auto-loaded for `**/*Tests*/**/*.cs`)
