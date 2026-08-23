#!/usr/bin/env bash
# ─────────────────────────────────────────────────────────────────────────────
# scripts/audit-harness.sh — Biotrackr agent-harness budget and hygiene audit
#
# Usage:
#   bash scripts/audit-harness.sh [path/to/repo]
#
# Measures the Biotrackr agent harness against explicit budgets and reports
# WHAT failed / WHY it matters / HOW to fix it. Exits non-zero on any CRITICAL
# violation so it can gate CI.
#
# Runs on Git Bash (Windows) and the Linux devcontainer. Requires bash 4+,
# grep, sed, awk, sort, comm, tr, wc. Does NOT require jq, Node, or Python.
#
# Structure adapted from walkinglabs/learn-harness-engineering (MIT, (c) 2025
# WalkingLab; original author Stephen Kimoi): severity model, output helpers,
# remediation block, and exit contract. Checks C1-C6 are Biotrackr-original.
# https://github.com/walkinglabs/learn-harness-engineering/blob/main/LICENSE
# ─────────────────────────────────────────────────────────────────────────────

set -euo pipefail
export LC_ALL=C

# ── Arguments ────────────────────────────────────────────────────────────────
# --json writes a machine-readable copy of every check result to a file. It
# never touches stdout: the human report is consumed verbatim by another
# workflow, so it has to stay byte-identical whether or not --json is passed.
JSON_MODE=0
JSON_OUT=""
POSITIONAL=()
while [[ $# -gt 0 ]]; do
  case "$1" in
    --json)     JSON_MODE=1 ;;
    --json-out) JSON_OUT="${2:-}"; shift ;;
    --)         shift; break ;;
    *)          POSITIONAL+=("$1") ;;
  esac
  shift
done

REPO="${POSITIONAL[0]:-.}"
REPO="${REPO%/}"

# ── Budgets (override via environment) ───────────────────────────────────────
# A ratchet. Each value is the figure achieved by the harness refactor plus a
# small margin, so the next drift trips the check rather than being absorbed.
BUDGET_ALWAYS_LOADED="${BUDGET_ALWAYS_LOADED:-240}"        # total lines, C1 (achieved 207)
BUDGET_INSTRUCTION_FILE="${BUDGET_INSTRUCTION_FILE:-100}"  # lines per file, C2 (achieved 96)
BUDGET_DUPLICATE_PCT="${BUDGET_DUPLICATE_PCT:-10}"         # max % overlap, C3 (achieved 6)
BUDGET_INSTRUCTION_STACK="${BUDGET_INSTRUCTION_STACK:-220}" # lines loaded for any one file, C4
# A ratchet on existing debt rather than a budget: 30 of 37 skill descriptions
# already exceed the documented ~150-char listing cap (harness-guide.md). The
# figure is pinned at the current count so the debt cannot grow while it is
# driven down. It is not zero because fixing 30 descriptions is its own cycle.
BUDGET_SKILL_DESC_OVERLONG="${BUDGET_SKILL_DESC_OVERLONG:-30}" # count over cap, C10 (achieved 30)

# ── Output helpers ── PORTED (MIT, walkinglabs) ──────────────────────────────
if [[ -t 1 ]]; then
  RED=$'\033[0;31m'; GREEN=$'\033[0;32m'; YELLOW=$'\033[1;33m'
  CYAN=$'\033[0;36m'; BOLD=$'\033[1m'; RESET=$'\033[0m'
else
  RED=''; GREEN=''; YELLOW=''; CYAN=''; BOLD=''; RESET=''
fi

pass()   { printf '  %s[PASS]%s %s\n' "$GREEN" "$RESET" "$1"; }
fail()   { printf '  %s[FAIL]%s %s\n' "$RED" "$RESET" "$1"; }
warn()   { printf '  %s[WARN]%s %s\n' "$YELLOW" "$RESET" "$1"; }

# The leading token of each section heading ("C1", "C7", ...) is the stable
# check identifier for machine-readable output. The human heading is unchanged.
CURRENT_CHECK=""
header() {
  CURRENT_CHECK="${1%%:*}"
  printf '\n%s%s%s\n' "$CYAN$BOLD" "$1" "$RESET"
}

CRITICAL_PASS=0; CRITICAL_FAIL=0
RECOMMENDED_PASS=0; RECOMMENDED_FAIL=0
RECS=()

# Parallel accumulator for --json. Populated for EVERY check, pass or fail,
# because a consumer trending results needs the passes too. Mirrors the RECS
# pattern rather than inventing a second mechanism.
JSON_RECS=()

# jq is unavailable on the Windows host and the container python has no json
# module, so escaping is done here with parameter expansion only.
json_escape() {
  local s="${1:-}"
  s="${s//\\/\\\\}"
  s="${s//\"/\\\"}"
  s="${s//$'\t'/ }"
  s="${s//$'\r'/}"
  s="${s//$'\n'/ }"
  printf '%s' "$s"
}

json_record() {
  local id="$1" sev="$2" result="$3" desc="$4" why="${5:-}" how="${6:-}"
  JSON_RECS+=("$(printf '{"check":"%s","severity":"%s","result":"%s","what":"%s","why":"%s","how":"%s"}' \
    "$(json_escape "$id")" "$(json_escape "$sev")" "$(json_escape "$result")" \
    "$(json_escape "$desc")" "$(json_escape "$why")" "$(json_escape "$how")")")
}

