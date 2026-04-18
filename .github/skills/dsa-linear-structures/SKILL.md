---
name: dsa-linear-structures
description: "Arrays, strings, hash tables, linked lists, stacks, and queues — core linear data structures with complexity analysis, implementation patterns, and common problems. Use when: teaching linear data structures, reviewing code for data structure optimization, analyzing collection operation complexity, solving problems involving arrays or hash tables, implementing sliding window algorithms, or analyzing buffered/streamed data processing."
---

# DSA Linear Structures

## When to Use

- Teaching or reviewing arrays, lists, and their access patterns
- Explaining when a linked list beats an array and vice versa
- Analyzing code that uses LINQ chains over collections (hidden O(n log n) risk)
- Implementing or explaining sliding window patterns on sequential data
- Reviewing background job queues, request buffers, or paginated responses
- Discussing stack-based algorithms (balanced parentheses, expression evaluation)

## Arrays and Strings

### Key Properties

| Operation | Array (fixed) | `List<T>` (dynamic) |
|---|---|---|
| Index access | O(1) | O(1) |
| Append | N/A | O(1) amortized |
| Insert at index | O(n) | O(n) |
| Search (unsorted) | O(n) | O(n) |
| Search (sorted) | O(log n) binary search | O(log n) |

### Common Array Techniques

- **Two pointers:** left and right indices moving inward — useful for sorted arrays
- **Prefix sums:** precompute cumulative totals for O(1) range sum queries
- **In-place reversal:** swap elements without extra allocation

### String Manipulation Complexity

Building strings with `+` in a loop is O(n²) due to repeated allocation. Prefer `StringBuilder`
for concatenation inside loops.

```csharp
// O(n²) — allocates a new string each iteration
string result = "";
foreach (var item in items) result += item;

// O(n) — single allocation at the end
var sb = new StringBuilder();
foreach (var item in items) sb.Append(item);
string result = sb.ToString();
```

## Linked Lists

### Singly vs Doubly Linked

| Operation | Singly | Doubly |
|---|---|---|
| Prepend | O(1) | O(1) |
| Append (with tail pointer) | O(1) | O(1) |
| Delete known node | O(n) — needs predecessor | O(1) |
| Reverse traversal | Not possible | O(n) |

### When to Prefer Linked List over Array

- Frequent insertions/deletions at the head or middle with known node reference
- Implementing LRU cache eviction (doubly linked list + hash map)

**In .NET:** `LinkedList<T>` provides doubly linked behaviour. Most business logic should
prefer `List<T>` for cache-friendliness unless the linked structure is the point of the exercise.

### Biotrackr Anchor — Middleware as Linked List

The ASP.NET Core middleware pipeline in Biotrackr services (ToolPolicy → ConversationPersistence →
GracefulDegradation in Chat.Api) is structurally a **singly linked list of request handlers**:

```
Request → ToolPolicyMiddleware → ConversationPersistenceMiddleware → GracefulDegradationMiddleware → Handler
```

**Teaching angle:** Each middleware holds a reference to `next` — the Chain of Responsibility
pattern is a linked list of function references. Inserting a middleware at the wrong position
changes execution semantics, just like inserting at position i in a linked list changes traversal.

## Stacks and Queues

### Stack (LIFO)

```csharp
var stack = new Stack<int>();
stack.Push(1);   // O(1)
stack.Push(2);
int top = stack.Pop();   // O(1) — returns 2
```

Use cases: DFS traversal, undo/redo, balanced bracket validation, call stack simulation.

### Queue (FIFO)

```csharp
var queue = new Queue<string>();
queue.Enqueue("job-1");   // O(1)
queue.Enqueue("job-2");
string next = queue.Dequeue();   // O(1) — returns "job-1"
```

Use cases: BFS traversal, background job scheduling, rate limiting queues, request buffering.

### Biotrackr Anchor — Report Job Queue

In `Biotrackr.Reporting.Api`, health report generation runs as a background job. The
conceptual model is a FIFO queue:

```
enqueue(reportRequest) → [job-1, job-2, job-3] → dequeue → process → artifact
```

**Teaching angle:** Why a queue and not a stack? Report jobs should process in the order
they were requested (fairness). A stack would process the most recent request first
(LIFO), making early requests wait indefinitely — a form of starvation.

### Biotrackr Anchor — Cosmos DB Streaming as Queue Consumption

When Biotrackr services stream Cosmos DB query results using feed iterators, the pattern
mirrors queue consumption: items arrive in order, processed once, then discarded.

## Sliding Window Pattern

The sliding window avoids nested loops when the sub-problem involves a **contiguous
subsequence** of fixed or variable length.

### Fixed-Size Window Template

```
initialize window with first k elements
for i = k to n - 1:
    add element[i] to window
    remove element[i - k] from window
    update result
```

### Variable-Size Window Template

```
left = 0
for right = 0 to n - 1:
    expand window by including element[right]
    while window violates constraint:
        shrink window by excluding element[left]
        left++
    update result with current window [left, right]
```

### Biotrackr Anchor — Pagination as Sliding Window

`PaginationResponse<T>` across all Biotrackr APIs implements pagination using page number
and page size — a **fixed-size sliding window** over a sorted dataset:

| Concept | Pagination Equivalent |
|---|---|
| Window size | `pageSize` |
| Window start | `(pageNumber - 1) * pageSize` offset |
| Slide step | Increment `pageNumber` by 1 |
| Total items | `totalCount` in response |

**Teaching angle:** Cursor-based pagination (used in Cosmos DB continuation tokens) is a
**variable-size sliding window** — the window boundary is defined by a bookmark rather than
a fixed offset. This avoids the "page drift" problem when items are inserted during pagination.

## Hash Tables

### Dictionary vs HashSet

| Use Case | Type |
|---|---|
| Key → value mapping | `Dictionary<TKey, TValue>` |
| Membership test only | `HashSet<T>` |
| Ordered key access | `SortedDictionary<TKey, TValue>` |

### Collision Handling Concepts

- **Chaining:** bucket holds a linked list of colliding entries — worst case O(n) all in one bucket
- **Open addressing:** probe for the next open slot — better cache performance
- .NET `Dictionary<TKey, TValue>` uses chaining internally

### Biotrackr Anchor — CachingMcpToolWrapper

`CachingMcpToolWrapper` in `Biotrackr.Chat.Api` wraps MCP tool calls with an
in-memory cache. Conceptually, the cache is a **hash map with a TTL eviction policy**:

```
cache[toolName + params] → CachedResult { Value, ExpiresAt }
```

**Teaching angle:** A cache IS a hash map. The difference between `ConcurrentDictionary`
and a proper cache is just eviction logic. When cache entries grow unbounded, you have a
memory leak disguised as a performance optimization.
