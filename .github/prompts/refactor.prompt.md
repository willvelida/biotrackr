---
description: "Refactor code with pre/post regression verification"
agent: "C# Expert"
argument-hint: "Refactoring scope and goal (e.g., 'Extract date validation into shared helper in Activity.Api')"
---

Refactor code in a Biotrackr service with baseline capture and regression verification to ensure no tests are lost or broken.

## Workflow

### 1. Capture Baseline (deterministic)

Run in the service directory before making any changes:

```bash
# audit-exempt: not-a-gate — captures the baseline test total, does not pass/fail
dotnet test --no-build
```

This step reports a count rather than gating, which is why it calls `dotnet test` directly: `scripts/verify.sh` communicates through its exit code and does not print a test total. Every later step in this prompt is a gate and uses the script.

Record the baseline test count and pass/fail status. This is the regression target — test count must not decrease.

### 2. Assess Scope

Identify the refactoring scope:

- Files and methods affected
- Risk assessment (low: rename/extract, medium: restructure, high: change interfaces)
- Dependencies on the code being refactored

### 3. Implement Refactoring

Apply the refactoring incrementally. Prefer small, focused changes over large rewrites.

### 4. Verify (deterministic)

```bash
bash scripts/verify.sh Biotrackr.{Domain}.{Type}
```

Exit 0 passed. Exit 2 means the code is broken; fix it before proceeding. Exit 1 means the check could not run, which is an environment problem, not a regression.

### 5. Regression Check

Compare the test total against the baseline captured in step 1.

Verify:

- All tests pass
- Test count is equal to or greater than the baseline from Step 1

### 6. Compare Results

Compare post-refactoring test results against the baseline:

- If test count decreased, investigate which tests were lost and why
- If tests fail, identify whether the failure is a regression from the refactoring

### 7. Fix Regressions (bounded retry)

If tests fail or test count decreased, diagnose and fix. **Maximum 2 retry cycles** through steps 4-5.

### 8. Escalation

**STOP** if the baseline test count cannot be maintained after 2 retries. Present:

- Baseline vs current test count
- Which tests broke or were removed
- What was attempted
- Assessment of the root cause

## Deliverables

- Refactored code that compiles cleanly
- All baseline tests passing with no decrease in test count
- Summary of the refactoring changes and their rationale
