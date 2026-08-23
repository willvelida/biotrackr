#!/usr/bin/env bash
# Biotrackr harness observability — structured event emission.
#
# SOURCED LIBRARY, not an executable. Callers do:
#   . "$REPO_ROOT/scripts/observability.sh" 2>/dev/null || true
#   emit_event verify build Biotrackr.Activity.Api pass 1840
#
# Deliberately does NOT run `set -uo pipefail`. Shell options set in a sourced
# file leak into the caller, and every caller here is a git hook whose exit-code
# contract is load-bearing. For the same reason nothing here ever calls `exit` —
# that would terminate the hook, not the function. Every path returns 0.
#
# Records are 9 pipe-delimited fields, one line each, append-only:
#   ts|session|component|action|subject|outcome|ms|detail|ref
#
# Off unless BIOTRACKR_OBS_ENABLED=1, and the disabled path returns before
# touching the filesystem or forking.

# Sourcing twice is harmless but wasteful; hooks may source transitively.
if [ -n "${_BIOTRACKR_OBS_LOADED:-}" ]; then
  return 0 2>/dev/null || true
fi
_BIOTRACKR_OBS_LOADED=1

_OBS_SCHEMA=1

# Bounded so a long error string cannot dominate a record, and so nothing
# resembling file content can be captured (spec NG8).
_OBS_DETAIL_MAX="${BIOTRACKR_OBS_DETAIL_MAX:-80}"

# Rotation threshold in bytes for the raw store. Rotation never deletes: the
# live log is renamed and a fresh one started, so records that have not yet been
# promoted survive in the archive.
_OBS_MAX_BYTES="${BIOTRACKR_OBS_MAX_BYTES:-1048576}"

# Session boundary. The harness cannot observe a true session edge — no agent
# surface exposes one — so an idle gap is used as a proxy. 30 minutes per the
# Phase 3 clarification.
_OBS_IDLE_SECS="${BIOTRACKR_OBS_IDLE_SECS:-1800}"

# Resolve the repo root once, at source time. This is the only fork in the
# library outside rotation, and it happens once per process rather than per
# event. Callers that already know the root can skip it via BIOTRACKR_OBS_ROOT.
if [ -n "${BIOTRACKR_OBS_ROOT:-}" ]; then
  _OBS_ROOT="$BIOTRACKR_OBS_ROOT"
else
  _OBS_ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." 2>/dev/null && pwd)" || _OBS_ROOT=""
fi

_OBS_DIR="$_OBS_ROOT/.copilot-tracking/observability"
_OBS_LOG="$_OBS_DIR/events.log"
_OBS_SENTINEL="$_OBS_DIR/session"
_OBS_WATERMARK="$_OBS_DIR/promoted"
_OBS_METRICS="$_OBS_ROOT/.copilot-tracking/harness-evolution-metrics.md"
_OBS_GITDIR="$_OBS_ROOT/.git"

# ---------------------------------------------------------------------------
# Session identity
#
# The sentinel holds "<session-id> <last-epoch> <bytes-written>" on one line.
# Reading and writing it uses only builtins, so no fork is needed to decide
# whether the session has rolled over.
# ---------------------------------------------------------------------------
_obs_session() {
  local sid last bytes now
  now="${EPOCHSECONDS:-0}"

  if [ -r "$_OBS_SENTINEL" ]; then
    read -r sid last bytes < "$_OBS_SENTINEL" 2>/dev/null || true
  fi
  sid="${sid:-}"
  last="${last:-0}"
  bytes="${bytes:-0}"

  # Mint a new session when there is no sentinel, or the idle gap has elapsed.
  # The byte counter deliberately survives a session change: it tracks the log
  # file, which persists across sessions, and is reset only by rotation.
  if [ -z "$sid" ] || [ $((now - last)) -ge "$_OBS_IDLE_SECS" ]; then
    sid="s-${now}-${RANDOM}"
  fi

  # A lost sentinel would restart the counter at zero while the log is already
  # large, delaying rotation. Recover the real size instead. One fork, in a path
  # that runs at most once per clone.
  if [ "$bytes" = "0" ] && [ -s "$_OBS_LOG" ]; then
    bytes="$(wc -c < "$_OBS_LOG" 2>/dev/null | tr -d ' ')" || bytes=0
    bytes="${bytes:-0}"
  fi

  _OBS_SESSION="$sid"
  _OBS_BYTES="$bytes"
  return 0
}