# Written from the EXIT trap rather than inline, so results survive an early
# exit. That matters in both directions: the audit exits 1 on a CRITICAL
# failure, which is exactly when a consumer most wants the machine-readable
# detail, and `set -e` can abort a run part-way when the repo is incomplete.
JSON_WRITTEN=0
write_json_output() {
  [[ $JSON_MODE -eq 1 ]] || return 0
  [[ $JSON_WRITTEN -eq 0 ]] || return 0
  JSON_WRITTEN=1

  if [[ -z "$JSON_OUT" ]]; then
    local _ts _dir
    _ts="$(date -u +%Y%m%dT%H%M%SZ 2>/dev/null || echo unknown)"
    _dir="${BIOTRACKR_AUDIT_JSON_DIR:-$REPO/.copilot-tracking/observability/audit}"
    mkdir -p "$_dir" 2>/dev/null || true
    JSON_OUT="$_dir/audit-${_ts}-$$.json"
  else
    mkdir -p "$(dirname "$JSON_OUT")" 2>/dev/null || true
  fi

  {
    printf '{\n'
    printf '  "generated": "%s",\n' "$(date -u +%Y-%m-%dT%H:%M:%SZ 2>/dev/null || echo unknown)"
    printf '  "critical": {"pass": %d, "fail": %d},\n' "$CRITICAL_PASS" "$CRITICAL_FAIL"
    printf '  "recommended": {"pass": %d, "fail": %d},\n' "$RECOMMENDED_PASS" "$RECOMMENDED_FAIL"
    printf '  "checks": [\n'
    local _n=${#JSON_RECS[@]} _i=0 _rec
    for _rec in ${JSON_RECS[@]+"${JSON_RECS[@]}"}; do
      _i=$((_i + 1))
      if [[ $_i -lt $_n ]]; then printf '    %s,\n' "$_rec"; else printf '    %s\n' "$_rec"; fi
    done
    printf '  ]\n'
    printf '}\n'
  } > "$JSON_OUT" 2>/dev/null || true

  # stderr, never stdout: the human report must stay byte-identical.
  printf 'audit: machine-readable results written to %s\n' "$JSON_OUT" >&2
}

# ── Agent-oriented failure recorder ── BIOTRACKR-ORIGINAL ────────────────────
# A failure an agent can act on must state WHAT broke, WHY the rule exists,
# and HOW to fix it. Emit all three, always.
record_fix() {
  local sev="$1" what="$2" why="$3" how="$4"
  RECS+=("${sev}|${what}|${why}|${how}")
}

check_critical() {
  local desc="$1" result="$2" why="${3:-}" how="${4:-}"
  if [[ "$result" == "pass" ]]; then
    pass "[CRITICAL] $desc"; CRITICAL_PASS=$((CRITICAL_PASS + 1))
    json_record "$CURRENT_CHECK" "CRITICAL" "pass" "$desc" "" ""
  else
    fail "[CRITICAL] $desc"; CRITICAL_FAIL=$((CRITICAL_FAIL + 1))
    [[ -n "$how" ]] && record_fix "CRITICAL" "$desc" "$why" "$how"
    json_record "$CURRENT_CHECK" "CRITICAL" "fail" "$desc" "$why" "$how"
  fi
  return 0
}

check_recommended() {
  local desc="$1" result="$2" why="${3:-}" how="${4:-}"
  if [[ "$result" == "pass" ]]; then
    pass "[RECOMMENDED] $desc"; RECOMMENDED_PASS=$((RECOMMENDED_PASS + 1))
    json_record "$CURRENT_CHECK" "RECOMMENDED" "pass" "$desc" "" ""
  else
    warn "[RECOMMENDED] $desc"; RECOMMENDED_FAIL=$((RECOMMENDED_FAIL + 1))
    [[ -n "$how" ]] && record_fix "RECOMMENDED" "$desc" "$why" "$how"
    json_record "$CURRENT_CHECK" "RECOMMENDED" "fail" "$desc" "$why" "$how"
  fi
  return 0
}

# ── Portable primitives ── BIOTRACKR-ORIGINAL ────────────────────────────────
TMPDIR_AUDIT="$(mktemp -d)"
trap 'write_json_output; rm -rf "$TMPDIR_AUDIT"' EXIT

# Safe glob probe. compgen -G avoids spawning ls on an attacker-controlled
# path, closing the CWE-78 hole patched upstream.
any_file_match() {
  local pattern
  for pattern in "$@"; do
    if compgen -G "$REPO/$pattern" > /dev/null 2>&1; then
      echo "pass"; return
    fi
  done
  echo "fail"
}

# Replaces the upstream makefile_has_target helper: make is absent from Git
# Bash on Windows ARM64, so repo operations are shell scripts instead.
script_exists() {
  any_file_match "scripts/$1"
}

# Line count with CRLF stripped. Missing file counts as 0.
lines_of() {
  [[ -f "$1" ]] || { echo 0; return; }
  tr -d '\r' < "$1" | wc -l | tr -d ' '
}

# Normalised, deduped, sorted body lines: strips CR, collapses whitespace,
# drops blanks and HTML comments. Used for C3 overlap detection.
normalize_body() {
  tr -d '\r' < "$1" \
    | sed -e 's/[[:space:]][[:space:]]*/ /g' -e 's/^ //' -e 's/ $//' \
    | grep -v '^$' \
    | grep -v '^<!--' \
    | sort -u
}

# Harness corpus: every file an agent can read as harness config.
# .github/workflows is included because gh-aw workflows pull skills and
# instructions in via {{#runtime-import <path>}}. docs/ is included because
# the harness inventory lives in docs/harness-guide.md. Omitting either
# reports a referenced artifact as an orphan.
harness_corpus() {
  local d f
  for d in "$REPO/.github/instructions" "$REPO/.github/agents" \
           "$REPO/.github/prompts" "$REPO/.github/skills" "$REPO/docs"; do
    [[ -d "$d" ]] && find "$d" -type f -name '*.md' 2>/dev/null
  done
  [[ -d "$REPO/.github/workflows" ]] && \
    find "$REPO/.github/workflows" -type f \( -name '*.md' -o -name '*.lock.yml' \) 2>/dev/null
  for f in "$REPO/AGENTS.md" "$REPO/CLAUDE.md" \
           "$REPO/.github/copilot-instructions.md"; do
    [[ -f "$f" ]] && echo "$f"
  done
  return 0
}

echo "${BOLD}Biotrackr Harness Audit${RESET}"
echo "Repo: $REPO"
echo "Budgets: always-loaded <= ${BUDGET_ALWAYS_LOADED} lines | per-instruction-file <= ${BUDGET_INSTRUCTION_FILE} lines | duplication <= ${BUDGET_DUPLICATE_PCT}%"

# ═════════════════════════════════════════════════════════════════════════════
# C1 — Always-loaded context budget                        BIOTRACKR-ORIGINAL
# ═════════════════════════════════════════════════════════════════════════════
header "C1: Always-Loaded Context Budget"

AL_FILES=("AGENTS.md" ".github/copilot-instructions.md" "CLAUDE.md")
AL_TOTAL=0
for f in "${AL_FILES[@]}"; do
  n="$(lines_of "$REPO/$f")"
  AL_TOTAL=$((AL_TOTAL + n))
  printf '         %-40s %5s lines\n' "$f" "$n"
done

check_critical "Always-loaded total is within budget (${AL_TOTAL} / ${BUDGET_ALWAYS_LOADED} lines)" \
  "$([[ $AL_TOTAL -le $BUDGET_ALWAYS_LOADED ]] && echo pass || echo fail)" \
  "Every file in this set is injected into EVERY agent turn. At ${AL_TOTAL} lines the agent burns context on boilerplate before it reads a single line of your code, and instructions buried mid-block get lost (lost-in-the-middle degradation)." \
  "Reduce the set to <= ${BUDGET_ALWAYS_LOADED} lines. Move detail out of AGENTS.md and .github/copilot-instructions.md into path-scoped .github/instructions/*.instructions.md files (loaded on demand) or docs/ topic files (loaded on reference). Keep the always-loaded files as routers: what the system is, how to build/test, hard boundaries, and pointers."

# ═════════════════════════════════════════════════════════════════════════════
# C2 — Per-instruction-file budget                         BIOTRACKR-ORIGINAL
# ═════════════════════════════════════════════════════════════════════════════
header "C2: Per-Instruction-File Budget"

INSTR_DIR="$REPO/.github/instructions"
if [[ ! -d "$INSTR_DIR" ]]; then
  check_recommended "Instruction directory present" "fail" \
    "Path-scoped instruction files are how detail stays out of the always-loaded set." \
    "Create .github/instructions/ and add *.instructions.md files with an applyTo glob in YAML frontmatter."
else
  OVERSIZE=0
  for f in "$INSTR_DIR"/*.instructions.md; do
    [[ -e "$f" ]] || continue
    n="$(lines_of "$f")"
    rel="${f#"$REPO"/}"
    if [[ $n -gt $BUDGET_INSTRUCTION_FILE ]]; then
      OVERSIZE=$((OVERSIZE + 1))
      printf '         %-62s %5s lines  %sOVER%s\n' "$rel" "$n" "$YELLOW" "$RESET"
      record_fix "RECOMMENDED" \
        "$rel is ${n} lines (budget ${BUDGET_INSTRUCTION_FILE})" \
        "Oversized instruction files defeat progressive disclosure: once the glob matches, the whole file enters context even when only one section is relevant to the file being edited." \
        "Split $rel. Extract the largest sections into a companion doc under docs/ and reference it, or split into two narrower instruction files with tighter applyTo globs."
    else
      printf '         %-62s %5s lines\n' "$rel" "$n"
    fi
  done
  check_recommended "All instruction files within ${BUDGET_INSTRUCTION_FILE}-line budget" \
    "$([[ $OVERSIZE -eq 0 ]] && echo pass || echo fail)" \
    "Oversized instruction files defeat progressive disclosure." \
    "See the per-file entries above; ${OVERSIZE} file(s) exceed the budget."
fi

# ═════════════════════════════════════════════════════════════════════════════
# C3 — Duplicate content between always-loaded files       BIOTRACKR-ORIGINAL
# ═════════════════════════════════════════════════════════════════════════════
header "C3: Duplicate Content Across Always-Loaded Files"

A="$REPO/AGENTS.md"
B="$REPO/.github/copilot-instructions.md"

if [[ -f "$A" && -f "$B" ]]; then
  normalize_body "$A" > "$TMPDIR_AUDIT/a.txt"
  normalize_body "$B" > "$TMPDIR_AUDIT/b.txt"
  A_N="$(wc -l < "$TMPDIR_AUDIT/a.txt" | tr -d ' ')"
  SHARED="$(comm -12 "$TMPDIR_AUDIT/a.txt" "$TMPDIR_AUDIT/b.txt" | wc -l | tr -d ' ')"
  if [[ "$A_N" -gt 0 ]]; then
    DUP_PCT=$(( SHARED * 100 / A_N ))
  else
    DUP_PCT=0
  fi

  printf '         AGENTS.md unique body lines:               %5s\n' "$A_N"
  printf '         Shared verbatim with copilot-instructions: %5s (%s%%)\n' "$SHARED" "$DUP_PCT"

  SHARED_H2="$(comm -12 "$TMPDIR_AUDIT/a.txt" "$TMPDIR_AUDIT/b.txt" | grep '^## ' || true)"
  if [[ -n "$SHARED_H2" ]]; then
    echo "         Duplicated top-level sections:"
    echo "$SHARED_H2" | sed 's/^/           - /'
  fi

  check_critical "Duplication between AGENTS.md and copilot-instructions.md within ${BUDGET_DUPLICATE_PCT}% (actual ${DUP_PCT}%)" \
    "$([[ $DUP_PCT -le $BUDGET_DUPLICATE_PCT ]] && echo pass || echo fail)" \
    "Both files load on every turn. ${SHARED} duplicated lines are paid for twice in context on every single request, and when the two copies drift the agent receives two conflicting versions of the same rule with no way to tell which is authoritative." \
    "Pick one owner per section. Recommended split: AGENTS.md owns the cross-tool content (the agents.md spec), and .github/copilot-instructions.md keeps only Copilot-specific additions plus a pointer to AGENTS.md. Delete the duplicated sections listed above from the non-owning file and replace each with a one-line cross-reference."
else
  check_recommended "Both always-loaded markdown files present for duplication check" "fail" \
    "Cannot compare duplication when one file is missing." \
    "Ensure AGENTS.md and .github/copilot-instructions.md both exist."
fi

# ═════════════════════════════════════════════════════════════════════════════
# C4 — applyTo glob collisions and instruction stacking    BIOTRACKR-ORIGINAL
# ═════════════════════════════════════════════════════════════════════════════
header "C4: Instruction applyTo Globs and Stacking"

GLOBS="$TMPDIR_AUDIT/globs.txt"
: > "$GLOBS"

if [[ -d "$INSTR_DIR" ]]; then
  for f in "$INSTR_DIR"/*.instructions.md; do
    [[ -e "$f" ]] || continue
    rel="${f#"$REPO"/}"
    # First applyTo: line; strip surrounding quotes; split on commas.
    line="$(tr -d '\r' < "$f" | grep -m1 '^applyTo:' || true)"
    if [[ -z "$line" ]]; then
      record_fix "RECOMMENDED" "$rel has no applyTo glob" \
        "Without applyTo the file's activation scope is undefined, so it may never load or may load everywhere." \
        "Add 'applyTo: \"<glob>\"' to the YAML frontmatter of $rel."
      continue
    fi
    value="$(printf '%s' "$line" | sed -e 's/^applyTo:[[:space:]]*//' -e 's/^["'\'']//' -e 's/["'\'']$//')"
    printf '%s' "$value" | tr ',' '\n' \
      | sed -e 's/^[[:space:]]*//' -e 's/[[:space:]]*$//' \
      | grep -v '^$' > "$TMPDIR_AUDIT/one.txt" || true
    while IFS= read -r g; do
      printf '%s\t%s\t%s\n' "$g" "$rel" "$(lines_of "$f")" >> "$GLOBS"
    done < "$TMPDIR_AUDIT/one.txt"
  done
fi

COLLISIONS=0
if [[ -s "$GLOBS" ]]; then
  DUPGLOBS="$(cut -f1 "$GLOBS" | sort | uniq -d || true)"
  if [[ -n "$DUPGLOBS" ]]; then
    while IFS= read -r g; do
      [[ -n "$g" ]] || continue
      COLLISIONS=$((COLLISIONS + 1))
      owners="$(awk -F'\t' -v G="$g" '$1==G {print $2}' "$GLOBS" | paste -sd', ' - | sed 's/,/, /g')"
      printf '         %sIDENTICAL%s  %-32s <- %s\n' "$YELLOW" "$RESET" "$g" "$owners"
      record_fix "RECOMMENDED" \
        "Glob '$g' is claimed by multiple instruction files: $owners" \
        "Both files load simultaneously for every matching file, doubling the injected instruction bytes and creating ambiguity when the two files give overlapping or conflicting guidance on the same topic." \
        "Either narrow one glob so the two no longer overlap (e.g. scope one to a subdirectory), or merge the two files if they genuinely cover the same concern."
    done <<< "$DUPGLOBS"
  fi
fi

# Identical globs are the easy case. The expensive one is INTERSECTION: distinct
# globs that still match the same files. `**/*.cs`, `**/*Tests*/**/*.cs` and
# `**/*Repository*.cs` are all different strings and all match a repository class
# inside a test project. Comparing glob strings cannot see that, so resolve each
# glob against the real file list and measure what actually stacks.
glob_to_ere() {
  printf '%s' "$1" | sed \
    -e 's/[].[^$()+|{}]/\\&/g' \
    -e 's#\*\*/#@@DIRSTAR@@#g' \
    -e 's#\*\*#@@STAR2@@#g' \
    -e 's#\*#@@STAR1@@#g' \
    -e 's#?#[^/]#g' \
    -e 's#@@DIRSTAR@@#(.*/)?#g' \
    -e 's#@@STAR2@@#.*#g' \
    -e 's#@@STAR1@@#[^/]*#g'
}

FILELIST="$TMPDIR_AUDIT/files.txt"
( cd "$REPO" && git ls-files -co --exclude-standard 2>/dev/null ) \
  | grep -E '\.(cs|csproj|bicep|razor|css|yml|yaml|md|props|targets)$' \
  > "$FILELIST" || true

STACK_MAX=0
STACK_FILE=""
if [[ -s "$FILELIST" && -s "$GLOBS" ]]; then
  HITS="$TMPDIR_AUDIT/hits.txt"
  : > "$HITS"
  while IFS=$'\t' read -r g owner glines; do
    [[ -n "$g" ]] || continue
    ere="$(glob_to_ere "$g")"
    grep -E "^${ere}$" "$FILELIST" 2>/dev/null \
      | awk -v O="$owner" -v L="$glines" '{print $0 "\t" O "\t" L}' >> "$HITS" || true
  done < "$GLOBS"

  # One instruction file can match a given source file through several of its
  # own globs. Dedupe on (file, owner) so its lines are counted once.
  sort -u "$HITS" > "$TMPDIR_AUDIT/hits-uniq.txt"
  awk -F'\t' '
    { key = $1 "\t" $2 }
    !seen[key]++ { total[$1] += $3; owners[$1]++ }
    END { for (f in total) printf "%d\t%d\t%s\n", total[f], owners[f], f }
  ' "$TMPDIR_AUDIT/hits-uniq.txt" | sort -rn > "$TMPDIR_AUDIT/stack.txt"

  if [[ -s "$TMPDIR_AUDIT/stack.txt" ]]; then
    STACK_MAX="$(head -1 "$TMPDIR_AUDIT/stack.txt" | cut -f1)"
    STACK_FILE="$(head -1 "$TMPDIR_AUDIT/stack.txt" | cut -f3)"
    echo "         Worst-case instruction stacks:"
    head -3 "$TMPDIR_AUDIT/stack.txt" | while IFS=$'\t' read -r tot cnt path; do
      printf '           %5s lines from %s files  %s\n' "$tot" "$cnt" "$path"
    done
  fi
fi

check_recommended "No duplicate applyTo globs across instruction files" \
  "$([[ $COLLISIONS -eq 0 ]] && echo pass || echo fail)" \
  "Colliding globs double-load instructions for the same file type." \
  "Resolve the ${COLLISIONS} collision(s) listed above."

check_recommended "Worst-case instruction stack within budget (${STACK_MAX} / ${BUDGET_INSTRUCTION_STACK} lines)" \
  "$([[ $STACK_MAX -le $BUDGET_INSTRUCTION_STACK ]] && echo pass || echo fail)" \
  "Editing ${STACK_FILE} loads ${STACK_MAX} lines of instruction before the agent reads a line of the file itself. Identical-glob checks cannot see this, because the globs involved are different strings that happen to match the same files." \
  "Narrow the overlapping globs so fewer instruction files match the same path, or move shared guidance into one file. Run 'bash scripts/audit-harness.sh' and read the worst-case list above to see which paths are affected."

# ═════════════════════════════════════════════════════════════════════════════
# C5 — Orphaned agents / skills / prompts                  BIOTRACKR-ORIGINAL
# ═════════════════════════════════════════════════════════════════════════════
header "C5: Orphaned Harness Artifacts"

harness_corpus > "$TMPDIR_AUDIT/corpus.txt"
CORPUS=()
while IFS= read -r _cf; do
  [[ -n "$_cf" ]] && CORPUS+=("$_cf")
done < "$TMPDIR_AUDIT/corpus.txt"

# One grep pass per artifact over the whole corpus. Per-file grep invocation
# is prohibitively slow under MSYS2, where process spawn dominates runtime.
# Sets REF_TOTAL (references anywhere) and REF_INV (references excluding the
# always-loaded inventory files), both ignoring the artifact's own file.
REF_TOTAL=0
REF_INV=0
classify_refs() {
  local name="$1" selfpat="$2" f
  REF_TOTAL=0; REF_INV=0
  [[ ${#CORPUS[@]} -gt 0 ]] || return 0
  while IFS= read -r f; do
    [[ -n "$f" ]] || continue
    case "$f" in *"$selfpat"*) continue ;; esac
    REF_TOTAL=$((REF_TOTAL + 1))
    case "$f" in
      */AGENTS.md|*/CLAUDE.md|*/copilot-instructions.md|*/harness-guide.md) continue ;;
    esac
    REF_INV=$((REF_INV + 1))
  done < <(grep -lF -e "$name" -- "${CORPUS[@]}" 2>/dev/null || true)
  return 0
}

ORPHANS=0
INVENTORY_ONLY=0

audit_artifact_set() {
  local label="$1" kind="$2"   # kind: agent | skill | prompt
  local f name selfpat

  case "$kind" in
    agent)  set -- "$REPO"/.github/agents/*.agent.md ;;
    prompt) set -- "$REPO"/.github/prompts/*.prompt.md ;;
    skill)  set -- "$REPO"/.github/skills/*/ ;;
  esac

  for f in "$@"; do
    [[ -e "$f" ]] || continue
    case "$kind" in
      agent)  name="$(basename "$f" .agent.md)";  selfpat="agents/$name.agent.md" ;;
      prompt) name="$(basename "$f" .prompt.md)"; selfpat="prompts/$name.prompt.md" ;;
      skill)  name="$(basename "${f%/}")";        selfpat="skills/$name/" ;;
    esac

    classify_refs "$name" "$selfpat"

    if [[ "$REF_TOTAL" -eq 0 ]]; then
      ORPHANS=$((ORPHANS + 1))
      printf '         %sORPHAN%s         %-8s %s\n' "$RED" "$RESET" "$label" "$name"
      record_fix "RECOMMENDED" \
        "$label '$name' is referenced by no other harness file" \
        "An artifact nothing points at is dead weight: it is never discovered by an agent following the harness, it still costs review and maintenance effort, and it makes the inventory counts wrong." \
        "Either wire it in (reference it from .github/copilot-instructions.md, a prompt, or a related skill so an agent can find it) or delete .github/${kind}s/${name}* and remove its inventory row."
    elif [[ "$REF_INV" -eq 0 ]]; then
      INVENTORY_ONLY=$((INVENTORY_ONLY + 1))
      printf '         %sINVENTORY-ONLY%s %-8s %s\n' "$YELLOW" "$RESET" "$label" "$name"
    fi
  done
  return 0
}

