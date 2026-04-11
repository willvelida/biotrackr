<!-- markdownlint-disable-file -->
# AGENTS.md

Universal guide for AI coding agents working on the Biotrackr repository. Follows the open [agents.md](https://agents.md) specification. For comprehensive Copilot-specific guidance, see `.github/copilot-instructions.md`.

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
├── AGENTS.md                          # This file
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

## Security Overview

### Key Security Controls

- **Managed Identity:** user-assigned managed identity (`uai-biotrackr-dev`) for all services
- **Agent Identity:** Chat API acquires agent identity token for inter-service authentication (ASI03/ASI07)
- **API Management:** JWT validation on all external endpoints, subscription keys required
- **Tool Policies:** maximum 20 tool calls per session, tool whitelisting enforced
- **Conversation Limits:** max 50 hydrated messages, 10K character limit, 100 message cap (ASI06)
- **Code Validation:** Python script scanning for dangerous patterns before execution (ASI05)
- **Report Review:** independent reviewer agent validates reports against source data (ASI09)
- **Prompt Injection Detection:** blocklist patterns on report generation requests (ASI01)
- **Prompts in Key Vault:** system prompts stored in Azure Key Vault, not committed to git

Full OWASP Agentic Security (ASI01-ASI10) details are in `.github/copilot-instructions.md`.

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

## Additional Resources

For comprehensive Copilot-specific guidance including testing conventions, PR guidelines, infrastructure details, full OWASP Agentic Security (ASI01-ASI10), AI/agent architecture, and agent configuration inventory, see `.github/copilot-instructions.md`.
