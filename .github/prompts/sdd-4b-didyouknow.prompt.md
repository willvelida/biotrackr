---
description: "Build shared understanding by surfacing non-obvious insights from SDD artifacts"
argument-hint: "[slug=...] [artifact=spec|plan|tasks]"
---

# SDD Phase 4b: Did You Know?

Surface non-obvious insights from SDD artifacts to build shared understanding before implementation. Presents insights one at a time and immediately updates the relevant artifact after each discussion.

## Inputs

* ${input:slug}: (Optional) Slug from prior phases. Inferred from context if omitted.
* ${input:artifact}: (Optional) Which artifact to analyze: `spec`, `plan`, or `tasks`. If omitted, auto-detect the most recent relevant artifact.

## Step 0: Doctrine Resolution

Before starting, resolve project conventions:

1. Check for project doctrine files:
   * `docs/project-rules/` (constitution.md, rules.md, idioms.md, architecture.md)
   * `copilot-instructions.md`, `AGENTS.md`, `CONTRIBUTING.md`, `README.md`
2. If no doctrine found, scan the codebase for dependency manifests, build systems, and directory patterns.
3. Extract: build command, test command, coverage threshold, naming conventions, CI platform.
4. Unknown values become explicit `[TODO]` markers — never assume silently.

## Step 1: Load Artifact Context

1. Determine which artifact to analyze based on `{artifact}` or auto-detection:
   * `spec` → `.copilot-tracking/plans/{date}/{slug}/{slug}-spec.md`
   * `plan` → `.copilot-tracking/plans/{date}/{slug}/{slug}-plan.md`
   * `tasks` → Phase task tables within the plan file
2. Read the target artifact completely.
3. Read supporting artifacts (research dossier, clarifications, workshops, ADRs) for additional context.

## Step 2: Analyze from Multiple Perspectives

Analyze the artifact from these perspectives:

1. **Assumption Auditor** — identify unstated assumptions that could cause surprises during implementation.
2. **Edge Case Scout** — find boundary conditions, error paths, or unusual states not explicitly covered.
3. **Dependency Detective** — surface hidden dependencies between tasks, modules, or external systems.
4. **Convention Compass** — flag areas where the plan diverges from established project patterns.
5. **Risk Radar** — identify risks that were downplayed or missing from the risk analysis.

Select the **5 most impactful insights** from across all perspectives.

## Step 3: Present Insights One at a Time

For each insight:

1. Present the insight with context and evidence.
2. Offer 2-4 response options:
   * "Good catch — update the artifact"
   * "Already considered — no change needed"
   * "Needs more research — mark as open question"
   * "Skip this one"
3. **Wait for the user's response before presenting the next insight.**
4. If the user chooses to update, **immediately apply the change** to the relevant artifact. Do not defer updates to the end.

### Insight Format

```markdown
### Did You Know? #{N}

**Perspective:** {perspective name}
**Artifact:** {file path}
**Finding:** {the insight}

**Why it matters:** {impact on implementation if not addressed}

**Evidence:** {file:line reference or artifact section}

**Options:**
1. Update the artifact with this insight
2. Already considered — no change
3. Needs more research
4. Skip
```

## Step 4: Summary

After all insights are presented (or the user stops early):

1. List which insights were accepted and which artifacts were updated.
2. List any insights marked for further research.
3. Note any new open questions added to the spec.

---

> [!IMPORTANT]
> **STOP** after presenting each insight. Wait for user response before continuing. Updates are applied immediately, not batched.