audit_artifact_set "agent"  agent
audit_artifact_set "skill"  skill
audit_artifact_set "prompt" prompt

check_recommended "No fully-orphaned harness artifacts" \
  "$([[ $ORPHANS -eq 0 ]] && echo pass || echo fail)" \
  "Zero-reference artifacts are undiscoverable by agents and rot silently." \
  "Wire in or delete the ${ORPHANS} orphan(s) listed above."

if [[ $INVENTORY_ONLY -gt 0 ]]; then
  warn "[INFO] ${INVENTORY_ONLY} artifact(s) appear only in an inventory table and are never invoked from another prompt, agent, or skill body."
fi

# ═════════════════════════════════════════════════════════════════════════════
# C6 — Stale hand-maintained counts                        BIOTRACKR-ORIGINAL
# ═════════════════════════════════════════════════════════════════════════════
header "C6: Stale Hand-Maintained Counts"

ACTUAL_AGENTS="$(ls -1 "$REPO"/.github/agents/*.agent.md 2>/dev/null | wc -l | tr -d ' ')"
ACTUAL_PROMPTS="$(ls -1 "$REPO"/.github/prompts/*.prompt.md 2>/dev/null | wc -l | tr -d ' ')"
ACTUAL_SKILLS="$(ls -1d "$REPO"/.github/skills/*/ 2>/dev/null | wc -l | tr -d ' ')"
ACTUAL_BICEP="$(find "$REPO/infra/modules" -name '*.bicep' 2>/dev/null | wc -l | tr -d ' ')"

