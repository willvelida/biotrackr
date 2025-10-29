# Cosmos DB Emulator for Local Development

This directory contains Docker Compose configuration and helper scripts for running Azure Cosmos DB Emulator locally.

## Quick Start

### Prerequisites
- Docker Desktop installed and running
- **Windows**: 8GB RAM allocated to Docker Desktop (Settings → Resources → Memory)
- PowerShell (for Windows helper script)

### Start Emulator

**Windows (PowerShell)**:
```powershell
.\cosmos-emulator.ps1 start
```

**macOS/Linux**:
```bash
docker-compose -f docker-compose.cosmos.yml up -d
```

### Check Status

**Windows (PowerShell)**:
```powershell
.\cosmos-emulator.ps1 status
```

**macOS/Linux**:
```bash
docker ps --filter "name=biotrackr-cosmos-emulator"
```

### Install SSL Certificate (Required for E2E Tests)

**Windows (PowerShell as Administrator)**:
```powershell
# Right-click PowerShell → "Run as Administrator"
.\cosmos-emulator.ps1 cert
```

**macOS/Linux**:
```bash
# Download certificate
curl -k https://localhost:8081/_explorer/emulator.pem > ~/cosmos-emulator.crt

# macOS: Import to Keychain
sudo security add-trusted-cert -d -r trustRoot -k /Library/Keychains/System.keychain ~/cosmos-emulator.crt

# Linux: Install to trusted certificates
sudo cp ~/cosmos-emulator.crt /usr/local/share/ca-certificates/
sudo update-ca-certificates
```

### Stop Emulator

**Windows (PowerShell)**:
```powershell
.\cosmos-emulator.ps1 stop
```

**macOS/Linux**:
```bash
docker-compose -f docker-compose.cosmos.yml down
```

## Connection Details

| Property | Value |
|----------|-------|
| **Endpoint** | `https://localhost:8081` |
| **Primary Key** | `C2y6yDjf5/R+ob0N8A7Cgv30VRDJIWEHLM+4QDU5DE2nQ9nDuVTqobD4b8mGGyPMbIZnqyMsEcaGQy67XIw/Jw==` |
| **Database Name** | `biotrackr-test` |
| **Container Name** | `weight-data` |

These values are configured in `appsettings.Test.json` in each integration test project.

## Running E2E Tests

After starting the emulator and installing the certificate:

```powershell
# Run all E2E tests
cd src/Biotrackr.Activity.Api
dotnet test --filter "FullyQualifiedName~E2E"

# Run all integration tests (Contract + E2E)
dotnet test Biotrackr.Activity.Api.IntegrationTests
```

## Troubleshooting

### "No connection could be made (localhost:8081)"
- **Cause**: Emulator not running or still initializing
- **Fix**: Wait 2-3 minutes for first startup, check status with `.\cosmos-emulator.ps1 status`

### "The SSL connection could not be established"
- **Cause**: Emulator certificate not trusted
- **Fix**: Run `.\cosmos-emulator.ps1 cert` as Administrator (Windows) or install certificate manually

### Container won't start / crashes immediately
- **Cause**: Insufficient memory allocated to Docker Desktop
- **Fix**: Increase Docker memory to 8GB (Settings → Resources → Memory → Apply & Restart)

### Emulator is slow or unresponsive
- **Cause**: Normal for first startup or after container restart
- **Fix**: Wait 2-3 minutes for full initialization, check logs with:
  ```powershell
  docker logs biotrackr-cosmos-emulator -f
  ```

## Architecture Notes

- **Partition Count**: 10 (configured for test workloads)
- **Data Persistence**: Disabled (fresh state on each restart)
- **IP Override**: `127.0.0.1` (required for Docker networking)
- **Ports**:
  - `8081`: HTTPS endpoint (primary)
  - `10251-10254`: Additional data plane endpoints

## CI/CD vs Local

- **GitHub Actions**: Uses same Docker image with service containers
- **Local Development**: Uses Docker Compose for easier management
- **Both**: Share identical configuration (partition count, persistence, IP override)

## References

- [Cosmos DB Emulator Docker Hub](https://hub.docker.com/r/microsoft/azure-cosmosdb-emulator)
- [Cosmos DB Emulator Documentation](https://docs.microsoft.com/en-us/azure/cosmos-db/local-emulator)
- [GitHub Actions Services](https://docs.github.com/en/actions/using-containerized-services/about-service-containers)
