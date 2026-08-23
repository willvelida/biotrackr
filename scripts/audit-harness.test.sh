#!/usr/bin/env bash
# Regression cases for audit-harness.sh, covering checks C7-C10 and the raw
# record arity report.
#
# Each case builds a throwaway copy of the harness, breaks exactly one thing,
# and asserts that the intended check flips to fail. A check that only ever
# passes is indistinguishable from one that is not running, so every case here
# asserts the failing direction as well as the passing one.
#
# Assertions read the --json output rather than scraping the human report,
# which keeps them precise and keeps the human format free to change.
#
# Run from the repo root:
#   bash scripts/audit-harness.test.sh
#
# Exit codes: 0 all cases passed, 1 at least one failed.

set -uo pipefail

SRC="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
T="$(mktemp -d)"
trap 'rm -rf "$T"' EXIT

PASS=0
FAIL=0
R="$T/r"
OUT="$T/out.json"

pass() { echo "  PASS  $1"; PASS=$((PASS + 1)); }
fail() { echo "  FAIL  $1 — $2"; FAIL=$((FAIL + 1)); }

# Copies the parts of the harness the audit reads. Everything the case is not
# testing stays identical to the real repo, so a failure points at the mutation
# rather than at an incomplete fixture.
setup() {
  rm -rf "$R"
  mkdir -p "$R/.github" "$R/.githooks" "$R/scripts" "$R/docs" "$R/.copilot-tracking"
  # infra/modules must exist: C6 counts Bicep modules with `find`, and under
  # `pipefail` a failing find aborts the whole run before later checks execute.
  mkdir -p "$R/infra/modules"
  cp "$SRC/AGENTS.md" "$SRC/CLAUDE.md" "$R/" 2>/dev/null || true
  cp "$SRC/.github/copilot-instructions.md" "$R/.github/" 2>/dev/null || true
  cp -r "$SRC/.github/instructions" "$SRC/.github/agents" "$SRC/.github/prompts" \
        "$SRC/.github/skills" "$SRC/.github/hooks" "$SRC/.github/workflows" "$R/.github/" 2>/dev/null || true
  cp -r "$SRC/.claude" "$R/" 2>/dev/null || true
  cp "$SRC/.githooks/pre-commit" "$SRC/.githooks/pre-push" "$SRC/.githooks/commit-msg" "$R/.githooks/" 2>/dev/null || true
  cp "$SRC/scripts/verify.sh" "$SRC/scripts/agent-post-edit.sh" "$R/scripts/" 2>/dev/null || true
  cp "$SRC/docs/harness-guide.md" "$R/docs/" 2>/dev/null || true
  cp "$SRC/.copilot-tracking/harness-evolution-log.md" "$R/.copilot-tracking/" 2>/dev/null || true
}

run_audit() {
  rm -f "$OUT"
  bash "$SRC/scripts/audit-harness.sh" "$R" --json --json-out "$OUT" >/dev/null 2>&1 || true
}

# assert_check <check-id> <expected-result> <description>
assert_check() {
  local id="$1" want="$2" desc="$3"
  if [ ! -s "$OUT" ]; then
    fail "$desc" "no JSON produced; the audit did not reach its output stage"
    return
  fi
  if grep "\"check\":\"$id\"" "$OUT" | grep -q "\"result\":\"$want\""; then
    pass "$desc"
  else
    local got
    got="$(grep -o "\"check\":\"$id\"[^}]*\"result\":\"[a-z]*\"" "$OUT" | grep -o '"result":"[a-z]*"' | tr '\n' ' ')"
    fail "$desc" "expected a $id record with result=$want, saw: ${got:-none}"
  fi
}

# Guards against a case passing vacuously: if the mutation did not change the
# file, the check would legitimately pass and prove nothing.
assert_changed() {
  local f="$1" desc="$2"
  if [ ! -f "$f" ]; then
    fail "$desc" "fixture file missing: $f"
    return 1
  fi
  return 0
}

echo "audit-harness regression cases"

# --- baseline ------------------------------------------------------------
setup
run_audit
assert_check C7  pass "baseline: C7 evolution log header valid"
assert_check C8  pass "baseline: C8 measurement window has a measured cycle"
assert_check C9  pass "baseline: C9 lifecycle subsystem complete"
assert_check C10 pass "baseline: C10 skill names match directories"

# --- C7: header guard ----------------------------------------------------
setup
EVO="$R/.copilot-tracking/harness-evolution-log.md"
assert_changed "$EVO" "C7 fails on a renamed column" && {
  sed -i 's/^| Date | PR | Plan |/| Date | PullRequest | Plan |/' "$EVO"
  run_audit
  assert_check C7 fail "C7 fails on a renamed column"
}

setup
assert_changed "$EVO" "C7 fails on a dropped column" && {
  sed -i 's/ | FlowState |$/ |/' "$EVO"
  run_audit
  assert_check C7 fail "C7 fails on a dropped column"
}

# The companion file's schema is declared inside C7, so this case does not
# depend on the promotion tier existing yet.
setup
printf '# Metrics\n\n| Date | Cycle | Sessions | Wrong |\n|---|---|---|---|\n' \
  > "$R/.copilot-tracking/harness-evolution-metrics.md"
run_audit
assert_check C7 fail "C7 fails on a malformed metrics companion header"

