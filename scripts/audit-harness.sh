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

REPO="${1:-.}"
REPO="${REPO%/}"

# ── Budgets (override via environment) ───────────────────────────────────────
# A ratchet. Each value is the figure achieved by the harness refactor plus a
# small margin, so the next drift trips the check rather than being absorbed.
BUDGET_ALWAYS_LOADED="${BUDGET_ALWAYS_LOADED:-240}"        # total lines, C1 (achieved 207)
BUDGET_INSTRUCTION_FILE="${BUDGET_INSTRUCTION_FILE:-100}"  # lines per file, C2 (achieved 96)
BUDGET_DUPLICATE_PCT="${BUDGET_DUPLICATE_PCT:-10}"         # max % overlap, C3 (achieved 6)
BUDGET_INSTRUCTION_STACK="${BUDGET_INSTRUCTION_STACK:-220}" # lines loaded for any one file, C4

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
header() { printf '\n%s%s%s\n' "$CYAN$BOLD" "$1" "$RESET"; }

CRITICAL_PASS=0; CRITICAL_FAIL=0
RECOMMENDED_PASS=0; RECOMMENDED_FAIL=0
RECS=()

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
  else
    fail "[CRITICAL] $desc"; CRITICAL_FAIL=$((CRITICAL_FAIL + 1))
    [[ -n "$how" ]] && record_fix "CRITICAL" "$desc" "$why" "$how"
  fi
  return 0
}

check_recommended() {
  local desc="$1" result="$2" why="${3:-}" how="${4:-}"
  if [[ "$result" == "pass" ]]; then
    pass "[RECOMMENDED] $desc"; RECOMMENDED_PASS=$((RECOMMENDED_PASS + 1))
  else
    warn "[RECOMMENDED] $desc"; RECOMMENDED_FAIL=$((RECOMMENDED_FAIL + 1))
    [[ -n "$how" ]] && record_fix "RECOMMENDED" "$desc" "$why" "$how"
  fi
  return 0
}

# ── Portable primitives ── BIOTRACKR-ORIGINAL ────────────────────────────────
TMPDIR_AUDIT="$(mktemp -d)"
trap 'rm -rf "$TMPDIR_AUDIT"' EXIT

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
