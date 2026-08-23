#!/usr/bin/env bash
# Regression cases for observability.sh.
#
# The library writes to a file rather than stdout, and its callers discard its
# output, so a case that only checks the return code would pass while records
# were silently malformed. Every case here asserts on what was actually written.
#
# Run from the repo root:
#   bash scripts/observability.test.sh
#
# Exit codes: 0 all cases passed, 1 at least one failed.

set -uo pipefail

SRC="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
T="$(mktemp -d)"
trap 'rm -rf "$T"' EXIT

PASS=0
FAIL=0

pass() { echo "  PASS  $1"; PASS=$((PASS + 1)); }
fail() { echo "  FAIL  $1 — $2"; FAIL=$((FAIL + 1)); }

# Fresh store and a fresh copy of the library state. The load guard and the
# cached ref both survive a re-source, so they are cleared explicitly.
reload() {
  rm -rf "$T/root"
  mkdir -p "$T/root"
  unset _BIOTRACKR_OBS_LOADED _OBS_REF _OBS_REF_RESOLVED _OBS_TS _OBS_SESSION _OBS_BYTES _OBS_CLEAN
  export BIOTRACKR_OBS_ROOT="$T/root"
  LOG="$T/root/.copilot-tracking/observability/events.log"
  SENTINEL="$T/root/.copilot-tracking/observability/session"
  # shellcheck disable=SC1090
  . "$SRC/scripts/observability.sh"
}

# Non-schema record count.
records() {
  [ -f "$LOG" ] || { echo 0; return 0; }
  grep -vc '^#schema' "$LOG" 2>/dev/null || echo 0
}

field() {  # field <n> -> value from the last record
  tail -1 "$LOG" | awk -F'|' -v n="$1" '{print $n}'
}

arity_violations() {
  cat "$T/root/.copilot-tracking/observability"/events*.log 2>/dev/null \
    | awk -F'|' '!/^#schema/ && NF != 9' | wc -l | tr -d ' '
}

echo "observability regression cases"

# --- disabled path -------------------------------------------------------
reload
unset BIOTRACKR_OBS_ENABLED
emit_event verify build svc pass 100
rc=$?
if [ "$rc" -ne 0 ]; then
  fail "disabled: returns 0" "returned $rc; a non-zero return would break a caller under set -e"
else
  pass "disabled: returns 0"
fi
if [ -e "$LOG" ]; then
  fail "disabled: writes nothing" "log was created"
else
  pass "disabled: writes nothing"
fi

# --- enabled path --------------------------------------------------------
reload
export BIOTRACKR_OBS_ENABLED=1
emit_event verify build Biotrackr.Activity.Api pass 1840
n="$(records)"
if [ "$n" -eq 1 ]; then pass "enabled: one record written"; else fail "enabled: one record written" "found $n"; fi

# Guards against a vacuous pass: if nothing was written, later field assertions
# would compare empty strings and quietly succeed.
if [ "$(records)" -eq 0 ]; then
  fail "enabled: setup wrote a record" "no record, the field cases below prove nothing"
fi

got="$(head -1 "$LOG")"
case "$got" in
  '#schema '*) pass "schema marker written once at creation" ;;
  *) fail "schema marker written once at creation" "first line was: $got" ;;
esac

nf="$(tail -1 "$LOG" | awk -F'|' '{print NF}')"
if [ "$nf" -eq 9 ]; then pass "record has exactly 9 fields"; else fail "record has exactly 9 fields" "found $nf"; fi

if [ "$(field 3)" = "verify" ]; then pass "component in field 3"; else fail "component in field 3" "got '$(field 3)'"; fi
if [ "$(field 6)" = "pass" ];   then pass "outcome in field 6";   else fail "outcome in field 6" "got '$(field 6)'"; fi
if [ "$(field 7)" = "1840" ];   then pass "duration in field 7";  else fail "duration in field 7" "got '$(field 7)'"; fi

