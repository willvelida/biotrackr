---
name: dsa-trees-and-heaps
description: "Binary trees, BSTs, heaps, priority queues, tries, and tree traversal patterns with complexity analysis and practical trade-offs. Use when: teaching tree or heap data structures, analyzing priority queue implementations, reviewing BFS or DFS traversal code, optimizing sorted collection usage, explaining BST vs heap trade-offs, or modeling hierarchical data."
---

# DSA Trees and Heaps

## When to Use

- Teaching binary tree structure, traversal, or balancing concepts
- Explaining heap / priority queue semantics and implementation
- Reviewing code that sorts to find a min/max (O(n log n) vs O(n) heap alternative)
- Analyzing Cosmos DB index structures or hierarchical partition key design
- Modeling prioritized background job queues
- Explaining BFS vs DFS traversal choice

## Binary Trees and BST

### Binary Tree Fundamentals

A binary tree is a hierarchical structure where each node has at most two children (left, right).
Tree height *h* determines traversal cost:

- **Balanced tree:** h = O(log n) — optimal
- **Degenerate tree (skewed):** h = O(n) — degrades to linked list performance

### Binary Search Tree (BST)

BST invariant: `left.value < node.value < right.value` for every node.

| Operation | Average (balanced) | Worst (skewed) |
|---|---|---|
| Search | O(log n) | O(n) |
| Insert | O(log n) | O(n) |
| Delete | O(log n) | O(n) |
| Min/Max | O(log n) | O(n) |

Self-balancing variants (AVL, Red-Black) maintain O(log n) guarantees.
.NET `SortedDictionary<TKey, TValue>` uses a red-black tree internally.

### Biotrackr Anchor — Cosmos DB B-Tree Index

Cosmos DB builds a **B-tree index** over indexed properties. A B-tree is a
self-balancing tree generalizing BSTs to many children per node, optimized for disk I/O.

```
GET /activity/{date}   →   index lookup on 'date' field   →   O(log n) page reads
```

**Teaching angle:** A missing index forces a full container scan — O(n). Adding an index
converts repeated queries to O(log n). The partition key is a first-level hash dispatch
(O(1)), and the index is a second-level tree lookup (O(log n)) within the partition.

## Heaps and Priority Queues

### Min-Heap / Max-Heap

A heap is a **complete binary tree** where:

- **Min-heap:** parent ≤ children (root is the minimum element)
- **Max-heap:** parent ≥ children (root is the maximum element)

| Operation | Complexity |
|---|---|
| Insert (push) | O(log n) |
| Peek min/max | O(1) |
| Extract min/max (pop) | O(log n) |
| Build heap from n elements | O(n) |

.NET provides `PriorityQueue<TElement, TPriority>` (min-heap by default).

### Biotrackr Anchor — Priority-Ordered Report Jobs

A report generation queue where urgent reports (user-initiated) preempt scheduled
reports (background batch) maps directly to a **min-heap priority queue**:

```csharp
// Lower priority number = higher urgency
var jobQueue = new PriorityQueue<ReportJob, int>();
jobQueue.Enqueue(new ReportJob("batch-summary"), priority: 10);
jobQueue.Enqueue(new ReportJob("user-request"),  priority: 1);

var next = jobQueue.Dequeue(); // returns "user-request" — O(log n)
```

**Teaching angle:** Why not sort the list each time? Sorting is O(n log n). A heap
maintains the invariant at O(log n) per insert/extract — dramatically cheaper when jobs
arrive continuously.

### Biotrackr Anchor — MinBy/MaxBy vs Sort

In Biotrackr, `weightGroups.OrderByDescending(mg => mg.Date).First()` sorts to find
the latest record — O(n log n). The same result using `MaxBy()` is O(n):

```csharp
// O(n log n) — sorts everything to get one item
var latest = weightGroups.OrderByDescending(mg => mg.Date).First();

// O(n) — single pass, no sort
var latest = weightGroups.MaxBy(mg => mg.Date);
```

**Teaching angle:** LINQ's `MaxBy()`/`MinBy()` are O(n) linear scans. They do not build
a heap internally — they just track the running maximum in a single pass. A heap is only
warranted when you need repeated extract-min/max operations on a dynamic collection.

## Traversal Patterns

### In-Order, Pre-Order, Post-Order (DFS)

```
In-Order  (Left → Root → Right): produces sorted output for a BST
Pre-Order (Root → Left → Right): useful for tree serialisation
Post-Order(Left → Right → Root): useful for deletion or bottom-up evaluation
```

All three are O(n) — every node visited exactly once.

### BFS (Level-Order)

Uses a **queue** to visit nodes level by level:

```
queue = [root]
while queue not empty:
    node = dequeue
    process(node)
    enqueue(node.left), enqueue(node.right) if not null
```

BFS is the right choice when you need the **shortest path** in an unweighted tree or
the **minimum depth** of the tree.

## Decision: Heap vs BST vs Sorted List

| Requirement | Use |
|---|---|
| Repeated extract-min/max from dynamic set | `PriorityQueue<T>` (heap) |
| Ordered iteration + search by key | `SortedDictionary<TKey, TValue>` (BST) |
| One-time sort for stable ordered output | `List<T>.Sort()` or `OrderBy()` |
| Sliding window max/min in O(n) | Monotonic deque (see dsa-interview-patterns) |
| Merge k sorted sequences | Min-heap with k entries |

## Tries (Prefix Trees)

A trie stores strings character-by-character, sharing common prefixes.

- Insert and search: O(m) where m = string length — independent of number of strings
- Best for: autocomplete, prefix matching, IP routing tables

**Biotrackr relevance (conceptual):** APIM routing rules share a prefix-match structure.
Routing `/activity/*` vs `/sleep/*` vs `/food/*` can be modelled as a trie over URL path
segments — common prefix `/` shared at root, domain segment splits at depth 1.