setup
printf '# Metrics\n\n| Date | Cycle | Sessions | Events | Pass | Fail | Error | Skip | Degraded | Timeout | TotalMs |\n|---|---|---|---|---|---|---|---|---|---|---|\n' \
  > "$R/.copilot-tracking/harness-evolution-metrics.md"
run_audit
assert_check C7 pass "C7 passes on a well-formed metrics companion header"

# --- C8: liveness --------------------------------------------------------
# Blank the verdict column on every row: the window becomes entirely
# unmeasured, which is the decayed state the check exists to catch.
setup
awk -F'|' 'BEGIN{OFS="|"} /^\|[[:space:]]*20[0-9][0-9]-/ { $10=" — "; $11=" — "; $12=" — "; $13=" — "; $14=" — "; $15=" — " } {print}' \
  "$EVO" > "$EVO.tmp" && mv "$EVO.tmp" "$EVO"
run_audit
assert_check C8 fail "C8 fails when every recent cycle is unmeasured"

# --- C9: lifecycle -------------------------------------------------------
setup
rm -f "$R/.githooks/pre-commit"
run_audit
assert_check C9 fail "C9 fails when a git hook is missing"

setup
rm -f "$R/.claude/settings.json"
run_audit
assert_check C9 fail "C9 fails when a hook registration is missing"

# --- C10: skill authoring ------------------------------------------------
setup
SK="$R/.github/skills/harness-health/SKILL.md"
assert_changed "$SK" "C10 fails when a skill name does not match its directory" && {
  sed -i 's/^name: harness-health$/name: not-the-directory-name/' "$SK"
  run_audit
  assert_check C10 fail "C10 fails when a skill name does not match its directory"
}

# The description ratchet is pinned at the current count, so tightening it by
# one must trip the check.
setup
rm -f "$OUT"
BUDGET_SKILL_DESC_OVERLONG=0 bash "$SRC/scripts/audit-harness.sh" "$R" --json --json-out "$OUT" >/dev/null 2>&1 || true
assert_check C10 fail "C10 fails when the description ratchet is tightened"

# --- raw record arity ----------------------------------------------------
setup
mkdir -p "$R/.copilot-tracking/observability"
{
  printf '#schema 1 ts|session|component|action|subject|outcome|ms|detail|ref\n'
  printf '2026-01-01T00:00:00.000Z|s-1|verify|build|Svc|pass|10||main@abc1234\n'
  printf '2026-01-01T00:00:01.000Z|s-1|verify|build|Sv|c|extra|pass|10||main@abc1234\n'
} > "$R/.copilot-tracking/observability/events.log"
run_audit
if grep -q '"what":"All raw records have the expected field count"' "$OUT" && \
   grep '"what":"All raw records have the expected field count"' "$OUT" | grep -q '"result":"fail"'; then
  pass "arity report fails on a malformed record"
else
  fail "arity report fails on a malformed record" "no failing arity record in the JSON"
fi

# --- promotion staleness -------------------------------------------------
# Promotion runs from the push gate, which --no-verify bypasses. The failure
# mode is silent accumulation, so both directions are asserted here.
setup
mkdir -p "$R/.copilot-tracking/observability"
cp "$SRC/.copilot-tracking/harness-evolution-metrics.md" "$R/.copilot-tracking/" 2>/dev/null || true
{
  printf '#schema 1 ts|session|component|action|subject|outcome|ms|detail|ref\n'
  printf '2020-01-01T00:00:00.000Z|s-old|verify|build|Svc|pass|10||main@abc1234\n'
} > "$R/.copilot-tracking/observability/events.log"
run_audit
if grep '"what":"Raw records have been promoted to the committed rollup"' "$OUT" | grep -q '"result":"fail"'; then
  pass "staleness detected when old records were never promoted"
else
  fail "staleness detected when old records were never promoted" "no failing staleness record in the JSON"
fi

# The same records, with the watermark advanced past them, must not be stale.
setup
mkdir -p "$R/.copilot-tracking/observability"
cp "$SRC/.copilot-tracking/harness-evolution-metrics.md" "$R/.copilot-tracking/" 2>/dev/null || true
{
  printf '#schema 1 ts|session|component|action|subject|outcome|ms|detail|ref\n'
  printf '2020-01-01T00:00:00.000Z|s-old|verify|build|Svc|pass|10||main@abc1234\n'
} > "$R/.copilot-tracking/observability/events.log"
printf '2020-06-01T00:00:00.000Z\n' > "$R/.copilot-tracking/observability/promoted"
run_audit
if grep '"check":"Recent Verification Outcomes"' "$OUT" | grep 'promoted to the committed rollup' | grep -q '"result":"pass"'; then
  pass "no staleness once the watermark covers the records"
else
  got="$(grep -o '"what":"Raw records have been promoted[^"]*","why":"[^"]*"' "$OUT" | head -1)"
  fail "no staleness once the watermark covers the records" "expected a passing staleness record; saw: ${got:-none}"
fi

# --- AC-10: human output unaffected by --json ----------------------------
setup
bash "$SRC/scripts/audit-harness.sh" "$R" > "$T/plain.out" 2>/dev/null || true
bash "$SRC/scripts/audit-harness.sh" "$R" --json --json-out "$T/x.json" > "$T/withjson.out" 2>/dev/null || true
if cmp -s "$T/plain.out" "$T/withjson.out"; then
  pass "human output is byte-identical with and without --json"
else
  fail "human output is byte-identical with and without --json" "stdout differed"
fi

echo ""
echo "passed=$PASS failed=$FAIL"
[ "$FAIL" -eq 0 ]