# --- delimiter injection -------------------------------------------------
# The case the fixed-arity format exists to survive: a delimiter inside a value
# would otherwise produce a line that parses into the wrong columns.
reload
export BIOTRACKR_OBS_ENABLED=1
emit_event verify build "svc" fail 10 "verify failed | exit 2"
nf="$(tail -1 "$LOG" | awk -F'|' '{print NF}')"
if [ "$nf" -eq 9 ]; then pass "delimiter in detail: still 9 fields"; else fail "delimiter in detail: still 9 fields" "found $nf"; fi
case "$(field 8)" in
  *"|"*) fail "delimiter stripped from detail" "field still contains the delimiter" ;;
  "") fail "delimiter stripped from detail" "detail empty; value was dropped rather than cleaned" ;;
  *) pass "delimiter stripped from detail" ;;
esac

reload
export BIOTRACKR_OBS_ENABLED=1
emit_event verify build "a|b|c" fail 10 "x"
nf="$(tail -1 "$LOG" | awk -F'|' '{print NF}')"
if [ "$nf" -eq 9 ]; then pass "delimiter in subject: still 9 fields"; else fail "delimiter in subject: still 9 fields" "found $nf"; fi

# --- newline injection ---------------------------------------------------
reload
export BIOTRACKR_OBS_ENABLED=1
emit_event verify build svc fail 10 "line1
line2"
n="$(records)"
if [ "$n" -eq 1 ]; then pass "newline in detail: one record, not two"; else fail "newline in detail: one record, not two" "found $n"; fi

# --- detail bound --------------------------------------------------------
reload
export BIOTRACKR_OBS_ENABLED=1
long="$(printf 'x%.0s' $(seq 1 300))"
emit_event verify build svc fail 10 "$long"
dlen="$(tail -1 "$LOG" | awk -F'|' '{print length($8)}')"
if [ "$dlen" -le 80 ] && [ "$dlen" -gt 0 ]; then
  pass "detail truncated to bound (len=$dlen)"
else
  fail "detail truncated to bound" "length was $dlen"
fi

# --- outcome vocabulary --------------------------------------------------
reload
export BIOTRACKR_OBS_ENABLED=1
for o in pass fail error skip degraded timeout; do
  emit_event gate check subj "$o" 1
done
missing=""
for o in pass fail error skip degraded timeout; do
  grep -q "|gate|check|subj|$o|" "$LOG" || missing="$missing $o"
done
if [ -z "$missing" ]; then
  pass "all six outcome tokens recorded"
else
  fail "all six outcome tokens recorded" "missing:$missing"
fi

# An outcome outside the vocabulary is counted in the rollup's Events total but
# increments no outcome column, so the row stops adding up while still looking
# like a measurement. It must be coerced, and the original kept for diagnosis.
reload
export BIOTRACKR_OBS_ENABLED=1
emit_event verify build svc passed 10
if [ "$(field 6)" = "error" ]; then
  pass "outcome outside the vocabulary coerced to error"
else
  fail "outcome outside the vocabulary coerced to error" "field 6 was '$(field 6)'; an unknown token would leave the rollup row not adding up"
fi
case "$(field 8)" in
  *"bad-outcome:passed"*) pass "coerced outcome preserves the original in detail" ;;
  *) fail "coerced outcome preserves the original in detail" "detail was '$(field 8)'; expected it to name 'passed'" ;;
esac

# --- ref sanitisation ----------------------------------------------------
# git forbids space, `~`, `^`, `:`, `?`, `*`, `[` and `\` in a ref name, but
# not `|`. An unsanitised ref is the one generated field that can widen a
# record and shift every column after it.
reload
export BIOTRACKR_OBS_ENABLED=1
mkdir -p "$T/root/.git/refs/heads"
printf 'ref: refs/heads/a|b\n' > "$T/root/.git/HEAD"
printf '0123456789abcdef0123456789abcdef01234567\n' > "$T/root/.git/refs/heads/a|b"
unset _OBS_REF _OBS_REF_RESOLVED
emit_event verify build svc pass 1
nf="$(tail -1 "$LOG" | awk -F'|' '{print NF}')"
if [ "$nf" -eq 9 ]; then
  pass "delimiter in branch name: still 9 fields"
else
  fail "delimiter in branch name: still 9 fields" "found $nf; the ref widened the record and shifted every column"
fi

# --- session correlation -------------------------------------------------
reload
export BIOTRACKR_OBS_ENABLED=1
emit_event a b c pass 1
emit_event d e f pass 1
s1="$(awk -F'|' '!/^#schema/{print $2; exit}' "$LOG")"
s2="$(tail -1 "$LOG" | awk -F'|' '{print $2}')"
if [ -n "$s1" ] && [ "$s1" = "$s2" ]; then
  pass "session shared within the idle window"
