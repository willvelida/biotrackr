---
description: "Cosmos DB data access conventions for Biotrackr services. Use when: writing or editing Cosmos DB repository implementations, document models, or database-related code."
applyTo: "**/*Repository*.cs,**/*Document*.cs,**/*Cosmos*.cs"
---

# Cosmos DB Conventions

## Repository Pattern

- Constructor-inject `CosmosClient` — one repository class per domain
- Use parameterized `QueryDefinition` for all queries — never string interpolation
- Register extension method per service (e.g., `AddCosmosRepository()`)

## Document Models

- Every document requires `Id` property and a partition key field
- Use `[JsonPropertyName]` attributes matching Cosmos DB property names
- Apply TTL where applicable (e.g., 90-day TTL on conversation documents)
- Keep documents flat — avoid deep nesting for query performance

## Partition Keys

- Single partition key per container (e.g., `/date` for activity, `/sessionId` for conversations)
- Design queries to target a single partition — avoid cross-partition queries
- Choose high-cardinality keys that distribute writes evenly

## Connection Mode

- Use `ConnectionMode.Gateway` for Cosmos DB Emulator (avoids TCP+SSL issues)
- Use `ConnectionMode.Direct` for production (lower latency)
- E2E test fixtures force `ConnectionMode.Gateway` explicitly

## Query Patterns

- Prefer point reads (`ReadItemAsync`) over queries when Id and partition key are known
- Use `QueryDefinition` with `.WithParameter()` for parameterized queries
- Return `PaginationResponse<T>` for list endpoints with `pageNumber` and `pageSize`
- Use `OFFSET` and `LIMIT` for pagination in queries

## Service Lifetimes

- `CosmosClient` — **Singleton** (thread-safe, connection pooling)
- Repository implementations — **Scoped** (request-bound)

## E2E Test Patterns

- Implement `IAsyncLifetime` for setup/teardown
- `InitializeAsync()` clears the container and seeds test data
- `DisposeAsync()` removes test documents
- Force `ConnectionMode.Gateway` in test `CosmosClientOptions`
- Use `[Collection(nameof(IntegrationTestCollection))]` for fixture sharing
