# Dev Container Setup

The recommended way to develop Biotrackr locally. The dev container provides a fully configured environment with .NET 10.0, Cosmos DB emulator, CLI tools, and pre-built services — no manual setup required.

## Quick Start

### Prerequisites

- [Docker Desktop](https://www.docker.com/products/docker-desktop/) installed and running
- [VS Code](https://code.visualstudio.com/) with the [Dev Containers extension](https://marketplace.visualstudio.com/items?itemName=ms-vscode-remote.remote-containers)

### Open in Dev Container

1. Open the repository in VS Code
2. Press `Ctrl+Shift+P` and run **Dev Containers: Reopen in Container**
3. Wait for the container to build (first run takes several minutes for image pulls)

The container automatically:

- Starts the Cosmos DB vNext emulator with HTTPS
- Installs .NET global tools, Azure CLI, PowerShell, GitHub CLI, Bicep, and Gitleaks
- Trusts the emulator HTTPS certificate
- Installs a Gitleaks pre-commit hook for secret scanning
- Restores and builds all 8 in-scope services
- Creates `BiotrackrDB` with `records` and `conversations` containers
- Seeds 30 sample documents (7 days of activity, food, sleep, vitals + 2 chat conversations)

### Run the Full Stack

After the container opens:

```bash
bash scripts/start-local.sh
```

This starts the Caddy API gateway (port 9000), 4 domain APIs, and the Blazor UI. Open `http://localhost:5239` in your browser.

| Service | Port | URL |
|---------|------|-----|
| API Gateway (Caddy) | 9000 | `http://localhost:9000` |
| Activity API | 5272 | `http://localhost:5272` |
| Food API | 5006 | `http://localhost:5006` |
| Sleep API | 5004 | `http://localhost:5004` |
| Vitals API | 5062 | `http://localhost:5062` |
| UI | 5239 | `http://localhost:5239` |
| Cosmos Data Explorer | 1234 | `http://localhost:1234` |

Press `Ctrl+C` to stop all services.

## Cosmos DB Emulator

The dev container uses the [vNext Cosmos DB emulator](https://learn.microsoft.com/en-us/azure/cosmos-db/emulator) (`vnext-preview` tag), which supports both arm64 (Apple Silicon) and x64 architectures.

- **HTTPS Gateway**: `https://cosmos-emulator:8081` (inside container) / `https://localhost:8081` (from host)
- **Data Explorer**: `http://localhost:1234`
- **Health probe**: `http://cosmos-emulator:8080/ready`
- **Well-known key**: `C2y6yDjf5/R+ob0N8A7Cgv30VRDJIWEHLM+4QDU5DE2nQ9nDuVTqobD4b8mGGyPMbIZnqyMsEcaGQy67XIw/Jw==`

### Seed Data

The emulator is pre-seeded with 30 documents across 2026-04-24 through 2026-04-30:

| Container | Partition Key | Documents | Content |
|-----------|--------------|-----------|---------|
| `records` | `/documentType` | 28 | 7 Activity + 7 Food + 7 Sleep + 7 Vitals |
| `conversations` | `/sessionId` | 2 | Sample chat conversations |

## Optional Secrets

API keys are optional. The dev container works without them — AI features (Chat, Reporting) are unavailable.

### VS Code / Local

Copy `.devcontainer/.env.example` to `.devcontainer/.env` and fill in values:

```bash
cp .devcontainer/.env.example .devcontainer/.env
```

### GitHub Codespaces

Codespaces prompts for secrets on creation. Configure them in your [Codespaces secrets](https://github.com/settings/codespaces):

| Secret | Required | Purpose |
|--------|----------|---------|
| `ANTHROPIC_API_KEY` | No | Chat.Api AI features |
| `MCP_SERVER_API_KEY` | No | MCP Server inter-service auth |
| `COPILOT_GITHUB_TOKEN` | No | Reporting.Api Copilot sidecar |

### Azure Key Vault (opt-in)

Developers with Azure access can pull secrets from `kv-biotrackr-dev`:

```bash
source scripts/pull-dev-secrets.sh
```

Requires `az login` and `Key Vault Secrets User` RBAC role.

## Running Tests

Inside the dev container:

```bash
# Unit tests for a specific service
cd src/Biotrackr.Activity.Api
dotnet test --no-build

# E2E tests against the emulator
dotnet test --no-build --filter "FullyQualifiedName~E2E"

# Contract tests
dotnet test --no-build --filter "FullyQualifiedName~Contract"
```

## Architecture

The dev container uses Docker Compose mode with two services:

```text
┌─────────────────────────────────────────────┐
│ app (dotnet:10.0-noble)                     │
│  .NET 10.0, Azure CLI, PowerShell, Caddy,   │
│  GitHub CLI, Bicep, Gitleaks                 │
│                                              │
│  start-local.sh → Caddy :9000               │
│                 → Activity API :5272         │
│                 → Food API :5006             │
│                 → Sleep API :5004            │
│                 → Vitals API :5062           │
│                 → UI :5239                   │
├─────────────────────────────────────────────┤
│ cosmos-emulator (vnext-preview)              │
│  HTTPS :8081, Health :8080, Explorer :1234   │
└─────────────────────────────────────────────┘
```

## Troubleshooting

### Emulator health check times out

The vNext emulator needs shared memory for its PostgreSQL backend. The `docker-compose.yml` sets `shm_size: 256mb`. If it still fails, increase to `512mb`.

### Port conflicts

If port 8080 or 8081 is in use by another container, stop it first:

```bash
docker ps --format "table {{.Names}}\t{{.Ports}}"
docker stop <conflicting-container>
```

### APIs fail to connect to Cosmos DB

The `start-local.sh` script sets `cosmosdbaccountkey` for emulator key-based auth. If you see `DefaultAzureCredential` errors, ensure you started services via the script rather than `dotnet run` directly.

### UI shows loading spinner

The UI requires the backend APIs running behind the Caddy gateway. Run `bash scripts/start-local.sh` — do not start the UI standalone.

## Manual Setup (Alternative)

For manual setup without the dev container, see:

- [Cosmos DB Emulator Setup](cosmos-emulator-setup.md) — manual emulator configuration
- [AGENTS.md](../AGENTS.md) — prerequisites and build commands