else
  fail "session shared within the idle window" "'$s1' vs '$s2'"
fi

# Age the sentinel past the idle threshold; the next emit must start a new
# session. Rewriting the stored timestamp avoids waiting for real time.
old_epoch=$(( $(awk '{print $2}' "$SENTINEL") - 99999 ))
awk -v e="$old_epoch" '{print $1, e, $3}' "$SENTINEL" > "$SENTINEL.tmp" && mv "$SENTINEL.tmp" "$SENTINEL"
emit_event g h i pass 1
s3="$(tail -1 "$LOG" | awk -F'|' '{print $2}')"
if [ -n "$s3" ] && [ "$s3" != "$s2" ]; then
  pass "session rolls over after the idle gap"
else
  fail "session rolls over after the idle gap" "'$s2' then '$s3' — expected a different id"
fi

# --- rotation ------------------------------------------------------------
# The property that matters is that nothing is lost: a record may not have been
# promoted yet, so rotation must archive rather than discard.
reload
export BIOTRACKR_OBS_ENABLED=1
export BIOTRACKR_OBS_MAX_BYTES=2000
unset _BIOTRACKR_OBS_LOADED _OBS_REF
# shellcheck disable=SC1090
. "$SRC/scripts/observability.sh"
i=0
while [ "$i" -lt 40 ]; do
  emit_event verify build "Biotrackr.Service$i" pass 100 "record-$i"
  i=$((i + 1))
done
arch="$(ls -1 "$T/root/.copilot-tracking/observability"/events-*.log 2>/dev/null | wc -l | tr -d ' ')"
if [ "$arch" -ge 1 ]; then pass "rotation produced an archive"; else fail "rotation produced an archive" "none found"; fi

total="$(cat "$T/root/.copilot-tracking/observability"/events*.log 2>/dev/null | grep -vc '^#schema')"
if [ "$total" -eq 40 ]; then
  pass "rotation preserved every record (40)"
else
  fail "rotation preserved every record" "expected 40, found $total — records were discarded"
fi

live="$(wc -c < "$LOG" | tr -d ' ')"
if [ "$live" -le 2500 ]; then pass "live log bounded after rotation ($live bytes)"; else fail "live log bounded after rotation" "$live bytes"; fi

v="$(arity_violations)"
if [ "$v" -eq 0 ]; then pass "no arity violations across live and archives"; else fail "no arity violations across live and archives" "$v bad records"; fi
unset BIOTRACKR_OBS_MAX_BYTES

# --- promotion -----------------------------------------------------------
# Promotion holds the only irreversible decision in the library: advancing the
# watermark declares records finished with, and a record wrongly skipped is
# gone from the rollup for good. Nothing here emits, because the cases that
# matter are ones emit_event cannot be made to produce on demand — a shared
# millisecond, a batch spanning two refs.

seed_store() {  # seed_store <record-line>...
  rm -rf "$T/root"
  mkdir -p "$T/root/.copilot-tracking/observability"
  METRICS="$T/root/.copilot-tracking/harness-evolution-metrics.md"
  WM="$T/root/.copilot-tracking/observability/promoted"
  LOG="$T/root/.copilot-tracking/observability/events.log"
  : > "$METRICS"
  printf '#schema 1 ts|session|component|action|subject|outcome|ms|detail|ref\n' > "$LOG"
  [ "$#" -eq 0 ] || printf '%s\n' "$@" >> "$LOG"
  unset _BIOTRACKR_OBS_LOADED _OBS_REF _OBS_REF_RESOLVED
  export BIOTRACKR_OBS_ROOT="$T/root"
  # shellcheck disable=SC1090
  . "$SRC/scripts/observability.sh"
}

metrics_rows() { local n; n="$(grep -c '^| ' "$METRICS" 2>/dev/null)" || n=0; echo "${n:-0}"; }
metrics_events() { awk -F'|' '/^\| /{gsub(/[[:space:]]/,"",$5); s+=$5} END{print s+0}' "$METRICS" 2>/dev/null; }
metrics_col() { awk -F'|' -v r="$1" -v c="$2" '/^\| /{n++; if (n==r) {gsub(/[[:space:]]/,"",$c); print $c}}' "$METRICS"; }