STALE=0
assert_count() {
  local file="$1" phrase="$2" actual="$3" label="$4"
  [[ -f "$file" ]] || return 0
  local claimed rel
  claimed="$(tr -d '\r' < "$file" | grep -oE "[0-9]+ ${phrase}" | head -1 | grep -oE '^[0-9]+' || true)"
  [[ -n "$claimed" ]] || return 0
  rel="${file#"$REPO"/}"
  if [[ "$claimed" -ne "$actual" ]]; then
    STALE=$((STALE + 1))
    printf '         %sSTALE%s %-16s claims %-4s actual %-4s (%s)\n' \
      "$YELLOW" "$RESET" "$label" "$claimed" "$actual" "$rel"
    record_fix "RECOMMENDED" \
      "$rel claims ${claimed} ${label} but the repo has ${actual}" \
      "Agents treat these inventory counts as ground truth. A wrong count makes an agent believe an artifact exists that does not, or miss one that does, and it is a reliable signal that the surrounding inventory table has also drifted." \
      "Update the '${claimed} ${phrase}' figure in $rel to ${actual}, and re-check every row of the adjacent inventory table against the filesystem."
  else
    printf '         OK    %-16s %-4s (%s)\n' "$label" "$actual" "$rel"
  fi
  return 0
}

for target in "$REPO/.github/copilot-instructions.md" "$REPO/AGENTS.md" \
              "$REPO/docs/harness-guide.md"; do
  assert_count "$target" "custom agent definitions" "$ACTUAL_AGENTS"  "agents"
  assert_count "$target" "prompt templates"         "$ACTUAL_PROMPTS" "prompts"
  assert_count "$target" "skills"                   "$ACTUAL_SKILLS"  "skills"
  assert_count "$target" "reusable Bicep modules"   "$ACTUAL_BICEP"   "bicep modules"
