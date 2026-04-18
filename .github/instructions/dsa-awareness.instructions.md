---
description: "DSA awareness conventions for Biotrackr C# code. Use when: reviewing C# code for algorithmic efficiency, choosing data structures, or optimizing LINQ operations."
applyTo: "**/*.cs"
---

# DSA Conventions

## Data Structure Selection

- Use `HashSet<T>` for membership checks — O(1) average vs O(n) for `List<T>.Contains()`
- Use `Dictionary<TKey, TValue>` for key-based lookup — O(1) average vs O(n) for list scan
- Use `Queue<T>` for FIFO processing; use `Stack<T>` for LIFO or undo/redo patterns
- Use `SortedDictionary<TKey, TValue>` or `SortedSet<T>` only when ordered iteration is required — adds O(log n) insert/remove cost
- Pre-size collections when capacity is known: `new List<T>(expectedCount)` avoids O(n) resizing

## LINQ Complexity

- `.Where()` is O(n) — acceptable for filtering
- `.OrderBy()` is O(n log n) — avoid when only the extreme value is needed
- Prefer `MinBy(x => x.Prop)` or `MaxBy(x => x.Prop)` over `.OrderBy(x => x.Prop).First()` — O(n) vs O(n log n)
- `.GroupBy()` is O(n) in time but O(n) in space — be aware of allocations on large result sets
- `.ToList()` or `.ToArray()` at the end of a chain forces immediate evaluation — only materialize when needed

## Nested Loops

- Flag any nested loop as a potential O(n²) hotspot; verify the inner collection is bounded or constant-size
- Replace inner `List<T>.Contains()` with a pre-built `HashSet<T>` lookup when the set does not change between iterations
- Consider whether the problem can be solved with a single-pass hash table instead of two nested passes

## Collection Initialization

- When building a lookup from a sequence, prefer `.ToDictionary()` or `.ToHashSet()` over repeated `.FirstOrDefault()` calls inside a loop
- Avoid calling `.Count()` or `.Any()` on `IEnumerable<T>` multiple times — materialize once or use the `Count` property on typed collections

## Complexity Annotations

- When a method performs non-trivial algorithmic work, add a brief inline note: `// O(n log n) — sorted by date`
- Include space complexity notes when allocations grow with input size: `// O(n) space — builds lookup dictionary`
