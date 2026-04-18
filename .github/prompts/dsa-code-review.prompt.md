---
description: "Review C# code for DSA anti-patterns and recommend algorithmic improvements"
agent: "DSA Mentor"
argument-hint: "filepath or paste code snippet"
---

Review the provided C# code for DSA anti-patterns and algorithmic inefficiencies.

## Review Areas

### Collection Type Selection

- `List<T>` used where `HashSet<T>` or `Dictionary<TKey, TValue>` would reduce lookup from O(n) to O(1)
- `Queue<T>` or `Stack<T>` missed where FIFO/LIFO semantics are needed
- `SortedSet<T>` or `SortedDictionary<TKey, TValue>` missed where ordering matters

### Algorithmic Complexity

- Nested loops producing O(n²) or worse where a linear or O(n log n) approach exists
- Redundant sorts applied to already-sorted data or data that does not need ordering
- Repeated linear scans where a pre-built lookup or index would reduce total work
- Recursive calls without memoization on overlapping subproblems

### LINQ Misuse

- `OrderBy().First()` where `MinBy()` / `MaxBy()` achieves O(n) instead of O(n log n)
- `.Where().Count()` where `.Count(predicate)` is sufficient
- Chained `.ToList()` calls materialising intermediate sequences unnecessarily
- `.Contains()` on a `List<T>` where the source should be a `HashSet<T>`

### Two-Pointer and Sliding-Window Opportunities

- Paired index traversal written as nested loops
- Substring or range aggregation recomputed from scratch each iteration

## Output Format

Produce a findings table followed by corrected code for each flagged location.

| Location | Issue | Severity | Recommendation |
|----------|-------|----------|----------------|
| MethodName (line N) | Description of the anti-pattern | Critical / High / Medium / Low | Suggested fix |

After the table, provide a corrected code block for every Critical and High finding, with inline comments explaining the improvement and its complexity impact.

Conclude with a summary: total findings by severity and the estimated overall complexity improvement (e.g., "reduces worst-case from O(n²) to O(n)").