done

check_recommended "Hand-maintained inventory counts match the filesystem" \
  "$([[ $STALE -eq 0 ]] && echo pass || echo fail)" \
  "Stale counts mislead agents that treat the inventory as authoritative." \
  "Correct the ${STALE} stale count(s) listed above."

# ═════════════════════════════════════════════════════════════════════════════
# Summary  ── PORTED layout (MIT, walkinglabs)
# ═════════════════════════════════════════════════════════════════════════════
# ── C7: Measurement record schema ── BIOTRACKR-ORIGINAL ──────────────────────
# The evolution log's column list is restated in prose across six files with no
# shared definition. Nothing detects a divergence, and the failure is silent:
# a consumer reads the wrong column and reports a wrong number rather than an
# error. This is the one mechanical guard on that schema.
header "C7: Measurement Record Schema"

EVO_LOG="$REPO/.copilot-tracking/harness-evolution-log.md"
EVO_METRICS="$REPO/.copilot-tracking/harness-evolution-metrics.md"

# The authoritative column lists live here, in the check, so that a divergence
# anywhere else is what fails rather than what wins.
EXPECTED_EVO_COLS="Date | PR | Plan | Proposed | Accepted | Severity (C/H/M/L) | Files Modified | Status | Verdict | FixCycles | FindDensity | CycleTime | SpecClarity | FlowState"
EXPECTED_METRICS_COLS="Date | Cycle | Sessions | Events | Pass | Fail | Error | Skip | Degraded | Timeout | TotalMs"

