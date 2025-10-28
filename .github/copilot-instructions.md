# biotrackr Development Guidelines

Auto-generated from all feature plans. Last updated: 2025-10-28

## Active Technologies
- Azure Cosmos DB (existing, no changes needed) (002-weight-svc-coverage)
- C# / .NET 9.0 (003-weight-svc-integration-tests)
- Azure Cosmos DB (via Emulator in tests - mcr.microsoft.com/cosmosdb/linux/azure-cosmos-emulator:latest) (003-weight-svc-integration-tests)

- .NET 9.0 (C#) + xUnit 2.9.3, FluentAssertions 8.4.0, Moq 4.20.72, AutoFixture 4.18.1, coverlet.collector 6.0.4 (001-weight-api-tests)

## Project Structure

```text
backend/
frontend/
tests/
```

## Commands

# Add commands for .NET 9.0 (C#)

## Code Style

.NET 9.0 (C#): Follow standard conventions

## Recent Changes
- 003-weight-svc-integration-tests: Added C# / .NET 9.0
- 002-weight-svc-coverage: Added .NET 9.0 (C#) + xUnit 2.9.3, FluentAssertions 8.4.0, Moq 4.20.72, AutoFixture 4.18.1, coverlet.collector 6.0.4

- 001-weight-api-tests: Added .NET 9.0 (C#) + xUnit 2.9.3, FluentAssertions 8.4.0, Moq 4.20.72, AutoFixture 4.18.1, coverlet.collector 6.0.4

<!-- MANUAL ADDITIONS START -->

## Service Lifetime Guidelines

When registering services with dependency injection:

- **Azure SDK clients** (CosmosClient, SecretClient) → **Singleton** (expensive to create, thread-safe, manage connection pooling)
- **Application services** (Repositories, Services) → **Scoped** (one instance per request/execution scope)
- **HttpClient-based services** → **Transient** (registered via AddHttpClient, managed by HttpClientFactory)

⚠️ **Do NOT use duplicate registrations** for services registered with `AddHttpClient<TClient, TImplementation>()` - the AddHttpClient call handles the registration.

See [docs/decision-records/2025-10-28-service-lifetime-registration.md](../docs/decision-records/2025-10-28-service-lifetime-registration.md) for details.

<!-- MANUAL ADDITIONS END -->
