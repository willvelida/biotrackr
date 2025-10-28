# Decision Record: Service Lifetime Registration for HttpClient-Based Services

- **Status**: Accepted
- **Deciders**: Development Team
- **Date**: 28 October 2025
- **Related Docs**: [Integration Test Project Structure](2025-10-28-integration-test-project-structure.md)

## Context

During implementation of integration tests for `Biotrackr.Weight.Svc`, we discovered conflicting service registrations in `Program.cs`:

```csharp
services.AddScoped<IFitbitService, FitbitService>();  // First registration
services.AddHttpClient<IFitbitService, FitbitService>()  // Second registration (overrides first)
    .AddStandardResilienceHandler();
```

This caused test failures when verifying service lifetime behavior, as the test expected scoped lifetime but the actual runtime behavior was transient (from `AddHttpClient`).

The issue raised questions about:
1. What is the correct service lifetime for HttpClient-based services?
2. Should we have duplicate registrations?
3. How should integration tests verify service lifetimes?

## Decision

**Remove duplicate service registrations when using `AddHttpClient<TClient, TImplementation>()`.**

Services that depend on `HttpClient` should be registered **only** via `AddHttpClient`, which registers them as **transient** by default. This is the correct lifetime for the following reasons:

1. **HttpClientFactory manages connection pooling** - The factory handles HttpClient instance management and connection pooling, so services don't need extended lifetimes
2. **Lightweight service instances** - HttpClient-based services are typically stateless and cheap to instantiate
3. **Avoids lifetime conflicts** - Having two registrations where the second overrides the first is confusing and error-prone
4. **Follows Microsoft best practices** - [HttpClient guidelines](https://learn.microsoft.com/en-us/dotnet/core/extensions/httpclient-factory) recommend transient lifetime for typed clients

### Service Lifetime Guidelines

| Service Type | Lifetime | Reason |
|-------------|----------|--------|
| `CosmosClient` | Singleton | Expensive to create, thread-safe, manages connection pooling |
| `SecretClient` | Singleton | Expensive to create, thread-safe, caches secrets |
| `ICosmosRepository` | Scoped | Stateless, one instance per execution scope |
| `IWeightService` | Scoped | Orchestration service, one instance per scope |
| `IFitbitService` | Transient | HttpClient-based, managed by HttpClientFactory |

### Corrected Registration

```csharp
// Azure SDK clients - Singleton (expensive, thread-safe)
services.AddSingleton(new SecretClient(new Uri(keyVaultUrl), new DefaultAzureCredential(defaultCredentialOptions)));
services.AddSingleton(new CosmosClient(cosmosDbEndpoint, new DefaultAzureCredential(defaultCredentialOptions), cosmosClientOptions));

// Application services - Scoped (one per request/execution)
services.AddScoped<ICosmosRepository, CosmosRepository>();
services.AddScoped<IWeightService, WeightService>();

// HttpClient-based services - Transient (managed by HttpClientFactory)
services.AddHttpClient<IFitbitService, FitbitService>()
    .AddStandardResilienceHandler();
```

## Consequences

### Positive
- **Clearer code**: Single registration point per service eliminates confusion
- **Correct behavior**: Service lifetime matches Microsoft best practices
- **Better testing**: Integration tests can verify actual runtime behavior
- **Performance**: HttpClientFactory properly manages connection pooling
- **Consistency**: All microservices follow the same registration pattern

### Negative
- **Breaking change for tests**: Existing tests expecting scoped lifetime needed updates
- **Documentation needed**: Team must understand why HttpClient services are transient

### Neutral
- **Test updates required**: ServiceRegistrationTests updated to verify transient lifetime for FitbitService
- **No runtime impact**: The duplicate registration was already being overridden, so behavior doesn't change

## Alternatives Considered

### Alternative 1: Keep duplicate registrations and update tests
- **Rejected**: Having two registrations where one is ignored is confusing and error-prone
- Violates principle of least surprise

### Alternative 2: Force scoped lifetime for HttpClient services
```csharp
services.AddHttpClient<IFitbitService, FitbitService>()
    .SetHandlerLifetime(Timeout.InfiniteTimeSpan);
services.AddScoped<IFitbitService>(sp => sp.GetRequiredService<IFitbitService>());
```
- **Rejected**: Circumvents HttpClientFactory's intended design
- Increases memory usage and connection pool exhaustion risk
- No clear benefit over transient lifetime

### Alternative 3: Remove HttpClient dependency
- **Rejected**: Would require reimplementing retry/resilience logic
- `AddStandardResilienceHandler()` provides battle-tested retry patterns

## Follow-up Actions

- [x] Remove duplicate `AddScoped<IFitbitService>` registration from `Program.cs`
- [x] Update `ServiceRegistrationTests.cs` to verify transient lifetime for `FitbitService`
- [x] Verify all contract tests pass with corrected registration
- [ ] Apply same pattern to other microservices (`Activity`, `Sleep`, `Food`, `Auth`)
- [ ] Document service lifetime guidelines in project README
- [ ] Add code review checklist item: "Verify no duplicate service registrations with AddHttpClient"

## Notes

### Testing Service Lifetimes

**Transient services** return different instances on every resolution:
```csharp
var service1 = scope.ServiceProvider.GetService<IFitbitService>();
var service2 = scope.ServiceProvider.GetService<IFitbitService>();
service1.Should().NotBeSameAs(service2); // Different instances
```

**Scoped services** return the same instance within a scope:
```csharp
using (var scope = serviceProvider.CreateScope())
{
    var service1 = scope.ServiceProvider.GetService<IWeightService>();
    var service2 = scope.ServiceProvider.GetService<IWeightService>();
    service1.Should().BeSameAs(service2); // Same instance
}
```

**Singleton services** return the same instance across all scopes:
```csharp
var service1 = serviceProvider.GetService<CosmosClient>();
var service2 = serviceProvider.GetService<CosmosClient>();
service1.Should().BeSameAs(service2); // Same instance globally
```

### References
- [HttpClientFactory in .NET](https://learn.microsoft.com/en-us/dotnet/core/extensions/httpclient-factory)
- [Dependency Injection Guidelines](https://learn.microsoft.com/en-us/dotnet/core/extensions/dependency-injection-guidelines)
- [Typed HttpClient Best Practices](https://learn.microsoft.com/en-us/aspnet/core/fundamentals/http-requests)