_obs_session_touch() {
  printf '%s %s %s\n' "$_OBS_SESSION" "${EPOCHSECONDS:-0}" "$_OBS_BYTES" \
    > "$_OBS_SENTINEL" 2>/dev/null || true
  return 0
}

# ---------------------------------------------------------------------------
# Commit reference
#
# Read from .git directly so the common path costs no fork. A packed ref (fresh
# clone with no local commit) has no loose ref file, and only then does this
# fall back to one `git` call.
#
# Sets _OBS_REF rather than printing it. A `$(...)` substitution would fork a
# subshell even though every operation inside is a builtin, which is precisely
# the cost this design exists to avoid.
# ---------------------------------------------------------------------------
_obs_ref() {
  local head branch sha

  # HEAD cannot move within a single short-lived process, so resolve once and
  # reuse. Matters for callers that emit several times, such as the
  # affected-service sensor emitting per service.
  if [ -n "${_OBS_REF:-}" ]; then
    return 0
  fi

  branch=""
  sha=""

  if [ -r "$_OBS_GITDIR/HEAD" ]; then
    read -r head < "$_OBS_GITDIR/HEAD" 2>/dev/null || head=""
    case "$head" in
      "ref: "*)
        branch="${head#ref: }"
        if [ -r "$_OBS_GITDIR/$branch" ]; then
          read -r sha < "$_OBS_GITDIR/$branch" 2>/dev/null || sha=""
        fi
        branch="${branch#refs/heads/}"
        ;;
      "")
        ;;
      *)
        # Detached HEAD holds a bare sha.
        branch="detached"
        sha="$head"
        ;;
    esac
  fi

  # Only a packed ref reaches here, and only then is a fork spent.
  if [ -z "$sha" ] && [ -n "$branch" ]; then
    sha="$(git -C "$_OBS_ROOT" rev-parse --short HEAD 2>/dev/null)" || sha=""
  fi

  _OBS_REF="${branch:-unknown}@${sha:0:7}"
  return 0
}

# ---------------------------------------------------------------------------
# Timestamp — sub-second, so records within one session are orderable without a
# sequence counter. EPOCHREALTIME is a bash 5 builtin; `date` is the fallback.
#
# Sets _OBS_TS rather than printing it, for the same no-subshell reason.
# ---------------------------------------------------------------------------
_obs_now() {
  local raw secs frac
  raw="${EPOCHREALTIME:-}"
  if [ -n "$raw" ]; then
    secs="${raw%%[.,]*}"
    frac="${raw##*[.,]}"
    frac="${frac:0:3}"
    printf -v _OBS_TS '%(%Y-%m-%dT%H:%M:%S)T.%sZ' "$secs" "$frac"
  else
    _OBS_TS="$(date -u +%Y-%m-%dT%H:%M:%S.000Z 2>/dev/null)" || _OBS_TS="unknown"
  fi
  return 0
}

# ---------------------------------------------------------------------------
# Field sanitisation
#
# Fixed arity is what makes a truncated record detectable. A delimiter inside a
# field value defeats that: the line still looks well-formed but parses into the
# wrong columns, which is worse than an obvious break. Newlines would split one
# record into two.
#
# Strip rather than escape. Escaping would require every reader to implement the
# matching unescape, and the available toolchain (no jq, no python json) makes
# that a liability. These fields are identifiers and short tokens, so removing
# the few reserved characters loses nothing worth keeping.
#
# Pure parameter expansion: no fork.
# ---------------------------------------------------------------------------
_obs_clean() {
  local v="${1:-}"
  v="${v//|/}"
  v="${v//$'\r'/}"
  v="${v//$'\n'/ }"
  _OBS_CLEAN="$v"
  return 0
}

