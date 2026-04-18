---
name: dsa-foundations
description: "Big-O complexity analysis, recursion, foundational data structures, and problem decomposition for engineers without a formal CS background. Use when: analyzing algorithm complexity, performing Big-O analysis, selecting data structures, reviewing code for structural efficiency, teaching recursion fundamentals, or explaining time and space trade-offs."
---

# DSA Foundations

## When to Use

- Explaining or evaluating time and space complexity of any algorithm
- Choosing between data structure options (array vs. hash map vs. set)
- Teaching or reviewing recursion and its relationship to the call stack
- Decomposing an unfamiliar problem into known sub-problems
- Reviewing code for structural inefficiency (nested loops, redundant scans)

## Computational Complexity (Big-O)

Big-O notation describes the **worst-case growth rate** of an algorithm as input size *n* grows.

### Common Complexities

| Notation | Name | Example |
|----------|------|---------|
| O(1) | Constant | Dictionary key lookup, array index access |
| O(log n) | Logarithmic | Binary search, balanced BST operations |
| O(n) | Linear | Single loop scan, `List<T>.Contains()` |
| O(n log n) | Linearithmic | Merge sort, `OrderBy()` in LINQ |
| O(n²) | Quadratic | Nested loops, naive duplicate detection |
| O(2ⁿ) | Exponential | Recursive subset enumeration |

### Space Complexity

Space complexity measures **auxiliary memory** allocated by an algorithm — not the input size itself.

- A recursive function with depth *d* uses O(d) call stack space
- Building a dictionary from a list uses O(n) extra space to gain O(1) lookups

## Foundational Data Structures

### Arrays

- Contiguous memory, O(1) random access by index
- Insertion/deletion at middle: O(n) due to shifting
- Best for: fixed-size indexed collections, cache-friendly iteration

### Stacks

- LIFO (Last In, First Out)
- Push/pop: O(1)
- Best for: undo history, depth-first traversal, call stack simulation

### Queues

- FIFO (First In, First Out)
- Enqueue/dequeue: O(1) with a proper implementation
- Best for: BFS, background job scheduling, request buffering

### Hash Maps (Dictionary / HashSet)

- Average O(1) insert, lookup, delete via hashing
- Worst case O(n) with hash collisions (rare with good hash functions)
- Best for: frequency counting, deduplication, fast membership checks

### Sets

- Unordered collection of unique values
- O(1) membership test (HashSet), O(log n) for sorted sets (SortedSet)
- Best for: deduplication, intersection / union operations

## Biotrackr Anchor Examples

### O(1) Lookup vs O(n) Scan

In `WithingsWeightAdapter`, measurements are converted from a list to a dictionary:

```csharp
// O(n) to build, then O(1) per lookup
var measuresByType = measurements.ToDictionary(m => m.Type, m => m);
```

**Teaching angle:** Why not just scan the list each time? If you have 10 measurements and
call `.FirstOrDefault(m => m.Type == x)` five times, that is 5 × O(n) = O(5n) ≈ O(n) total.
With a dictionary, the five lookups are 5 × O(1). The up-front O(n) build cost pays for itself
after the first reuse.

### Partition Key Routing as O(1) Access

Cosmos DB partitioning in Biotrackr is based on logical keys such as `/documentType` for
the shared `records` container and `/sessionId` for `conversations`. Routing a query to
the correct partition is O(1) hash dispatch — no full-container scan needed.

**Teaching angle:** The partition key is a hash map key. Choosing a poor partition key
(for example, one with low cardinality or one that creates hot partitions) degrades
effective lookup from O(1) to O(n/k) where k is the number of distinct partitions.

## Complexity Selection Guidelines

Use this table when deciding which structure to reach for first:

| Primary Need | Preferred Structure | Why |
|---|---|---|
| Fast lookup by key | `Dictionary<TKey, TValue>` | O(1) average |
| Ordered iteration | `SortedDictionary` or `List` + sort | O(log n) / O(n log n) |
| Uniqueness enforcement | `HashSet<T>` | O(1) add/contains |
| LIFO processing | `Stack<T>` | O(1) push/pop |
| FIFO processing | `Queue<T>` or `Channel<T>` | O(1) enqueue/dequeue |
| Max/min tracking | `PriorityQueue<T>` | O(log n) push, O(log n) pop |

## Recursion Fundamentals

Every recursive function needs:

1. **Base case** — the condition that stops recursion
2. **Recursive case** — reduces the problem toward the base case
3. **No shared mutable state** — each call frame is independent

```csharp
// Canonical pattern
int Factorial(int n)
{
    if (n <= 1) return 1;          // base case
    return n * Factorial(n - 1);   // recursive case
}
```

**Call stack cost:** Each recursive call adds a stack frame. A depth of *n* uses O(n)
stack space. For large *n*, prefer iteration or tail-call patterns to avoid stack overflow.
