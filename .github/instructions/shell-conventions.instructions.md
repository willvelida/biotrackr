---
description: "Shell script conventions for Biotrackr. Use when: writing or editing scripts under scripts/ or the git hooks in .githooks/."
applyTo: "scripts/**/*.sh,.githooks/*"
---

# Shell Script Conventions

Scripts here are enforcement, not convenience. `check-devcontainer.sh`,
`audit-harness.sh`, and `verify.sh` decide whether a commit is allowed, so a bug
in one does not fail loudly — it silently stops enforcing.

## Enforcement scripts ship with a test

A script that gates a commit needs a sibling `{name}.test.sh` covering the case it
exists to catch and the case it must not flag. Both directions matter: a check
that never passes gets bypassed with `--no-verify` and is then worth nothing.

Every case asserts that its setup actually staged a change. A helper that silently
edits nothing leaves a clean tree, and every check trivially returns 0 — the case
passes while proving nothing.

## Reading a git diff

- `--diff-filter=ACMR` excludes deletions. For a check that guards a file's
  presence, deletion is the worst case, so the filter hides exactly the change that
  matters most. Filter to ACMR only when "was this updated" is the question.
- Gate on every file involved, not just the obvious one. A check keyed on
  `devcontainer.json` never runs on a commit that only touches the lock file.
- Identify additions as ids-on-added-lines **minus** ids-on-removed-lines. A moved
  or reformatted line appears as both `-` and `+`, so inspecting `+` alone reports
  a reorder as a batch of new entries.

## Portability

- Write LF line endings. `.gitattributes` normalises the committed blob, but the
  working tree keeps CRLF, and the dev container bind-mounts the working tree —
  bash then fails with `cd: $'/workspaces/biotrackr\r'`. Editors and agent tooling
  on Windows write CRLF by default, so check a new script before committing it.
- The container's `python3` ships without the `json` module. Use `awk`, `sed`, and
  `grep` for anything a script must do inside the container.
- Do not depend on `jq`. These scripts run from git hooks on a bare Windows host
  where it is not installed.
- `set -uo pipefail`, not `-e`: a check script needs to collect every failure and
  report them together, not abort on the first one.

## Output

A failure message names what broke, why it matters, and the next action. An
assertion an agent cannot act on is a defect.

## Observability

Verification events are recorded through `scripts/observability.sh`, a sourced
library. Load it, never execute it, and always behind shims:

```bash
BIOTRACKR_OBS_ROOT="$REPO_ROOT"
. "$REPO_ROOT/scripts/observability.sh" 2>/dev/null || true
type -t emit_event >/dev/null 2>&1 || emit_event() { :; }
type -t obs_now_ms >/dev/null 2>&1 || obs_now_ms() { _OBS_NOW_MS=0; }

emit_event <component> <action> <subject> <outcome> <ms> [detail] || true
```

- Off unless `BIOTRACKR_OBS_ENABLED=1`, following the `BIOTRACKR_*` hook-control
  convention. The shims mean deleting the library changes nothing.
- `outcome` is one of `pass fail error skip degraded timeout`. Record what
  actually happened: a bypassed gate and a passing gate must not both read
  `pass`, and a script with three-valued exit codes must not collapse them.
- Guard every call with `|| true`, and never emit to stdout. Both agent
  surfaces discard successful hook stdout, and hooks reserve stderr for
  failures.
- A sourced library must not run `set -` or `exit`: options leak into the
  calling hook, and an exit terminates it rather than the function.
- The emit path is measured against a 50ms budget on the Windows host, where
  file I/O rather than forking dominates. Re-run the benchmark before adding
  another read to it.

## Recording and verifying

- Sanitise every field written to a delimited record, including ones the script
  generates. "This value cannot contain the delimiter" is an assumption: a git
  branch may contain `|`, and one unsanitised field silently shifts every column
  after it.
- A hook that writes a tracked file has not committed it. `pre-push` runs after
  the commits exist, so anything it writes lands outside the push. Claiming
  automatic persistence needs a trace showing how the write reaches a commit.
- `git check-ignore` proves ignore status, not tracking; with a negation it exits
  0 on a file that is *not* ignored. Use `git ls-files --error-unmatch`.
- Timestamps are not uniqueness mechanisms. Filenames and watermarks keyed on
  time need a tie-breaker, or two events in the same tick collide.
- Prove an enforcement test can fail. Break the thing it guards, confirm it goes
  red, restore. A suite that has only ever passed is indistinguishable from one
  whose assertions never run.
