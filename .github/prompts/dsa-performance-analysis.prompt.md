---
description: "Profile C# code for time and space complexity and suggest targeted optimisations"
agent: "DSA Mentor"
argument-hint: "filepath or paste code snippet"
---

Perform a time and space complexity audit of the provided C# code.

## Analysis Structure

### Overall Complexity Class

State the dominant time and space complexity for the code as a whole. Classify it:

- **Efficient:** O(1), O(log n), O(n), O(n log n)
- **Caution:** O(n²)
- **Problematic:** O(n³) or worse, or unbounded allocation

### Per-Method Breakdown

For each non-trivial method or code block, produce one row in the table below.

| Method / Block | Time Complexity | Space Complexity | Key Driver |
|----------------|-----------------|------------------|------------|
| MethodName | O(...) | O(...) | What causes this complexity |

### Bottleneck Identification

Identify the two or three highest-impact bottlenecks — the methods or patterns that dominate overall cost. For each bottleneck:

- Current complexity and why
- Whether the input size makes this bottleneck observable in production (estimate realistic n for this service)
- Root cause (wrong data structure, unnecessary sort, redundant traversal, missing cache, etc.)

### Optimisation Suggestions

For each identified bottleneck, provide a concrete suggestion:

| Bottleneck | Current | Suggested Change | Expected Improvement |
|------------|---------|-----------------|----------------------|
| Description | O(...) | What to change and how | New complexity |

Provide a corrected code snippet for every High and Critical optimisation suggestion.

### Summary

- Total methods reviewed
- Methods already efficient (no change needed)
- Methods with suggested improvements
- Estimated overall improvement if all suggestions are applied (e.g., "reduces hot path from O(n²) to O(n log n)")