check_table_header() {  # <file> <expected-cols> <label>
  local file="$1" expected="$2" label="$3" actual=""
  if [[ ! -f "$file" ]]; then
    echo "notfound"; return 0
  fi
  actual="$(grep -m1 -E '^\|[[:space:]]*Date[[:space:]]*\|' "$file" 2>/dev/null || true)"
  if [[ -z "$actual" ]]; then
    echo "noheader"; return 0
  fi
  # Normalise the outer pipes and collapse runs of spaces so the comparison is
  # about columns, not about how the table happens to be padded.
  actual="${actual#|}"; actual="${actual%|}"
  actual="$(printf '%s' "$actual" | sed -e 's/^[[:space:]]*//' -e 's/[[:space:]]*$//' -e 's/[[:space:]]*|[[:space:]]*/ | /g')"
  if [[ "$actual" == "$expected" ]]; then echo "pass"; else echo "$actual"; fi
}

EVO_RESULT="$(check_table_header "$EVO_LOG" "$EXPECTED_EVO_COLS" "evolution log")"
if [[ "$EVO_RESULT" == "pass" ]]; then
  EVO_COUNT="$(awk -F'|' '/^\|[[:space:]]*Date[[:space:]]*\|/{print NF-2; exit}' "$EVO_LOG" 2>/dev/null || echo 0)"
  check_critical "Evolution log header matches the declared 14-column schema (found $EVO_COUNT)" "pass"
else
  check_critical "Evolution log header matches the declared 14-column schema" "fail" \
    "Four consumers restate this column list in prose — the metrics collector, the evolve reminder, the health skill, and the SDD conventions. A divergence is never reported as an error; a consumer simply reads the wrong column and publishes a wrong number." \
    "Restore the header in .copilot-tracking/harness-evolution-log.md to exactly: | ${EXPECTED_EVO_COLS} |  (found: ${EVO_RESULT})"
fi

# The companion file does not exist until the promotion tier is built. Its
# schema is declared here anyway, so the guard and its test do not depend on
# the order the two land in.
if [[ -f "$EVO_METRICS" ]]; then
  MET_RESULT="$(check_table_header "$EVO_METRICS" "$EXPECTED_METRICS_COLS" "metrics companion")"
  if [[ "$MET_RESULT" == "pass" ]]; then
    check_critical "Metrics companion header matches its declared schema" "pass"
  else
    check_critical "Metrics companion header matches its declared schema" "fail" \
      "The companion file is the only committed record of runtime outcomes. A drifted header silently misaligns every column a consumer reads." \
      "Restore the header in .copilot-tracking/harness-evolution-metrics.md to exactly: | ${EXPECTED_METRICS_COLS} |  (found: ${MET_RESULT})"
  fi
else
  printf '         %-6s %s\n' "SKIP" "metrics companion not present yet (created with the promotion tier)"
fi

# ── C8: Measurement liveness ── BIOTRACKR-ORIGINAL ───────────────────────────
# The health skill warns when the five most recent rows are all unmeasured, and
# that window slides: every unmeasured cycle pushes the measured ones closer to
# falling out of it. Because unmeasured rows are never backfilled, once the
# window is empty it stays empty. This check makes that state fail loudly
# instead of degrading quietly into a dead trend surface.
header "C8: Measurement Liveness"

if [[ -f "$EVO_LOG" ]]; then
  LIVENESS="$(awk -F'|' '
    /^\|[[:space:]]*20[0-9][0-9]-/ {
      v = $10; gsub(/[[:space:]]/, "", v)
      rows[++n] = v
    }
    END {
      start = (n > 5) ? n - 4 : 1
      c = 0
      for (i = start; i <= n; i++)
        if (rows[i] == "APPROVE" || rows[i] == "REQUEST_CHANGES") c++
      printf "%d %d %d", c, (n > 5 ? 5 : n), n
    }' "$EVO_LOG" 2>/dev/null || echo "0 0 0")"

  MEASURED_RECENT="${LIVENESS%% *}"
  WINDOW="$(printf '%s' "$LIVENESS" | cut -d' ' -f2)"
  TOTAL_ROWS="$(printf '%s' "$LIVENESS" | cut -d' ' -f3)"

  if [[ "$TOTAL_ROWS" -eq 0 ]]; then
    check_recommended "Measurement window contains at least one measured cycle" "pass"
    printf '         %-6s %s\n' "NOTE" "no cycles logged yet"
  elif [[ "$MEASURED_RECENT" -gt 0 ]]; then
    check_critical "Measurement window contains at least one measured cycle ($MEASURED_RECENT of the last $WINDOW)" "pass"
  else
    check_critical "Measurement window contains at least one measured cycle" "fail" \
      "All $WINDOW most recent cycles are unmeasured, so the trend surface has no data to work from. Unmeasured rows are never backfilled by policy, which means this state does not recover on its own — it persists until a measured cycle is added." \
      "Complete a review for the current cycle and record its verdict, fix cycles, finding density, cycle time, and self-reported scores in .copilot-tracking/harness-evolution-log.md. Do not invent values for past cycles."
  fi
fi

# ── C9: Lifecycle subsystem coverage ── BIOTRACKR-ORIGINAL ───────────────────
# Every check above measures documents. This one measures the layer that
# actually runs: the hooks, the sensor, and the two registration files that
# decide whether an agent edit triggers verification at all. A missing
# registration is invisible — the hook simply never fires.
header "C9: Lifecycle Subsystem"

LIFECYCLE_MISSING=()
lifecycle_require() {  # <path> <role>
  if [[ -f "$REPO/$1" ]]; then
    printf '         %-6s %-34s %s\n' "OK" "$1" "$2"
  else
    LIFECYCLE_MISSING+=("$1")
    printf '         %-6s %-34s %s\n' "MISSING" "$1" "$2"
  fi
}