seed_store \
  '2026-08-23T10:00:00.100Z|s-1|verify|build|svc-a|pass|100||core/obs@abc1234' \
  '2026-08-23T10:00:00.200Z|s-1|verify|build|svc-b|fail|200||core/obs@abc1234'
promote_observability
if [ "$(metrics_rows)" -eq 1 ]; then pass "promotion writes one row"; else fail "promotion writes one row" "found $(metrics_rows)"; fi
if [ "$(metrics_col 1 3)" = "core/obs" ]; then pass "row labelled with the recorded ref"; else fail "row labelled with the recorded ref" "got '$(metrics_col 1 3)'"; fi
if [ "$(metrics_col 1 5)" = "2" ] && [ "$(metrics_col 1 6)" = "1" ] && [ "$(metrics_col 1 7)" = "1" ]; then
  pass "row totals events and outcomes"
else
  fail "row totals events and outcomes" "events=$(metrics_col 1 5) pass=$(metrics_col 1 6) fail=$(metrics_col 1 7); expected 2/1/1"
fi

promote_observability
if [ "$(metrics_rows)" -eq 1 ]; then
  pass "re-promotion with no new records adds nothing"
else
  fail "re-promotion with no new records adds nothing" "row count grew to $(metrics_rows); the watermark did not hold"
fi

# The regression this watermark format exists for. Timestamps resolve to
# milliseconds, so records can share the boundary one. A strict `>` comparison
# against the previous maximum skipped every later record on that millisecond,
# permanently and with no symptom.
seed_store \
  '2026-08-23T10:00:00.500Z|s-1|verify|build|a|pass|1||b@1111111' \
  '2026-08-23T10:00:00.500Z|s-1|verify|build|b|pass|1||b@1111111'
promote_observability
printf '%s\n' '2026-08-23T10:00:00.500Z|s-1|verify|build|c|pass|1||b@1111111' >> "$LOG"
promote_observability
ev="$(metrics_events)"
if [ "$ev" -eq 3 ]; then
  pass "record sharing the watermark millisecond is still promoted"
else
  fail "record sharing the watermark millisecond is still promoted" "promoted $ev of 3 events; a record on the boundary millisecond was dropped for good"
fi

# A batch spanning two refs must not be attributed to whichever one happens to
# be checked out when the push runs.
seed_store \
  '2026-08-23T11:00:00.100Z|s-1|verify|build|a|pass|1||branch-one@aaaaaaa' \
  '2026-08-23T11:00:00.200Z|s-2|verify|build|b|fail|2||branch-two@bbbbbbb'
promote_observability
if [ "$(metrics_rows)" -eq 2 ]; then pass "batch spanning two refs writes a row each"; else fail "batch spanning two refs writes a row each" "found $(metrics_rows) row(s); records were merged under one ref"; fi
refs="$(awk -F'|' '/^\| /{gsub(/[[:space:]]/,"",$3); print $3}' "$METRICS" | sort | tr '\n' ' ')"
if [ "$refs" = "branch-one branch-two " ]; then
  pass "each row carries its own ref"
else
  fail "each row carries its own ref" "got '$refs'"
fi

# Promotion is called unconditionally from the push gate, so an absent store,
# an absent rollup and an empty batch must all be non-events.
seed_store
promote_observability
rc=$?
if [ "$rc" -eq 0 ] && [ "$(metrics_rows)" -eq 0 ]; then
  pass "empty store promotes nothing and returns 0"
else
  fail "empty store promotes nothing and returns 0" "rc=$rc rows=$(metrics_rows)"
fi

# --- caller safety -------------------------------------------------------
# The library is sourced into git hooks whose exit-code contract is
# load-bearing. An `exit` inside it would terminate the hook.
if grep -Eq '^[[:space:]]*exit[[:space:]]' "$SRC/scripts/observability.sh"; then
  fail "library never calls exit" "an exit would terminate the sourcing hook, not the function"
else
  pass "library never calls exit"
fi

if grep -Eq '^[[:space:]]*set[[:space:]]+-' "$SRC/scripts/observability.sh"; then
  fail "library sets no shell options" "options set in a sourced file leak into the caller"
else
  pass "library sets no shell options"
fi

echo ""
echo "passed=$PASS failed=$FAIL"
[ "$FAIL" -eq 0 ]
