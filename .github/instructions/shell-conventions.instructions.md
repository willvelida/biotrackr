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