lifecycle_require ".githooks/pre-commit"            "secret scan, devcontainer, verify"
lifecycle_require ".githooks/pre-push"              "verify across the push range"
lifecycle_require ".githooks/commit-msg"            "commit standard enforcement"
lifecycle_require "scripts/verify.sh"               "shared affected-service sensor"
lifecycle_require "scripts/agent-post-edit.sh"      "agent post-edit handler"
lifecycle_require ".github/hooks/post-edit.json"    "VS Code Copilot registration"
lifecycle_require ".claude/settings.json"           "Claude Code registration"

if [[ ${#LIFECYCLE_MISSING[@]} -eq 0 ]]; then
  check_recommended "Lifecycle subsystem complete (7 components)" "pass"
else
  check_recommended "Lifecycle subsystem complete (7 components)" "fail" \
    "A missing hook or registration file does not raise an error — the hook simply never fires, and verification silently stops happening for every agent edit or commit that would have triggered it." \
    "Restore the missing component(s): ${LIFECYCLE_MISSING[*]}. Run bash scripts/init.sh to reinstall hooks and reset core.hooksPath."
fi

# ── C10: Skill authoring validation ── BIOTRACKR-ORIGINAL ────────────────────
# A skill whose `name` does not match its directory loads as nothing, with no
# error anywhere. The skill is simply never available, and the only symptom is
# an agent that does not do what the skill would have made it do.
header "C10: Skill Authoring"

SKILL_NAME_BAD=()
SKILL_DESC_LONG=()
SKILL_DESC_MAX=150

for skill_file in "$REPO"/.github/skills/*/SKILL.md; do
  [[ -f "$skill_file" ]] || continue
  skill_dir="$(basename "$(dirname "$skill_file")")"

  # Frontmatter only: a later body line could otherwise be mistaken for a field.
  fm_name="$(awk 'NR==1 && $0=="---"{inside=1; next} inside && $0=="---"{exit} inside && /^name:/{sub(/^name:[[:space:]]*/,""); gsub(/^["'"'"']|["'"'"']$/,""); print; exit}' "$skill_file" 2>/dev/null || true)"
  fm_desc="$(awk 'NR==1 && $0=="---"{inside=1; next} inside && $0=="---"{exit} inside && /^description:/{sub(/^description:[[:space:]]*/,""); print; exit}' "$skill_file" 2>/dev/null || true)"

  # A missing `name:` fails discovery exactly as a mismatched one does, so it
  # belongs in the same bucket. Guarding on `-n` treated the worst case — no
  # name at all — as a pass.
  if [[ -z "$fm_name" ]]; then
    SKILL_NAME_BAD+=("$skill_dir (no 'name:' in frontmatter)")
  elif [[ "$fm_name" != "$skill_dir" ]]; then
    SKILL_NAME_BAD+=("$skill_dir (declares '$fm_name')")
  fi
  if [[ ${#fm_desc} -gt $SKILL_DESC_MAX ]]; then
    SKILL_DESC_LONG+=("$skill_dir (${#fm_desc} chars)")
  fi
done

if [[ ${#SKILL_NAME_BAD[@]} -eq 0 ]]; then
  check_critical "Every skill's declared name matches its directory" "pass"
else
  for bad in "${SKILL_NAME_BAD[@]}"; do
    printf '         %-6s %s\n' "BAD" "$bad"
  done
  check_critical "Every skill's declared name matches its directory" "fail" \
    "Skill discovery is name-matched at load time. A mismatch does not warn — the skill loads as nothing and is silently unavailable, so the only symptom is an agent that never uses it." \
    "Set the frontmatter 'name:' to the parent directory name for: ${SKILL_NAME_BAD[*]}"
fi

if [[ ${#SKILL_DESC_LONG[@]} -le $BUDGET_SKILL_DESC_OVERLONG ]]; then
  check_recommended "Skill descriptions past the ${SKILL_DESC_MAX}-char cap within ratchet (${#SKILL_DESC_LONG[@]} / $BUDGET_SKILL_DESC_OVERLONG)" "pass"
  if [[ ${#SKILL_DESC_LONG[@]} -gt 0 ]]; then
    printf '         %-6s %s\n' "DEBT" "${#SKILL_DESC_LONG[@]} skill(s) exceed the cap; longest: ${SKILL_DESC_LONG[0]}"
    printf '         %-6s %s\n' "" "pre-existing, tracked as DR-04. Ratchet down; do not let it grow."
  fi
else
  for long in "${SKILL_DESC_LONG[@]}"; do
    printf '         %-6s %s\n' "LONG" "$long"
  done
  check_recommended "Skill descriptions past the ${SKILL_DESC_MAX}-char cap within ratchet" "fail" \
    "Selection is description-matched and the listing truncates at roughly ${SKILL_DESC_MAX} characters, so anything past it is invisible when the agent chooses a skill. The count grew beyond the agreed ratchet of $BUDGET_SKILL_DESC_OVERLONG." \
    "Front-load the distinctive trigger language and shorten: ${SKILL_DESC_LONG[*]}. Then lower BUDGET_SKILL_DESC_OVERLONG to the new figure."
fi

# ── Recent verification outcomes ── BIOTRACKR-ORIGINAL ───────────────────────
# The checks above describe the harness's structure. This describes what it has
# actually been doing, so one command answers "is the harness sound" and "is it
# working" together. Reporting only: the raw store is local and optional, and a
# developer who has never enabled emission must not see a failure because of it.
header "Recent Verification Outcomes"

OBS_DIR="$REPO/.copilot-tracking/observability"
OBS_FILES=()
while IFS= read -r _f; do
  [[ -n "$_f" ]] && OBS_FILES+=("$_f")
done < <(ls -1 "$OBS_DIR"/events*.log 2>/dev/null || true)

if [[ ${#OBS_FILES[@]} -eq 0 ]]; then
  printf '         %-6s %s\n' "NONE" "no raw records (emission is off by default; enable with BIOTRACKR_OBS_ENABLED=1)"
else
  OBS_SUMMARY="$(cat "${OBS_FILES[@]}" 2>/dev/null \
    | awk -F'|' '
        !/^#schema/ && NF == 9 {
          total++
          outcome[$6]++
          session[$2] = 1
          if ($7 ~ /^[0-9]+$/) ms += $7
        }
        END {
          n = 0
          for (s in session) n++
          printf "%d %d %d", total, n, ms
        }' || true)"

  OBS_TOTAL="${OBS_SUMMARY%% *}"
  OBS_SESSIONS="$(printf '%s' "$OBS_SUMMARY" | cut -d' ' -f2)"
  OBS_MS="$(printf '%s' "$OBS_SUMMARY" | cut -d' ' -f3)"

  printf '         %-6s %s\n' "" "$OBS_TOTAL record(s) across $OBS_SESSIONS session(s), ${OBS_MS}ms of verification"

  for _o in pass fail error skip degraded timeout; do
    _c="$(cat "${OBS_FILES[@]}" 2>/dev/null | awk -F'|' -v o="$_o" '!/^#schema/ && NF==9 && $6==o {n++} END{print n+0}' || echo 0)"
    [[ "$_c" -gt 0 ]] && printf '         %-10s %s\n' "$_o" "$_c"
  done

  # A malformed record is the failure mode the fixed-arity format exists to make
  # visible, so surface it here rather than letting a consumer trip over it.
  OBS_BAD="$(cat "${OBS_FILES[@]}" 2>/dev/null | awk -F'|' '!/^#schema/ && NF != 9 {n++} END{print n+0}' || echo 0)"
  if [[ "$OBS_BAD" -gt 0 ]]; then
    check_recommended "All raw records have the expected field count" "fail" \
      "A record with the wrong field count means a value contained the delimiter or a write interleaved. Every positional consumer of the store silently reads the wrong column from that line onward." \
      "Inspect $OBS_DIR/events*.log for the $OBS_BAD malformed line(s) and check the sanitisation path in scripts/observability.sh."
  else
    check_recommended "All raw records have the expected field count ($OBS_TOTAL checked)" "pass"
  fi

  # Promotion staleness. Promotion runs from the push gate, and that gate can be
  # bypassed with --no-verify or BIOTRACKR_SKIP_HOOKS. When it is, records pile
  # up locally and never reach the committed tier — silently, because nothing
  # else looks. The health skill also reports this, but it only runs when an
  # agent invokes it, so the check lives here too where the audit already runs.
  OBS_WM=""
  if [[ -r "$OBS_DIR/promoted" ]]; then
    read -r OBS_WM < "$OBS_DIR/promoted" 2>/dev/null || OBS_WM=""
  fi

  OBS_UNPROMOTED="$(cat "${OBS_FILES[@]}" 2>/dev/null \
    | awk -F'|' -v wm="${OBS_WM:-}" '!/^#schema/ && NF==9 && ($1 "") > (wm "") {n++} END{print n+0}' || echo 0)"

  # Age, not volume, is the signal: unpromoted records are normal mid-session
  # and only meaningful once they have sat there across several days of work.
  STALE_CUTOFF=""
  printf -v STALE_CUTOFF '%(%Y-%m-%d)T' $(( ${EPOCHSECONDS:-0} - 604800 )) 2>/dev/null || STALE_CUTOFF=""

  OBS_STALE=0
  if [[ -n "$STALE_CUTOFF" ]]; then
    OBS_STALE="$(cat "${OBS_FILES[@]}" 2>/dev/null \
      | awk -F'|' -v wm="${OBS_WM:-}" -v cut="$STALE_CUTOFF" \
          '!/^#schema/ && NF==9 && ($1 "") > (wm "") && ($1 "") < (cut "") {n++} END{print n+0}' || echo 0)"
  fi

  if [[ "$OBS_STALE" -gt 0 ]]; then
    check_recommended "Raw records have been promoted to the committed rollup" "fail" \
      "$OBS_STALE record(s) older than seven days have never been promoted. Promotion runs from the push gate, so this is what a repeatedly bypassed gate looks like: the raw store keeps growing while .copilot-tracking/harness-evolution-metrics.md stays frozen, and no trend anyone reads reflects the work that has actually happened." \
      "Run a push without --no-verify or BIOTRACKR_SKIP_HOOKS to promote them, or call promote_observability directly after sourcing scripts/observability.sh."
  else
    check_recommended "Raw records have been promoted to the committed rollup ($OBS_UNPROMOTED pending)" "pass"
  fi
fi

TOTAL_PASS=$((CRITICAL_PASS + RECOMMENDED_PASS))
TOTAL=$((CRITICAL_PASS + CRITICAL_FAIL + RECOMMENDED_PASS + RECOMMENDED_FAIL))

echo ""
echo "${BOLD}━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━${RESET}"
echo "${BOLD}Summary${RESET}"
echo "  Total:       ${TOTAL_PASS} / ${TOTAL} checks passing"
echo "  Critical:    ${CRITICAL_PASS} / $((CRITICAL_PASS + CRITICAL_FAIL)) (budget gates — fail the build)"
echo "  Recommended: ${RECOMMENDED_PASS} / $((RECOMMENDED_PASS + RECOMMENDED_FAIL)) (hygiene)"
echo "${BOLD}━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━${RESET}"

if [[ ${#RECS[@]} -gt 0 ]]; then
  echo ""
  echo "${BOLD}━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━${RESET}"
  echo "${BOLD}What to fix${RESET}"
  echo "${BOLD}━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━${RESET}"
  i=0
  for rec in "${RECS[@]}"; do
    i=$((i + 1))
    sev="${rec%%|*}"; rest="${rec#*|}"
    what="${rest%%|*}"; rest="${rest#*|}"
    why="${rest%%|*}"; how="${rest#*|}"
    col="$YELLOW"; [[ "$sev" == "CRITICAL" ]] && col="$RED"
    echo ""
    printf '%s[%s]%s %s#%s%s\n' "$col" "$sev" "$RESET" "$BOLD" "$i" "$RESET"
    printf '  WHAT: %s\n' "$what"
    printf '  WHY:  %s\n' "$why"
    printf '  FIX:  %s\n' "$how"
  done
  echo ""
  echo "${BOLD}━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━${RESET}"
fi

if [[ $CRITICAL_FAIL -gt 0 ]]; then
  echo ""
  echo "${RED}${BOLD}${CRITICAL_FAIL} CRITICAL budget violation(s). The harness exceeds its context budget.${RESET}"
  exit 1
else
  echo ""
  echo "${GREEN}${BOLD}All CRITICAL harness budgets are within limits.${RESET}"
  if [[ $RECOMMENDED_FAIL -gt 0 ]]; then
    echo "${YELLOW}${RECOMMENDED_FAIL} RECOMMENDED item(s) need attention. See above.${RESET}"
  fi
  exit 0
fi
