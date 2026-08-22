---
title: Development
description: Getting Biotrackr running locally, the dev container path, the manual path, the Cosmos DB emulator, and the failure modes worth knowing in advance
ms.date: 2026-08-21
ms.topic: how-to
---

Two supported paths to a running local stack: the dev container, which is the recommended one, and a manual host setup. Both end at `http://localhost:5239` with the Blazor UI reading seeded data from a local Cosmos DB emulator.

## Dev container

Open the repository in VS Code with the Dev Containers extension and run **Dev Containers: Reopen in Container**. The container provisions .NET 10.0, the Cosmos DB vNext emulator with a trusted certificate, the Azure and GitHub CLIs, Bicep, Gitleaks, and 30 seeded documents. Then:

```bash
bash scripts/start-local.sh
```

That starts a Caddy gateway on port 9000, four domain APIs, and the UI on 5239. Ctrl+C stops all of them.

The gateway is what makes local development match production routing. Caddy applies the same domain prefixes APIM applies in Azure, so an endpoint reached through port 9000 exercises the real path shape and an endpoint reached directly on the service port does not. See the APIM routing section in [architecture.md](architecture.md).

Full detail including ports, seed data, and optional secrets is in [devcontainer-setup.md](devcontainer-setup.md).

### What the dev container does not do

It does not seed Azure secrets or run the AI components against live model endpoints.
Those need real credentials, so the first run of anything under `Biotrackr.Chat.Api` or
`Biotrackr.Reporting.*` against a real model happens outside the container.

`scripts/init.sh` restores all fourteen services, so build scope is no longer the gap it
once was.

## Manual setup

Prerequisites: the .NET 10.0 SDK matching `global.json`, Docker Desktop, Azure CLI, and PowerShell 7 or later.

```powershell
./cosmos-emulator.ps1 start
./cosmos-emulator.ps1 status
./cosmos-emulator.ps1 cert
```

The `cert` step is not optional if you intend to run E2E tests. Without a trusted emulator certificate the tests fail on TLS handshake, and the error names the certificate chain rather than the missing trust step. Docker Compose is an alternative for the emulator alone:

```bash
docker compose -f docker-compose.cosmos.yml up -d
```

Full emulator detail, including connection strings and the well-known key, is in [cosmos-emulator-setup.md](cosmos-emulator-setup.md).

## Configuration

Every service reads its settings from Azure App Configuration, resolved with a user-assigned managed identity. Two environment variables drive that:

* `azureappconfigendpoint`
* `managedidentityclientid`

Both must be set even locally. A service missing either starts cleanly and then fails on the first configuration read, which surfaces downstream as a null reference in code that looks unrelated to configuration.

## Building and testing

There is no root solution. Every command starts from a service directory:

```bash
cd src/Biotrackr.Activity.Api
dotnet restore
dotnet build --no-restore
dotnet test --no-build
```

Once packages are stable, skip restore with `dotnet build --no-restore -v:q`, and skip dependency rebuilds with `--no-dependencies` when iterating inside one project. Test tiers and coverage are covered in [testing.md](testing.md).

Container images build per service from the service directory:

```bash
cd src/Biotrackr.Activity.Api
docker build -t biotrackr-activity-api:local .
```

## Known failure modes

The emulator health check timing out on first start usually means the container is still initialising rather than broken; the vNext emulator takes noticeably longer on its first run.

Port conflicts on 8081 or 1234 are common when a previous emulator container is still running. Stop it before starting a new one rather than changing the port, because the seeded connection settings assume the defaults.

APIs failing to connect to Cosmos DB after the emulator is confirmed healthy is almost always the certificate trust step, not the connection string.

More troubleshooting is in [devcontainer-setup.md](devcontainer-setup.md) and [cosmos-emulator-setup.md](cosmos-emulator-setup.md).

## Related documents

* [testing.md](testing.md) for tiers, filters, and coverage
* [architecture.md](architecture.md) for why the gateway matters locally
* [infrastructure.md](infrastructure.md) for deploying rather than running