# ---------------------------------------------------------------------------
# Rotation
#
# Renames the live log and starts a fresh one once it passes the threshold.
# Archives are never deleted, because a record may not have been promoted to the
# committed rollup yet and promotion is the only thing entitled to decide a
# record is finished with. Pruning archives is therefore a separate concern from
# bounding the live file.
#
# Costs one fork, but only on the rotation itself, which is rare.
# ---------------------------------------------------------------------------
_obs_rotate() {
  [ "${_OBS_BYTES:-0}" -ge "$_OBS_MAX_BYTES" ] || return 0
  [ -f "$_OBS_LOG" ] || return 0

  local stamp
  stamp="${_OBS_TS//:/-}"
  stamp="${stamp//./-}"

  if mv -f "$_OBS_LOG" "$_OBS_DIR/events-${stamp}.log" 2>/dev/null; then
    _OBS_BYTES=0
  fi
  return 0
}

# ---------------------------------------------------------------------------
# obs_now_ms — milliseconds since the epoch, into _OBS_NOW_MS.
#
# Public, because every producer needs to time its own work and duplicating
# this in five files would be five chances to get it wrong. Builtin-only on
# bash 5; falls back to `date` otherwise. Runs whether or not emission is
# enabled, so callers can time unconditionally.
# ---------------------------------------------------------------------------
obs_now_ms() {
  local raw secs frac
  raw="${EPOCHREALTIME:-}"
  if [ -n "$raw" ]; then
    secs="${raw%%[.,]*}"
    frac="${raw##*[.,]}"
    frac="${frac}000"
    _OBS_NOW_MS="${secs}${frac:0:3}"
  else
    _OBS_NOW_MS="$(date +%s 2>/dev/null)000" || _OBS_NOW_MS=0
  fi
  return 0
}

