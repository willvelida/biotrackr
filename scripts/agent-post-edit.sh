#!/usr/bin/env bash
# Biotrackr agent PostToolUse hook — shared by Claude Code and VS Code Copilot.
#
# Git hooks fire on git events, not file saves. This is the layer that actually
# answers "verify after the agent edits a file". It reads the hook JSON payload
# on stdin, works out which service owns the edited file, and builds it.
#
# Build-only by default (~1-2s incremental) to keep per-edit latency low; the
# full unit suite runs at pre-commit. Set BIOTRACKR_HOOK_TESTS=1 to run tests too.
#
# Exit codes: 0 = nothing to say (both harnesses discard successful hook stdout)
#             2 = failure, stderr is surfaced back to the model
#
# Three measured incompatibilities are absorbed here:
#   * VS Code ignores hook matchers and fires on EVERY tool call, so this script
#     filters by tool name itself rather than relying on config.
#   * Claude Code sends tool_input.file_path; VS Code sends tool_input.filePath.
#   * Tool names differ: Claude Code Edit/Write, VS Code create_file /
#     replace_string_in_file.

set -uo pipefail

INPUT="$(cat)"
[ -n "$INPUT" ] || exit 0

TOOL=""
FILE=""

# jq is absent on the Windows host but python 3 is present, so python is the
# primary parser. The grep/sed path exists so the hook degrades instead of
# erroring if python disappears.
if command -v python >/dev/null 2>&1; then
  PARSED="$(printf '%s' "$INPUT" | python -c '
import json, sys
try:
    d = json.load(sys.stdin)
except Exception:
    sys.exit(1)
if not isinstance(d, dict):
    sys.exit(1)
ti = d.get("tool_input") or {}
if not isinstance(ti, dict):
    ti = {}
print(d.get("tool_name") or "")
print(ti.get("file_path") or ti.get("filePath") or ti.get("path") or "")
' 2>/dev/null)" || PARSED=""

  if [ -n "$PARSED" ]; then
    TOOL="$(printf '%s\n' "$PARSED" | sed -n '1p')"
    FILE="$(printf '%s\n' "$PARSED" | sed -n '2p')"
  fi
fi

if [ -z "$TOOL" ] && [ -z "$FILE" ]; then
  TOOL="$(printf '%s' "$INPUT" \
    | grep -oE '"tool_name"[[:space:]]*:[[:space:]]*"[^"]*"' | head -1 \
    | sed 's/.*:[[:space:]]*"//; s/"$//')"
  FILE="$(printf '%s' "$INPUT" \
    | grep -oE '"(file_path|filePath|path)"[[:space:]]*:[[:space:]]*"[^"]*"' | head -1 \
    | sed 's/.*:[[:space:]]*"//; s/"$//')"
fi

# Claude Code: Write, Edit, NotebookEdit. VS Code: create_file,
# replace_string_in_file, multi_replace_string_in_file, edit_notebook_file.
case "$TOOL" in
  Write|Edit|MultiEdit|NotebookEdit) ;;
  create_file|replace_string_in_file|multi_replace_string_in_file|edit_notebook_file) ;;
  *) exit 0 ;;
esac

[ -n "$FILE" ] || exit 0

# Windows paths arrive with backslashes, JSON-escaped as \\. Normalise before
# matching — a forward-slash comparison never matches a backslash path.
FILE="${FILE//\\\\//}"
FILE="${FILE//\\//}"

# Resolve the root and load observability before the path filters, so that an
# edit this hook deliberately does not build can still be recorded. The shims
# make every call a no-op if the library is absent, leaving behaviour unchanged.
REPO_ROOT="${CLAUDE_PROJECT_DIR:-}"
if [ -z "$REPO_ROOT" ]; then
  REPO_ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." 2>/dev/null && pwd)" || exit 0
fi
cd "$REPO_ROOT" 2>/dev/null || exit 0

BIOTRACKR_OBS_ROOT="$REPO_ROOT"
# shellcheck source=/dev/null
. "$REPO_ROOT/scripts/observability.sh" 2>/dev/null || true
type -t emit_event >/dev/null 2>&1 || emit_event() { :; }
type -t obs_now_ms >/dev/null 2>&1 || obs_now_ms() { _OBS_NOW_MS=0; }

# Record paths relative to the root: absolute paths are noise and leak the
# machine's directory layout into the store.
REL="${FILE#$REPO_ROOT/}"

# Harness files have no build to run, so this hook used to exit silently on
# them — leaving the harness the one thing its own verification layer could not
# see. Record the edit, then exit as before. The structural audit remains the
# gate for whether the change is sound.
case "$REL" in
  .githooks/*|scripts/*.sh|.github/hooks/*|.claude/settings.json|.github/instructions/*|.github/skills/*|.github/agents/*)
    emit_event post-edit harness-edit "$REL" pass 0 || true
    exit 0
    ;;
esac

# Only C# source under src/ can break a build. Markdown, Bicep, YAML and
# anything under .copilot-tracking/ exit here without spawning dotnet.
case "$FILE" in
  */src/Biotrackr.*|src/Biotrackr.*) ;;
  *) exit 0 ;;
esac
case "$FILE" in
  *.cs|*.csproj|*.razor|*.sln|*.slnx|*.props|*.targets) ;;
  *) exit 0 ;;
esac

SVC="$(printf '%s' "$FILE" | sed -E 's#^.*src/(Biotrackr\.[^/]+)/.*$#\1#')"
[ -n "$SVC" ] && [ "$SVC" != "$FILE" ] || exit 0

[ -d "src/$SVC" ] || exit 0

ARGS=(--build-only)
[ "${BIOTRACKR_HOOK_TESTS:-0}" = "1" ] && ARGS=()

obs_now_ms; _edit_start="$_OBS_NOW_MS"

if OUT="$(bash scripts/verify.sh "${ARGS[@]}" "$SVC" 2>&1)"; then
  obs_now_ms
  emit_event post-edit build "$SVC" pass $((_OBS_NOW_MS - _edit_start)) || true
  exit 0
fi

obs_now_ms
emit_event post-edit build "$SVC" fail $((_OBS_NOW_MS - _edit_start)) "verify-failed" || true

# Exit 2 is the only code both harnesses route back to the model.
printf '%s\n' "$OUT" | head -40 >&2
exit 2
