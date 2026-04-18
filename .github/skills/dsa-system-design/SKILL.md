---
name: dsa-system-design
description: "Scalability fundamentals, data partitioning, caching strategies, rate limiting, distributed systems patterns, and microservices data design — bridging classical DSA concepts to real production architecture decisions. Use when: designing or reviewing distributed system components, selecting a Cosmos DB partition key, implementing rate limiting or throttling policies, choosing caching strategies, analysing microservice communication patterns, or explaining CAP theorem trade-offs in the context of the Biotrackr architecture."
---

# DSA System Design

## When to Use

- Selecting a partition key strategy for Cosmos DB containers
- Designing or reviewing rate limiting and throttling policies
- Explaining horizontal vs vertical scaling decisions
- Analysing caching strategies (in-memory, distributed, CDN)
- Reviewing microservice communication topology for bottlenecks
- Teaching CAP theorem and consistency trade-offs in the context of production systems
- Connecting classical data structure choices (heap, hash map) to distributed systems decisions

## Scalability Fundamentals

### Vertical vs Horizontal Scaling

| Approach | Mechanism | Limit |
|---|---|---|
| Vertical | Add CPU/RAM to one machine | Hardware ceiling |
| Horizontal | Add more machines / replicas | Near-unlimited with stateless design |

**Biotrackr context:** Azure Container Apps scales horizontally — new container replicas
are spun up under load. Services must be **stateless** (no local in-memory state that needs
to be shared) for horizontal scaling to work correctly. Cosmos DB handles horizontal scaling
via partitioning.

### Latency Reference Numbers

| Operation | Approximate Latency |
|---|---|
| L1 cache read | ~1 ns |
| Memory (RAM) read | ~100 ns |
| SSD read | ~100 µs |
| Network round-trip (same region) | ~1 ms |
| Cosmos DB query (indexed, right partition) | ~5–10 ms |
| Cross-region replication lag | ~50–150 ms |

**Teaching angle:** A cache hit that avoids a Cosmos DB query saves ~5–10 ms per call.
At 100 calls/sec, that is 500–1000 ms of latency removed from the system per second.

## Data Partitioning (Cosmos DB Context)

### Why Partitioning Matters

Cosmos DB distributes data across physical partitions by hash of the partition key. All
requests for a given key are routed to the same physical partition — O(1) dispatch.

### Partition Key Selection Criteria

| Criterion | Goal |
|---|---|
| High cardinality | Spread data evenly across partitions |
| Even access distribution | Avoid hot partitions (one partition gets all traffic) |
| Aligns with query patterns | Most queries filter by the partition key |
| Does not change after write | Partition key is immutable once written |

### Biotrackr Partition Key Examples

| Container | Partition Key | Rationale |
|---|---|---|
| Activity records | `/date` | Queries almost always filter by date; high cardinality |
| Sleep records | `/date` | Same date-based access pattern |
| Vitals records | `/date` | Same pattern — Withings data is date-keyed |
| Conversations | `/sessionId` | Each session is isolated; sessionId is unique |

**Teaching angle:** Using `/userId` as the partition key would concentrate all activity for
one user in one partition — a hot partition problem if a user is very active. Using `/date`
distributes writes across dates. If future multi-user support is added, a composite key
(`userId_date`) would be needed.

### Hot Partition Anti-Pattern

A hot partition occurs when a large fraction of requests target one partition key value.
Symptoms: throttling on one partition key despite low overall RU consumption.

**Fix:** Increase partition key cardinality by appending a suffix (synthetic sharding):
`date_shard` where shard = `hash(record_id) % N`.

## Caching Strategies

### Cache Patterns

| Pattern | Description | Use Case |
|---|---|---|
| Cache-aside (lazy) | App checks cache first; on miss, loads DB and populates cache | Read-heavy, tolerable staleness |
| Write-through | Write to cache and DB simultaneously | Read-heavy, strong consistency needed |
| Write-behind | Write to cache; async flush to DB | Write-heavy, eventual consistency acceptable |
| Read-through | Cache handles DB load on miss transparently | Simpler app logic |

### Biotrackr Anchor — CachingMcpToolWrapper