# ---------------------------------------------------------------------------
# emit_event <component> <action> <subject> <outcome> <ms> [detail]
#
# outcome is one of: pass fail error skip degraded timeout
# ---------------------------------------------------------------------------
emit_event() {
  [ "${BIOTRACKR_OBS_ENABLED:-0}" = "1" ] || return 0
  [ -n "$_OBS_ROOT" ] || return 0

  local component action subject outcome ms detail line
  component="${1:-unknown}"
  action="${2:-unknown}"
  subject="${3:-}"
  outcome="${4:-unknown}"
  ms="${5:-0}"
  detail="${6:-}"

  # mkdir is a fork, so it runs only when the directory is genuinely absent —
  # which is once per clone, not once per event.
  if [ ! -d "$_OBS_DIR" ]; then
    mkdir -p "$_OBS_DIR" 2>/dev/null || return 0
  fi

  # Schema marker is written once, so a future positional change is detectable
  # rather than silently misparsed.
  if [ ! -f "$_OBS_LOG" ]; then
    printf '#schema %s ts|session|component|action|subject|outcome|ms|detail|ref\n' \
      "$_OBS_SCHEMA" > "$_OBS_LOG" 2>/dev/null || return 0
  fi

  _obs_session
  _obs_now
  _obs_ref

  # Every variable field is sanitised. The generated fields (_OBS_TS, session
  # id, _OBS_REF) are constructed from constrained inputs and cannot contain the
  # delimiter, so they are not re-scanned.
  _obs_clean "$component"; component="$_OBS_CLEAN"
  _obs_clean "$action";    action="$_OBS_CLEAN"
  _obs_clean "$subject";   subject="$_OBS_CLEAN"
  _obs_clean "$outcome";   outcome="$_OBS_CLEAN"
  _obs_clean "$ms";        ms="$_OBS_CLEAN"
  _obs_clean "$detail";    detail="${_OBS_CLEAN:0:$_OBS_DETAIL_MAX}"

  line="${_OBS_TS}|${_OBS_SESSION}|${component}|${action}|${subject}|${outcome}|${ms}|${detail}|${_OBS_REF}"

  # Single whole-line append. Atomic for a short line on the container's
  # filesystem; on the Windows host it is a low-probability race rather than a
  # guarantee, which is why the self-test verifies field arity on read-back.
  printf '%s\n' "$line" >> "$_OBS_LOG" 2>/dev/null || return 0

  _OBS_BYTES=$((_OBS_BYTES + ${#line} + 1))
  _obs_rotate
  _obs_session_touch
  return 0
}

# ---------------------------------------------------------------------------
# promote_observability — distil raw records into one committed row.
#
# The raw store is local and disposable; the companion metrics file is
# committed and small. Promotion is the only thing that moves data between
# them, and it is the step this design was most worried about, because a
# discretionary measurement step is exactly what left nine of eleven evolution
# log rows unmeasured. It therefore runs from the push gate rather than being
# something a person remembers to do.
#
# Reads every record newer than the watermark across the live log AND its
# archives, since rotation means a cycle's records can span several files.
# Never deletes anything: the watermark advances, the records stay.
#
# Safe to call unconditionally. Returns 0 always, including when there is
# nothing to promote, when the store is absent, and when emission was never
# enabled.
# ---------------------------------------------------------------------------
promote_observability() {
  [ -n "${_OBS_ROOT:-}" ] || return 0
  [ -d "$_OBS_DIR" ] || return 0
  [ -f "$_OBS_METRICS" ] || return 0

  local watermark="" summary total sessions p f e s d t ms maxts cycle today
  if [ -r "$_OBS_WATERMARK" ]; then
    read -r watermark < "$_OBS_WATERMARK" 2>/dev/null || watermark=""
  fi

  summary="$(cat "$_OBS_DIR"/events*.log 2>/dev/null | awk -F'|' -v wm="${watermark:-}" '
    !/^#schema/ && NF == 9 && ($1 "") > (wm "") {
      total++
      o[$6]++
      sess[$2] = 1
      if ($7 ~ /^[0-9]+$/) ms += $7
      if (($1 "") > (maxts "")) maxts = $1
    }
    END {
      n = 0
      for (k in sess) n++
      printf "%d %d %d %d %d %d %d %d %d %s",
        total + 0, n, o["pass"] + 0, o["fail"] + 0, o["error"] + 0,
        o["skip"] + 0, o["degraded"] + 0, o["timeout"] + 0, ms + 0, maxts
    }' 2>/dev/null)" || return 0

  # Parsed with `read` rather than `set --`: the self-test asserts this library
  # contains no `set -` at all, and keeping that assertion blunt is worth more
  # than the convenience. `set --` would only touch this function's positional
  # parameters, but a safety check that needs a caveat is a weaker check.
  read -r total sessions p f e s d t ms maxts <<< "$summary"
  total="${total:-0}"; sessions="${sessions:-0}"; p="${p:-0}"; f="${f:-0}"; e="${e:-0}"
  s="${s:-0}"; d="${d:-0}"; t="${t:-0}"; ms="${ms:-0}"; maxts="${maxts:-}"

  # Nothing new since the last promotion. Not an error, and not worth a row.
  [ "$total" -gt 0 ] || return 0

  _obs_ref
  cycle="${_OBS_REF%@*}"
  printf -v today '%(%Y-%m-%d)T' -1 2>/dev/null || today="unknown"

  printf '| %s | %s | %s | %s | %s | %s | %s | %s | %s | %s | %s |\n' \
    "$today" "$cycle" "$sessions" "$total" "$p" "$f" "$e" "$s" "$d" "$t" "$ms" \
    >> "$_OBS_METRICS" 2>/dev/null || return 0

  printf '%s\n' "$maxts" > "$_OBS_WATERMARK" 2>/dev/null || true
  return 0
}