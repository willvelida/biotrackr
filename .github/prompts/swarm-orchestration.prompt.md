---
description: "Orchestrate parallel worker agents across isolated service worktrees, with harness-enforced verification"
argument-hint: "Audit issue or work item and the services in scope (e.g., 'Issue #515 test quality across Activity.Svc, Food.Svc, Auth.Svc')"
---

Coordinate several worker agents working **in parallel**, each in its own git worktree, each scoped to one service.

Use this when the work is wide rather than deep: an audit with findings spread across many services, a convention rollout, a dependency bump. For a change that must land in a fixed order across services, use [cross-service-change](cross-service-change.prompt.md) instead — it loops serially in one context, which is the right shape when service B depends on service A.

You are the orchestrator. **You do not write code.** Delegating and then also implementing produces two sources of truth about what was done, and yours will be the untested one.

## 0. Harness inventory (do this before planning)

State, in your first response, which harness artifacts apply to this work and which you are deliberately not using, with a reason for each omission.

| Artifact | Applies when |
|---|---|
| `AGENTS.md` | Always. Boundary rules are binding on you and on every worker. |
| `scripts/init.sh` | A worker is starting in a fresh worktree. |
| `scripts/verify.sh` | Always. This is the definition of done. |
| `.github/instructions/*.instructions.md` | Matched by the paths the workers will edit. |
| `.github/agents/*.agent.md` | A specialist matches the work — see the surface note below. |
| `.github/prompts/*.prompt.md` | An existing prompt already encodes this workflow. |
| `docs/testing.md`, `docs/architecture.md` | Per the startup workflow in `AGENTS.md`. |

Silent omission is the failure mode this step exists to prevent. An artifact you never named is one you never decided against.

**Surface capability.** The files in `.github/agents/` use VS Code chat-mode frontmatter. Agents on other surfaces — Copilot CLI, Codex, Claude Code — **cannot select them**, and can only read them as prose. If you are orchestrating from such a surface, say so rather than instructing a worker to "use the Code Reviewer agent", which it cannot do.

## 1. Shard by service

`src/Biotrackr.*` is the natural isolation unit: each directory builds and tests independently, so a shard boundary drawn there guarantees no two workers touch the same file.

Do not shard by finding type, by "angle", or by reviewer role. Those cut across files and produce merge conflicts and duplicated work.

Order shards by severity, and cap concurrency at 3. Beyond that you cannot supervise honestly.

## 2. One worktree per shard

```bash
git worktree add ../biotrackr-wt-<shard> -b <type>/<shard> origin/main
```

Branch names must match `^(feat|fix|core|docs|refactor|test)/[a-z0-9][a-z0-9-]*$`.

A fresh worktree has no restored packages, so `dotnet build --no-restore` fails until something restores. Have the worker run `bash scripts/init.sh` first, which is what it is for.

## 3. Brief discipline

**A brief may contain scope, assigned findings, and acceptance criteria. It must not contain anything the harness already owns.**

This is the load-bearing rule of this prompt. When a brief restates a build command, a naming convention, or a coverage caveat that already lives in `AGENTS.md` or an instruction file, there are now two sources of truth — and the brief wins, because it is more specific and more recent. The harness is not overridden loudly; it is quietly made redundant. Every convention you paste into a brief is one the harness stops enforcing.

Reference instead:

> Read `AGENTS.md` first, then the instruction files matching the paths you edit.
> Your scope is `src/Biotrackr.<Service>` only. Never edit outside your own worktree.
> Findings assigned to you: <list>.
> You are done when `bash scripts/verify.sh Biotrackr.<Service>` exits 0.

If a rule genuinely is missing from the harness, that is a harness defect. Fix it there, not in the brief.

Also state in every brief:

* Do not change production code to make a test pass. If a test reveals a real bug, report it and stop.
* Report back with an explicit completion marker so supervision can distinguish finished from stalled.

## 4. Verification is an exit code

`scripts/verify.sh` is the single shared sensor — the git hooks, the post-edit hooks, and the agent all call it. Route every worker through it rather than through hand-written `dotnet` invocations.

```bash
bash scripts/verify.sh Biotrackr.<Service>   # 0 pass, 1 environment, 2 code broken
```

Exit `1` and exit `2` mean different things. A worker that reports "verification failed" without distinguishing them has not diagnosed anything.

Prose describing a command is advisory and gets paraphrased. An exit code is a gate.

## 5. Findings are hypotheses, not facts

An audit finding is a claim made by a detector that has its own bugs. Require every worker to confirm the finding before acting on it, and to report false positives back rather than manufacturing a fix.

**The evidence standard is mutation testing.** Break the production behaviour the test claims to cover. If the test still passes, it does not test that behaviour, whatever it asserts. If it fails, it does — whatever the detector concluded.

This is not hypothetical. A test-quality audit of this repository reported 23 critical zero-assertion tests; 13 were false positives, because the detector did not recognise `ILogger.Moq`'s `VerifyLog`. The same audit passed two genuinely vacuous tautologies it should have caught. A worker that had "fixed" all 23 as instructed would have churned 13 working tests and left both real defects in place.

When a detector is wrong, fix the detector. Filing an issue about a tool you own, and then hand-correcting its output forever, is the expensive path.

## 6. Supervise, do not poll blindly

Wait on worker state changes rather than sleeping. On each change, report a status table.

**Never answer a blocked worker's question on the owner's behalf.** Summarise it and escalate. An approval you invented is indistinguishable, in the transcript, from one the owner gave.

## 7. Integration

`AGENTS.md` is explicit: never push to `main`, force-push a shared branch, or merge a pull request yourself. Open draft PRs, mark them ready when green, and stop. A human merges.

Before proposing any branch or worktree deletion, prove the work is reachable from `main`:

```bash
git merge-base --is-ancestor origin/<branch> origin/main
```

Deleting files or data without explicit confirmation is a boundary violation, and worktrees are where uncommitted work hides.

## 8. Close the loop

Multi-worker runs surface harness gaps faster than anything else, because the same instruction is tested N times in parallel. Where a brief had to explain something more than once, `AGENTS.md`'s review-feedback-promotion rule applies: encode it as an instruction rule, a test, or a hook.

Log the cycle in `.copilot-tracking/harness-evolution-log.md`. A run that changed how the work is done, and left no trace in the harness, will be improvised again from scratch.

## Anti-patterns

| Anti-pattern | Why it fails |
|---|---|
| Pasting conventions into the brief | Creates a competing source of truth that silently beats the harness |
| Hand-written `dotnet` commands | Bypasses the shared sensor; drifts from what the hooks enforce |
| Sharding by finding type | Cuts across files, so workers collide |
| Orchestrator also writing code | Two accounts of what changed, one of them unverified |
| Treating findings as ground truth | Churns correct code and leaves real defects untouched |
| Auto-approving a blocked worker | Fabricates owner consent |
| Deleting a worktree before proving merge | Unrecoverable, and worktrees hide uncommitted work |

## Deliverables

* A shard plan, approved before any worker starts
* One branch and one draft PR per shard, each referencing the tracking issue
* `scripts/verify.sh` exiting 0 for every touched service
* False positives reported as such, with mutation-test evidence
* Findings that could not be fixed in scope raised as issues, not silently dropped
* A harness evolution entry for anything learned twice
