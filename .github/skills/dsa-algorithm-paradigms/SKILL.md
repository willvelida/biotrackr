---
name: dsa-algorithm-paradigms
description: "Core algorithm design paradigms including dynamic programming, greedy algorithms, divide and conquer, sorting, searching, and backtracking with decision guidance and trade-off analysis. Use when: selecting an algorithm paradigm for a new problem, teaching dynamic programming memoisation vs tabulation, reviewing greedy algorithm correctness, explaining divide-and-conquer recursion, analysing sorting algorithm choices, or recognising which algorithmic strategy applies to a given problem shape."
---

# DSA Algorithm Paradigms

## When to Use

- Selecting the right algorithm paradigm before writing code
- Teaching DP (memoisation vs tabulation) or explaining overlapping sub-problems
- Reviewing greedy code for correctness (does local optimum imply global optimum?)
- Analysing divide-and-conquer recursion depth and merge cost
- Explaining sorting algorithm trade-offs (stability, in-place, adaptive)
- Recognising backtracking as constrained exhaustive search

## Sorting and Searching

### Sorting Algorithm Reference

| Algorithm | Best | Average | Worst | Space | Stable? |
|---|---|---|---|---|---|
| Merge sort | O(n log n) | O(n log n) | O(n log n) | O(n) | Yes |
| Quick sort | O(n log n) | O(n log n) | O(n²) | O(log n) | No |
| Heap sort | O(n log n) | O(n log n) | O(n log n) | O(1) | No |
| Insertion sort | O(n) | O(n²) | O(n²) | O(1) | Yes |
| Introspective sort (.NET `Array.Sort()` / `List<T>.Sort()`) | O(n log n) | O(n log n) | O(n log n) | O(log n) | No (not guaranteed) |

.NET's `Array.Sort()` and `List<T>.Sort()` use an introspective sort variant and are not guaranteed stable. If you need a stable sort in .NET, LINQ's `OrderBy` is stable.

### Binary Search

Binary search requires a **sorted collection** and halves the search space at each step.

```csharp
// Canonical binary search template
int left = 0, right = array.Length - 1;
while (left <= right)
{
    int mid = left + (right - left) / 2;  // avoids integer overflow
    if (array[mid] == target) return mid;
    if (array[mid] < target)  left = mid + 1;
    else                      right = mid - 1;
}
return -1; // not found
```

**Common variants:**

- Find first occurrence: when `array[mid] == target`, set `right = mid - 1` and continue
- Find insertion point: return `left` after the loop
- Search on a function: binary search the answer space, not an array

## Dynamic Programming

### Two Signs That DP Applies

1. **Overlapping sub-problems** — the same smaller problem is solved multiple times
2. **Optimal substructure** — the optimal solution to the full problem is built from optimal sub-solutions

### Memoisation (Top-Down)

```csharp
var memo = new Dictionary<int, long>();

long Fib(int n)
{
    if (n <= 1) return n;
    if (memo.TryGetValue(n, out var cached)) return cached;
    memo[n] = Fib(n - 1) + Fib(n - 2);
    return memo[n];
}
```

- Natural recursive structure — add cache to existing recursion
- Space: O(n) cache + O(n) call stack

### Tabulation (Bottom-Up)

```csharp
long[] dp = new long[n + 1];
dp[0] = 0; dp[1] = 1;
for (int i = 2; i <= n; i++)
    dp[i] = dp[i - 1] + dp[i - 2];
```

- Iterative, no call stack overhead
- Space: O(n); often reducible to O(1) for linear recurrences

### Biotrackr Anchor — Report Caching as DP

`CachingMcpToolWrapper` stores tool results keyed by tool name + parameter hash. This is
tabulated DP applied to AI tool calls: if the same tool call (same inputs) was made before,
return the cached result rather than re-executing.

**Teaching angle:** Memoisation and caching are the same idea at different scales. DP memoises
recursive sub-problem results; a service cache memoises expensive computation results.
Both trade space for time.

## Greedy Algorithms

### When Greedy Works

A greedy algorithm makes the locally optimal choice at each step, hoping to reach a globally
optimal solution. It is correct only when the **greedy choice property** holds: local optima
compose into a global optimum.

### Classic Greedy Problems

- Activity selection (interval scheduling maximisation)
- Dijkstra's shortest path (greedy on distances)
- Huffman encoding (greedy on frequencies)
- Fractional knapsack

### Biotrackr Anchor — Quota Allocation

Azure Container Apps and APIM apply greedy throttling: allow a request if quota remains,
reject immediately if not. This is a greedy policy — no lookahead, no reservation.

**Teaching angle:** Greedy works here because quota is consumed in the order of arrival and
there is no future information that would change the decision. If the problem were "maximise
the number of high-priority requests served given a quota window", greedy fails — you would
need DP or a priority queue.

## Divide and Conquer

### Pattern

1. **Divide** — split the problem into independent sub-problems
2. **Conquer** — solve each sub-problem recursively
3. **Combine** — merge sub-results into the final answer

### Recurrence Analysis (Master Theorem)

For T(n) = aT(n/b) + O(n^d):

- a > b^d: T(n) = O(n^log_b(a))
- a = b^d: T(n) = O(n^d log n)
- a < b^d: T(n) = O(n^d)

Merge sort: a=2, b=2, d=1 → a = b^d → O(n log n).

### Biotrackr Anchor — Parallel Service Health Checks

Health checks across 14 Biotrackr services can run in parallel then be merged — divide
and conquer on the service graph:

```csharp
// Conquer: each check runs independently
var tasks = services.Select(s => CheckHealthAsync(s));

// Combine: aggregate results
var results = await Task.WhenAll(tasks);
```

**Teaching angle:** Divide-and-conquer is why `Task.WhenAll` is faster than sequential
awaits. You pay O(max individual duration) instead of O(sum of all durations). The
"merge" step is trivial (collect results), so the paradigm reduces to parallel fan-out.

## Backtracking

### Pattern

Backtracking is **constrained exhaustive search** — explore all candidate solutions,
prune branches that violate constraints early.

```
def backtrack(state, candidates):
    if is_solution(state): record(state); return
    for each candidate in candidates:
        if is_valid(state + candidate):
            apply(candidate)
            backtrack(state + candidate, remaining)
            undo(candidate)           // backtrack
```

Use for: permutations, combinations, N-Queens, Sudoku, constraint satisfaction problems.

**Note:** Backtracking is inherently exponential — it is only practical when pruning
significantly reduces the search space.

## Recognising Which Paradigm to Apply

| Problem Signal | Likely Paradigm |
|---|---|
| Overlapping sub-problems, optimal substructure | Dynamic Programming |
| Local choice leads to global optimum (provable) | Greedy |
| Split into independent equal sub-problems | Divide and Conquer |
| "All valid combinations / arrangements" | Backtracking |
| Sorted input, find target | Binary Search |
| Need all items sorted once | Merge/Tim sort |
| Streaming data, min/max per window | Monotonic deque or heap |
