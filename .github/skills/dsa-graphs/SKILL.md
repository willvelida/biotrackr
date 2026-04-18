---
name: dsa-graphs
description: "Graph representations, BFS, DFS, topological sort, shortest path algorithms, and dependency analysis. Use when: teaching graph theory, analyzing service dependency topologies, implementing BFS or DFS traversal, detecting cycles in dependency graphs, performing topological ordering of tasks, or explaining directed acyclic graph (DAG) patterns in microservice or workflow architectures."
---

# DSA Graphs

## When to Use

- Explaining graph representations and when to choose each
- Analyzing microservice communication topology for cycles or bottlenecks
- Teaching BFS (shortest path, level-order) vs DFS (cycle detection, topological sort)
- Modelling workflow state machines as directed graphs
- Understanding API Management routing paths and middleware chains as directed sequences
- Reviewing code that could benefit from topological ordering (dependency resolution)

## Graph Representation

A graph G = (V, E) has vertices V and edges E.

### Adjacency List (Preferred for Sparse Graphs)

```csharp
// Dictionary maps each vertex to its list of neighbors
var graph = new Dictionary<string, List<string>>
{
    ["A"] = ["B", "C"],
    ["B"] = ["D"],
    ["C"] = ["D"],
    ["D"] = []
};
```

- Space: O(V + E)
- Edge check: O(degree of vertex)
- Best for: most real-world graphs (sparse)

### Adjacency Matrix (For Dense Graphs)

- Space: O(V²)
- Edge check: O(1)
- Best for: dense graphs where nearly all pairs are connected

### Biotrackr Anchor — Microservice Dependency Graph

Biotrackr's 14 services form a **directed dependency graph**:

```
UI → Chat.Api → Mcp.Server → [Activity.Api, Sleep.Api, Food.Api, Vitals.Api]
UI → Activity.Api, Sleep.Api, Food.Api, Vitals.Api
Reporting.Api → [Activity.Api, Sleep.Api, Vitals.Api]
```

**Teaching angle:** A cycle in this graph (Service A depends on B, B depends on A) causes
a startup deadlock or circular dependency injection failure. Topological sort detects cycles
before they reach production.

## BFS and DFS

### Breadth-First Search (BFS)

Uses a **queue**. Visits all nodes at distance d before any at distance d+1.

```
queue = [start]
visited = {start}
while queue not empty:
    node = dequeue
    for each neighbor of node:
        if neighbor not in visited:
            visited.add(neighbor)
            enqueue(neighbor)
```

- Time: O(V + E)
- Space: O(V) for the queue
- Use for: shortest path in unweighted graph, level-order traversal, component discovery

### Depth-First Search (DFS)

Uses a **stack** (or recursion). Explores as deep as possible before backtracking.

```
def dfs(node, visited):
    visited.add(node)
    for each neighbor of node:
        if neighbor not in visited:
            dfs(neighbor, visited)
```

- Time: O(V + E)
- Space: O(V) call stack (recursive)
- Use for: cycle detection, topological sort, strongly connected components, maze solving

### BFS vs DFS Decision

| Requirement | Algorithm |
|---|---|
| Shortest path (unweighted) | BFS |
| Detect cycle in directed graph | DFS with color marking |
| Topological ordering | DFS post-order |
| Find connected components | Either (DFS simpler) |
| Closest node to source | BFS |

## Topological Sort

Topological sort orders nodes of a **DAG** (directed acyclic graph) so that for every
directed edge u → v, u appears before v. Undefined if the graph has a cycle.

### Kahn's Algorithm (BFS-based)

```
1. Compute in-degree for every node
2. Enqueue all nodes with in-degree 0
3. While queue not empty:
     node = dequeue → add to result
     for each neighbor: decrement in-degree
     if in-degree becomes 0: enqueue
4. If result.length < V: cycle detected
```

### Biotrackr Anchor — Middleware Pipeline Ordering

The Chat.Api middleware pipeline must execute in strict order:
`ToolPolicyMiddleware → ConversationPersistenceMiddleware → GracefulDegradationMiddleware`.

This is a **topological constraint** — each stage depends on the previous one having run.
If GracefulDegradation ran first, exceptions from the conversation layer would be swallowed
before persistence could record the conversation state.

**Teaching angle:** Build system dependency resolution (NuGet, npm) uses topological sort.
Package A depends on B and C; C depends on B. The install order must respect these edges.
A circular dependency (A→B→A) is detected as a cycle and rejected.

## Shortest Path

### Dijkstra's Algorithm (Non-Negative Weights)

- Uses a min-heap priority queue
- Time: O((V + E) log V)
- Use for: weighted routing, minimizing latency across service hops

### Bellman-Ford (Handles Negative Weights)

- Time: O(V × E)
- Also detects negative-weight cycles

### BFS for Unweighted Graphs

When all edges have equal weight, BFS produces the shortest path in O(V + E) — faster
than Dijkstra for uniform-cost graphs.

## Practical Applications

### Biotrackr Anchor — APIM Routing as Directed Graph

APIM routes incoming requests through a sequence of policies (JWT validation →
rate limiting → backend routing). Each policy is a node; execution order is a
directed edge. Misconfiguring policy execution order is a **directed graph problem**:
the wrong topological ordering changes security guarantees.

### Biotrackr Anchor — Report Generation as State Machine (Directed Graph)

Report generation workflow states:

```
Queued → Running → CodeValidation → ReviewPending → Complete
                       ↓
                    Rejected
```

A state machine is a **directed graph** where nodes are states and edges are transitions.
Cycle detection ensures no infinite loop between states (e.g., repeated re-queuing).

### Cycle Detection in Directed Graph

```
White = unvisited, Gray = in current DFS path, Black = fully processed

def has_cycle(node, colors):
    colors[node] = Gray
    for neighbor in graph[node]:
        if colors[neighbor] == Gray: return True  # back edge = cycle
        if colors[neighbor] == White:
            if has_cycle(neighbor, colors): return True
    colors[node] = Black
    return False
```
