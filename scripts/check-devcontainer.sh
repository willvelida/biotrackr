#!/usr/bin/env bash
# Biotrackr dev container consistency check.
#
# A dev container feature added without a pinned digest is an unreviewed
# supply-chain dependency, and a feature added without a doc update leaves the
# tooling lists lying to the next contributor. Both are mechanical, so both are
# checked here rather than left to reviewer attention.
#
# Usage:
#   bash scripts/check-devcontainer.sh                     # staged changes (git hook)
#   bash scripts/check-devcontainer.sh --range origin/main..HEAD   # a diff range (CI)
#   bash scripts/check-devcontainer.sh --all               # pinning only, ignore the diff
#
# Exit codes:
#   0  passed, or nothing relevant changed
#   1  a check failed

set -uo pipefail

REPO_ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
cd "$REPO_ROOT" || exit 1

CONFIG=".devcontainer/devcontainer.json"
LOCK=".devcontainer/devcontainer-lock.json"
DOCS=("docs/devcontainer-setup.md" "docs/development.md")

MODE="staged"
RANGE=""

while [ $# -gt 0 ]; do
  case "$1" in
    --range)
      MODE="range"
      RANGE="${2:-}"
      if [ -z "$RANGE" ]; then
        echo "CHECK CANNOT RUN: --range requires a revision range, e.g. --range origin/main..HEAD" >&2
        exit 1
      fi
      shift
      ;;
    --all)     MODE="all" ;;
    -h|--help) sed -n '2,17p' "${BASH_SOURCE[0]}" | sed 's/^# \{0,1\}//'; exit 0 ;;
    *)
      echo "CHECK CANNOT RUN: unknown argument '$1'. Run with --help." >&2
      exit 1
      ;;
  esac
  shift
done

if [ ! -f "$CONFIG" ]; then
  echo "check-devcontainer: $CONFIG not found, nothing to check."
  exit 0
fi

# Files changed in the selected scope. Empty in --all mode, which forces the
# pinning check to run and skips the documentation check.
changed_files() {
  case "$MODE" in
    staged) git diff --cached --name-only --diff-filter=ACMR ;;
    range)  git diff "$RANGE" --name-only --diff-filter=ACMR ;;
    all)    : ;;
  esac
}

# Added lines only, so a reordered or reformatted features block is not mistaken
# for a new dependency.
added_feature_lines() {
  case "$MODE" in
    staged) git diff --cached -U0 -- "$CONFIG" ;;
    range)  git diff "$RANGE" -U0 -- "$CONFIG" ;;
    all)    : ;;
  esac | grep -E '^\+[^+]' | grep -oE '"ghcr\.io/[^"]+"' | tr -d '"' | sort -u
}

CHANGED="$(changed_files)"

if [ "$MODE" != "all" ] && ! printf '%s\n' "$CHANGED" | grep -qx "$CONFIG"; then
  exit 0
fi

# Feature ids declared in devcontainer.json. Feature ids are the only ghcr.io
# references in the file, so a direct match needs no JSON parser and therefore
# no jq dependency on the host.
DECLARED="$(grep -oE '"ghcr\.io/[^"]+"' "$CONFIG" | tr -d '"' | sort -u)"

# Feature ids in the lock file whose block carries a sha256 digest.
PINNED=""
if [ -f "$LOCK" ]; then
  PINNED="$(awk '
    match($0, /"ghcr\.io\/[^"]+"[[:space:]]*:[[:space:]]*\{/) {
      match($0, /"ghcr\.io\/[^"]+"/)
      current = substr($0, RSTART + 1, RLENGTH - 2)
      seen[current] = 0
      next
    }
    /sha256:/ { if (current != "") seen[current] = 1 }
    END { for (id in seen) if (seen[id]) print id }
  ' "$LOCK" | sort -u)"
fi

FAILED=0

# 1. Every declared feature is pinned to a digest.
UNPINNED="$(comm -23 <(printf '%s\n' "$DECLARED" | grep -v '^$') \
                     <(printf '%s\n' "$PINNED" | grep -v '^$'))"

if [ -n "$UNPINNED" ]; then
  {
    echo ""
    echo "DEVCONTAINER CHECK FAILED: feature declared without a pinned digest."
    printf '  %s\n' $UNPINNED
    echo "  what: the feature resolves through a floating tag, so the container can"
    echo "        change without a reviewable diff."
    echo "  next: add an entry to $LOCK with a 'resolved' digest read from the"
    echo "        registry. See .github/instructions/devcontainer-conventions.instructions.md"
    echo "        for the two curl commands that return it."
  } >&2
  FAILED=1
fi

# 2. A newly added feature updates the tooling lists.
if [ "$MODE" != "all" ]; then
  ADDED="$(added_feature_lines)"
  if [ -n "$ADDED" ]; then
    DOCS_TOUCHED=0
    for d in "${DOCS[@]}"; do
      if printf '%s\n' "$CHANGED" | grep -qx "$d"; then
        DOCS_TOUCHED=1
      fi
    done
    if [ "$DOCS_TOUCHED" -eq 0 ]; then
      {
        echo ""
        echo "DEVCONTAINER CHECK FAILED: feature added without a documentation update."
        printf '  %s\n' $ADDED
        echo "  what: the tooling lists in the docs below now understate what the"
        echo "        container ships, and they are what a new contributor reads."
        echo "  next: update the tooling lists in these files in the same commit:"
        printf '        %s\n' "${DOCS[@]}"
        echo "  note: if the feature ships nothing user-facing, say so in the commit"
        echo "        body and bypass with: git commit --no-verify"
      } >&2
      FAILED=1
    fi
  fi
fi

if [ "$FAILED" -eq 0 ]; then
  COUNT="$(printf '%s\n' "$DECLARED" | grep -c . || true)"
  echo "check-devcontainer: ${COUNT} feature(s) pinned, docs consistent."
fi

exit "$FAILED"
