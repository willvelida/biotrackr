# biotrackr Development Guidelines

Auto-generated from all feature plans. Last updated: 2025-11-03

## Active Technologies
- Azure Cosmos DB (existing, no changes needed) (002-weight-svc-coverage)
- C# / .NET 9.0 (003-weight-svc-integration-tests)
- Azure Cosmos DB (via Emulator in tests - mcr.microsoft.com/cosmosdb/linux/azure-cosmos-emulator:latest) (003-weight-svc-integration-tests)
- .NET 9.0 (C#) + xUnit 2.9.3, FluentAssertions 8.4.0, Moq 4.20.72, AutoFixture 4.18.1, coverlet.collector 6.0.4, Microsoft.AspNetCore.Mvc.Testing 9.0.0 (004-activity-api-tests)
- Azure Cosmos DB (for integration tests), In-memory mocks (for unit tests) (004-activity-api-tests)
- C# / .NET 9.0 + xUnit 2.9.3, FluentAssertions 8.4.0, Moq 4.20.72, AutoFixture 4.18.1, coverlet.collector 6.0.4, Microsoft.AspNetCore.Mvc.Testing 9.0.0 (005-activity-svc-tests)
- Azure Cosmos DB (via Emulator in tests using mcr.microsoft.com/cosmosdb/linux/azure-cosmos-emulator:latest) (005-activity-svc-tests)
- C# / .NET 9.0 + xUnit 2.9.3, FluentAssertions 8.4.0, Moq 4.20.72, AutoFixture 4.18.1, coverlet.collector 6.0.4, Microsoft.AspNetCore.Mvc.Testing 9.0.0 (007-sleep-svc-tests)
- Azure Cosmos DB (via Emulator in tests using mcr.microsoft.com/cosmosdb/linux/azure-cosmos-emulator:latest) (007-sleep-svc-tests)
- .NET 9.0 (C#) + xUnit 2.9.3, FluentAssertions 8.4.0, Moq 4.20.72, AutoFixture 4.18.1, coverlet.collector 6.0.4 (001-weight-api-tests)
- C# / .NET 9.0 + xUnit 2.9.3, FluentAssertions 8.4.0, Moq 4.20.72, AutoFixture 4.18.1, coverlet.collector 6.0.4, Microsoft.AspNetCore.Mvc.Testing 9.0.0, Azure.Identity 1.14.1 (008-auth-svc-tests)

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
- 009-food-svc-tests: Added C# / .NET 9.0 + xUnit 2.9.3, FluentAssertions 8.4.0, Moq 4.20.72, AutoFixture 4.18.1, coverlet.collector 6.0.4, Microsoft.AspNetCore.Mvc.Testing 9.0.0
- 008-auth-svc-tests: Added complete test coverage (97.5%) with unit, contract, and E2E tests for Auth Service
- 007-sleep-svc-tests: Added C# / .NET 9.0 + xUnit 2.9.3, FluentAssertions 8.4.0, Moq 4.20.72, AutoFixture 4.18.1, coverlet.collector 6.0.4, Microsoft.AspNetCore.Mvc.Testing 9.0.0


<!-- MANUAL ADDITIONS START -->

## Service Lifetime Guidelines

When registering services with dependency injection:

- **Azure SDK clients** (CosmosClient, SecretClient) → **Singleton** (expensive to create, thread-safe, manage connection pooling)
- **Application services** (Repositories, Services) → **Scoped** (one instance per request/execution scope)
- **HttpClient-based services** → **Transient** (registered via AddHttpClient, managed by HttpClientFactory)

⚠️ **Do NOT use duplicate registrations** for services registered with `AddHttpClient<TClient, TImplementation>()` - the AddHttpClient call handles the registration.

See [docs/decision-records/2025-10-28-service-lifetime-registration.md](../docs/decision-records/2025-10-28-service-lifetime-registration.md) for details.

## Common Issues & Resolutions

⚠️ **IMPORTANT**: Before implementing tests or workflows, READ [.specify/memory/common-resolutions.md](../.specify/memory/common-resolutions.md) for known issues and their solutions.

This file contains:
- GitHub Actions workflow issues (permissions, target frameworks, working directories)
- Service lifetime & dependency injection patterns
- Test organization (Contract vs E2E)
- E2E test isolation and Cosmos DB cleanup patterns
- Prevention strategies for recurring issues

**Always check this file first when encountering test failures or workflow issues.**

<!-- MANUAL ADDITIONS END -->
