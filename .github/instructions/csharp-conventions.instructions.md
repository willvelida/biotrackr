---
description: "C# coding conventions for Biotrackr .NET services. Use when: writing or editing C# source files, service implementations, API handlers, data models, or project files."
applyTo: "**/*.cs,**/*.csproj"
---

# C# Conventions

## Naming

- Private fields: `_camelCase` (e.g., `_cosmosRepository`)
- Constants: PascalCase (e.g., `MaxToolCalls`)
- Parameters and local variables: camelCase
- Interfaces: `I` prefix (e.g., `ICosmosRepository`)
- Test classes: `{ClassUnderTest}Should`
- Test methods: `{Method}_Should{Behavior}_When{Condition}`

## Error Handling

- Use `ArgumentNullException.ThrowIfNull(x)` for null guards
- Never throw base `Exception` — use precise exception types
- Validate at system boundaries (API handlers, service entry points), not internal methods
- Use `ArgumentException`, `InvalidOperationException`, `KeyNotFoundException` as appropriate

## Service Lifetimes

- **Singleton**: stateless services, HTTP client factories, Cosmos DB clients, configuration
- **Scoped**: request-bound services, repository implementations
- **Transient**: lightweight, short-lived, disposable services

## Async Patterns

- Suffix async methods with `Async`
- Always pass `CancellationToken` through the call chain where available
- Use `ConfigureAwait(false)` in library code, not in ASP.NET Core handlers

## API Handlers

- Root-mounted paths: `/`, `/{date}`, `/range/{startDate}/{endDate}`
- Date format: `yyyy-MM-dd` (validate with `DateOnly.TryParseExact`)
- Return `PaginationResponse<T>` for list endpoints
- Use `Results.Ok()`, `Results.NotFound()`, `Results.BadRequest()` from minimal APIs

## Dependency Injection

- Register services in extension methods (e.g., `AddCosmosRepository()`)
- Use `IOptions<T>` or `IOptionsSnapshot<T>` for configuration binding
- Prefer constructor injection over service locator patterns

## Code Organization

- One class per file (exceptions: small nested types, records)
- File-scoped namespaces (`namespace Biotrackr.Activity.Api.Models;`)
- Group `using` directives: System first, then Microsoft, then third-party, then project

## Security Dependency Pins

- When pinning a transitive NuGet dependency to fix a CVE, add an XML comment above the `PackageReference` with the CVE ID and removal condition
- Example: `<!-- CVE-2026-44375: pin until Microsoft.Agents.AI.* updates to >= 1.1.62 -->`
- After pinning, verify no new HIGH/CRITICAL audit warnings (NU1902/NU1903) were introduced by the updated package's own transitives
