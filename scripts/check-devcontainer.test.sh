#!/usr/bin/env bash
# Regression cases for check-devcontainer.sh.
#
# Each case builds a throwaway git repo from the real .devcontainer files, makes
# one kind of change, and asserts the exit code. Run from the repo root:
#   bash scripts/check-devcontainer.test.sh
#
# Exit codes: 0 all cases passed, 1 at least one failed.

set -uo pipefail

SRC="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
T="$(mktemp -d)"
trap 'rm -rf "$T"' EXIT

PASS=0
FAIL=0

setup() {
  rm -rf "$T/r"
  mkdir -p "$T/r/.devcontainer" "$T/r/scripts" "$T/r/docs"
  cd "$T/r" || exit 1
  git init -q .
  git config user.email test@example.com
  git config user.name test
  cp "$SRC/.devcontainer/devcontainer.json"      .devcontainer/
  cp "$SRC/.devcontainer/devcontainer-lock.json" .devcontainer/
  cp "$SRC/scripts/check-devcontainer.sh"        scripts/
  touch docs/devcontainer-setup.md docs/development.md
  git add -A >/dev/null 2>&1
  git commit -qm base >/dev/null 2>&1
}

assert_rc() {
  local want="$1" desc="$2" got
  bash scripts/check-devcontainer.sh >/dev/null 2>&1
  got=$?
  if [ "$got" -eq "$want" ]; then
    echo "  PASS  $desc (rc=$got)"
    PASS=$((PASS + 1))
  else
    echo "  FAIL  $desc — expected rc=$want, got rc=$got"
    FAIL=$((FAIL + 1))
  fi
}

# Guards against a case passing vacuously: a helper that edits nothing would
# leave a clean tree, and every check trivially returns 0.
assert_staged_change() {
  if git diff --cached --quiet; then
    echo "  FAIL  $1 — setup staged no change, the case proves nothing"
    FAIL=$((FAIL + 1))
    return 1
  fi
}

# Reverses the order of the features block without adding or removing a feature.
# awk rather than python3: the dev container image ships a python without the
# json module, so the tests stay on tools every environment has.
reorder_features() {
  local p=".devcontainer/devcontainer.json"
  awk '
    !inblk && /"features"[[:space:]]*:[[:space:]]*\{/ { print; inblk = 1; next }
    inblk && /^[[:space:]]*\},?[[:space:]]*$/ {
      for (i = n; i >= 1; i--) {
        line = buf[i]
        sub(/,[[:space:]]*$/, "", line)
        if (i > 1) line = line ","
        print line
      }
      print; inblk = 0; n = 0; next
    }
    inblk { buf[++n] = $0; next }
    { print }
  ' "$p" > "$p.tmp" && mv "$p.tmp" "$p"
}

# Removes the digest from one feature's lock entry, leaving the entry in place.
strip_digest() {
  local p=".devcontainer/devcontainer-lock.json"
  awk '
    /"ghcr\.io\/[^"]+"[[:space:]]*:[[:space:]]*\{/ { target = ($0 ~ /copilot-cli/) }
    target && /sha256:/ { next }
    { print }
  ' "$p" > "$p.tmp" && mv "$p.tmp" "$p"
}

echo "check-devcontainer regression cases"

setup
assert_rc 0 "clean tree, nothing staged"

setup
git rm -q .devcontainer/devcontainer-lock.json
assert_rc 1 "lock file deleted (every feature unpinned)"

setup
sed -i 's#"ghcr.io/devcontainers/features/github-cli:1": {},#"ghcr.io/devcontainers/features/github-cli:1": {},\n        "ghcr.io/devcontainers/features/node:1": {},#' .devcontainer/devcontainer.json
git add -A
assert_rc 1 "feature added with no lock entry"

setup
reorder_features
git add -A
assert_staged_change "features reordered, none added" && assert_rc 0 "features reordered, none added"

setup
printf '\n<!-- touched -->\n' >> docs/development.md
git add -A
assert_rc 0 "docs touched, features untouched"

setup
strip_digest
git add -A
assert_staged_change "lock entry present but digest stripped" && assert_rc 1 "lock entry present but digest stripped"

echo ""
echo "passed=$PASS failed=$FAIL"
[ "$FAIL" -eq 0 ]