`CachingMcpToolWrapper` in `Biotrackr.Chat.Api` implements **cache-aside** for MCP tool
calls. Cache key = tool name + parameter hash; TTL varies by tool type.

**Teaching angle:** The cache is a hash map with expiry. Cache-aside is the most common
pattern because it is simple and degrades gracefully: on cache miss, the application falls
back to the source of truth. Write-through and write-behind add complexity only when
write performance is the bottleneck.

### Cache Invalidation Strategies

- **TTL (time-to-live):** simplest — data expires after a fixed duration
- **Event-driven invalidation:** write to DB triggers cache eviction (more complex)
- **Versioned keys:** append a version/hash to the key; old entries become unreachable

## Rate Limiting and Throttling

### Rate Limiting Algorithms

| Algorithm | Description | Burst Handling |
|---|---|---|
| Token bucket | Bucket refills at rate r; each request consumes 1 token | Allows bursts up to bucket size |
| Leaky bucket | Requests exit at fixed rate regardless of input rate | Smooths bursts |
| Fixed window counter | Count requests per time window; reset at window boundary | Boundary burst problem |
| Sliding window log | Track request timestamps; evict expired | Accurate, memory-intensive |

### Biotrackr Anchor — MCP Server Rate Limiting

`Biotrackr.Mcp.Server` enforces **100 requests/minute per IP, queue size 10** using ASP.NET
Core rate limiting middleware. This is a **token bucket** variant — each IP gets a refill
rate of 100/min with a queue buffer of 10 for short bursts.

```csharp
// Conceptual token bucket per IP
options.AddFixedWindowLimiter("mcp-rate-limit", opt =>
{
    opt.PermitLimit = 100;
    opt.Window = TimeSpan.FromMinutes(1);
    opt.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
    opt.QueueLimit = 10;
});
```

### Biotrackr Anchor — APIM Throttling Policies

Azure API Management applies quota and rate limit policies at the gateway layer before
requests reach the backend services. This is a distributed rate limiter: APIM counts
requests centrally and rejects early, protecting backend services from overload.

**Teaching angle:** A rate limiter is a **leaky bucket or token bucket** implemented as
a counter with a TTL. In a distributed system, the counter must be stored in a shared
store (Redis, APIM internal store) rather than in-process memory.

## Microservices Data Patterns

### Database-per-Service

Biotrackr follows a modified database-per-service pattern: each domain (Activity, Sleep,
Food, Vitals) has its own **Cosmos DB container** within a shared account, with separate
partition keys and read/write access isolated per service.

**Trade-offs:**

- Avoids coupling through a shared schema
- Cross-domain queries require service-to-service calls or read models
- Each service can scale its container independently

### Eventual Consistency and the CAP Theorem

CAP theorem: a distributed system can guarantee at most two of:

- **Consistency** — every read sees the most recent write
- **Availability** — every request gets a response (possibly stale)
- **Partition tolerance** — the system continues despite network splits

Cosmos DB is **CP** (consistent + partition tolerant) by default with strong consistency,
or **AP** (available + partition tolerant) with eventual consistency. Biotrackr uses
**session consistency** — reads see writes from the same logical session, balancing
performance and correctness.

### Event-Driven Data Flow

Biotrackr's ingest services (Activity.Svc, Sleep.Svc, Food.Svc, Vitals.Svc) write to
Cosmos DB asynchronously. Downstream APIs read from these containers. This is an
**event-sourcing-lite** pattern: the service writes the fact, and readers query when needed.

### API Gateway Pattern (APIM)

APIM acts as the single entry point for all external requests. From a DSA perspective,
APIM is a **routing table** — an O(1) lookup from request path to backend service — plus a
policy pipeline (a linked list of processing steps: auth → rate-limit → transform → route).

## Container Apps Scaling Triggers

Azure Container Apps scales on:

- **HTTP concurrency** — scale out when concurrent requests exceed threshold
- **CPU/memory** — scale out on resource saturation
- **KEDA-based** — scale on queue depth, Cosmos DB change feed lag, custom metrics

**Teaching angle:** Scaling triggers are **threshold functions** — binary decisions based
on a measured value crossing a boundary. The correct threshold depends on the latency
target and per-replica capacity, which requires profiling (load testing) rather than guessing.
