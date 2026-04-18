---
description: "Design an algorithm or data structure solution for a given feature requirement"
agent: "DSA Mentor"
argument-hint: "problem statement or feature requirement"
---

Walk through a structured algorithm design for the provided problem statement or feature requirement.

## Design Walkthrough

### Step 1: Problem Decomposition

Restate the problem in your own words. Identify:

- Input types, sizes, and constraints
- Output requirements
- Edge cases and failure modes
- Any implicit ordering, uniqueness, or range guarantees

### Step 2: Paradigm Selection

Identify candidate algorithm paradigms (e.g., sliding window, two pointers, divide and conquer, dynamic programming, greedy, BFS/DFS). For each candidate:

- State why it is or is not a good fit
- Note the data structure that supports it

State the chosen paradigm and justify the decision.

### Step 3: Pseudocode

Write clear pseudocode for the chosen approach. Use named steps, not inline comments. The pseudocode must be language-agnostic but readable to a C# developer.

```
FUNCTION SolveProblem(input):
    ...
```

### Step 4: C# Implementation

Translate the pseudocode into idiomatic C# targeting .NET 10. Follow Biotrackr conventions: PascalCase methods, `_camelCase` private fields, `ArgumentNullException.ThrowIfNull` for null guards.

```csharp
// Implementation here
```

### Step 5: Big-O Analysis

| Phase | Time Complexity | Space Complexity | Notes |
|-------|-----------------|------------------|-------|
| Preprocessing | O(...) | O(...) | |
| Main algorithm | O(...) | O(...) | |
| Total | O(...) | O(...) | |

### Step 6: Alternative Approaches

List one or two alternative approaches not chosen, with a brief note on their trade-offs compared to the selected solution.

### Step 7: Test Cases

Provide at minimum: one typical case, one edge case (empty/null/boundary), and one performance-relevant case. For each: input, expected output, and why the case matters.
