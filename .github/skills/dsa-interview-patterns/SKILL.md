---
name: dsa-interview-patterns
description: "Classic coding interview patterns including two pointers, sliding window, binary search variants, fast/slow pointers, prefix sums, interval merging, and monotonic stack. Use when: solving array or string problems under interview constraints, recognising which pattern fits a given input shape, implementing cursor-based pagination, analysing date-range or sorted time-series queries, reviewing LINQ chains for hidden complexity, or teaching pattern-first problem solving."
---

# DSA Interview Patterns

## When to Use

- Approaching a new problem and needing to identify which pattern applies
- Implementing or reviewing two-pointer, sliding window, or binary search logic
- Teaching pattern-first problem solving (recognise the shape, apply the template)
- Reviewing pagination cursor logic or date-range query strategies
- Analysing activity log aggregation for hidden O(n²) in prefix sum candidates
- Preparing for technical interviews with common pattern recognition

## Two Pointers

Two pointers move through a collection — usually from both ends toward the middle, or
both from the start at different speeds.

### Same-Direction (Fast / Slow)

```csharp
// Remove duplicates from sorted array in-place (O(n) time, O(1) space)
int slow = 0;
for (int fast = 1; fast < nums.Length; fast++)
{
    if (nums[fast] != nums[slow])
        nums[++slow] = nums[fast];
}
return slow + 1;
```

### Opposite-Direction (Converging)

```csharp
// Two-sum on sorted array (O(n) time)
int left = 0, right = nums.Length - 1;
while (left < right)
{
    int sum = nums[left] + nums[right];
    if (sum == target)  return (left, right);
    if (sum < target)   left++;
    else                right--;
}
```

### When to Use Two Pointers

- Input is sorted or can be sorted
- Looking for a pair / triplet satisfying a condition
- Removing elements in-place
- Comparing characters from both ends of a string (palindrome check)

## Sliding Window

A window is a contiguous sub-sequence. Sliding window avoids the O(n²) of checking
all pairs/windows by maintaining and updating a running state as the window slides.

### Fixed-Size Window

```csharp
// Maximum sum of any subarray of length k — O(n)
int windowSum = nums[..k].Sum();
int maxSum = windowSum;
for (int i = k; i < nums.Length; i++)
{
    windowSum += nums[i] - nums[i - k];
    maxSum = Math.Max(maxSum, windowSum);
}
```

### Variable-Size Window

```csharp
// Longest substring with at most k distinct characters — O(n)
int left = 0, maxLen = 0;
var freq = new Dictionary<char, int>();
for (int right = 0; right < s.Length; right++)
{
    freq[s[right]] = freq.GetValueOrDefault(s[right]) + 1;
    while (freq.Count > k)
    {
        freq[s[left]]--;
        if (freq[s[left]] == 0) freq.Remove(s[left]);
        left++;
    }
    maxLen = Math.Max(maxLen, right - left + 1);
}
```

### Biotrackr Anchor — Pagination as Sliding Window

`PaginationResponse<T>` in Biotrackr APIs is a fixed-size sliding window over a
sorted dataset. Cosmos DB continuation tokens are variable-size window cursors — the
boundary is a bookmark, not a numeric offset:

```
Fixed: pageNumber=3, pageSize=20 → offset = 40, length = 20
Cursor: continuationToken="{etag}" → start after last seen item
```

**Teaching angle:** Cursor-based pagination avoids "page drift" when items are inserted
mid-pagination. It is the variable-size window pattern: the window start is pinned to the
last seen item, not a fixed numeric offset.

## Binary Search

Binary search works on any **monotone function** — not just sorted arrays.
The key is: "does the condition hold for all values on one side?"

### Template

```csharp
// Generalised binary search — finds leftmost position where condition is true
int left = 0, right = boundary;
while (left < right)
{
    int mid = left + (right - left) / 2;
    if (Condition(mid)) right = mid;       // condition true → search left half
    else                left = mid + 1;    // condition false → search right half
}
return left; // leftmost true position
```

### Biotrackr Anchor — Date-Range Queries on Sorted Time-Series

Activity, sleep, and vitals data in Biotrackr are stored sorted by `date`. A range query
`/range/{startDate}/{endDate}` on sorted records is binary search applied twice:

```
find first index where date >= startDate   →  binary search left boundary
find last  index where date <= endDate     →  binary search right boundary
```

**Teaching angle:** Cosmos DB range queries on indexed `date` fields use a B-tree for
O(log n) boundary location — the same concept as binary search, implemented in a tree.

## Fast/Slow Pointers

Fast pointer moves at 2× the speed of slow. Useful for cycle detection and finding
the middle of a linked list.

```
Floyd's cycle detection:
slow moves 1 step at a time
fast moves 2 steps at a time

If they meet: cycle exists
If fast reaches null: no cycle
```

### When to Use Fast/Slow

- Detect cycle in a linked list (Floyd's algorithm)
- Find the middle node of a linked list
- Detect duplicate in an array treated as implicit linked list (Floyd's on values)

## Prefix Sum

A prefix sum array P where `P[i] = nums[0] + ... + nums[i]` allows O(1) range sum queries:

```csharp
// Build prefix sum — O(n)
int[] prefix = new int[nums.Length + 1];
for (int i = 0; i < nums.Length; i++)
    prefix[i + 1] = prefix[i] + nums[i];

// Query sum of [left, right] — O(1)
int rangeSum = prefix[right + 1] - prefix[left];
```

### Biotrackr Anchor — Activity Log Aggregation

`Biotrackr.Activity.Api` returns activity records by date. Aggregating total steps over
a date range without prefix sums requires summing each record in the range: O(k) where
k is the range size. With a pre-built prefix sum over sorted daily totals, any range
aggregate is O(1) after O(n) preprocessing.

**Teaching angle:** Build the prefix sum once; answer all range queries in O(1). The
trade-off is O(n) space. When you have many range queries on static data, prefix sums are
the right tool.

## Pattern Recognition Guide

Use the input shape to identify the right pattern:

| Input Shape | Common Patterns |
|---|---|
| Sorted array, find target | Binary search |
| Sorted array, find pair satisfying condition | Two pointers (converging) |
| Array, longest/smallest subarray satisfying condition | Sliding window |
| Linked list, detect cycle or find middle | Fast/slow pointers |
| Array, range sum queries | Prefix sum |
| Array of intervals, merge or insert | Sort by start, linear sweep |
| Matrix, find path or connected region | BFS or DFS |
| String, all permutations/combinations | Backtracking |
| Scheduling with priorities | Heap / priority queue |
| "Nearest greater element" | Monotonic stack |

## Monotonic Stack

A monotonic stack maintains elements in a consistently increasing or decreasing order.
Useful for "next greater/smaller element" problems in O(n).

```csharp
// Next greater element for each item — O(n)
var result = new int[nums.Length];
var stack = new Stack<int>(); // stores indices
for (int i = 0; i < nums.Length; i++)
{
    while (stack.Count > 0 && nums[stack.Peek()] < nums[i])
        result[stack.Pop()] = nums[i];
    stack.Push(i);
}
// Remaining indices have no greater element → result stays 0
```

**When to reach for monotonic stack:** "For each element, find the first element to its
left/right that is larger/smaller." This pattern appears in histogram problems, stock span,
and trapping rainwater.
