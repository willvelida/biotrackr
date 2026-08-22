#!/usr/bin/env bash
# Biotrackr environment bootstrap. One entry point for the Windows host and the
# devcontainer alike — the devcontainer delegates to this script, not the
# reverse. Idempotent, non-interactive, fail-fast.
#
# Usage:
#   bash scripts/init.sh              # prerequisites + git hooks + manifest + restore
#   bash scripts/init.sh --no-restore # skip NuGet restore (fastest)
#   bash scripts/init.sh --emulator   # additionally start/await the Cosmos emulator
#
# Safe to run repeatedly and on every container start.

set -euo pipefail

REPO_ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
cd "$REPO_ROOT"

DO_RESTORE=1
DO_EMULATOR=0
for arg in "$@"; do
  case "$arg" in
    --no-restore) DO_RESTORE=0 ;;
    --emulator)   DO_EMULATOR=1 ;;
    -h|--help)    sed -n '2,12p' "${BASH_SOURCE[0]}" | sed 's/^# \{0,1\}//'; exit 0 ;;
    *) echo "init: unknown flag '$arg'. Run 'bash scripts/init.sh --help'." >&2; exit 1 ;;
  esac
done

step() { printf '\n=== %s ===\n' "$1"; }
die()  { echo "init: FATAL: $1" >&2; exit 1; }

# --- 1. Prerequisites (verify, never silently install) ------------------------
step "Checking prerequisites"

command -v git >/dev/null 2>&1 || die \
  "git is not on PATH. Install git, then re-run: bash scripts/init.sh"

command -v dotnet >/dev/null 2>&1 || die \
  ".NET SDK is not on PATH. global.json pins 10.0.102 (rollForward latestFeature). Install .NET 10 from https://dotnet.microsoft.com/download, then re-run: bash scripts/init.sh"

SDK_VERSION="$(dotnet --version 2>/dev/null || true)"
echo "  dotnet SDK: ${SDK_VERSION:-unknown}"
case "$SDK_VERSION" in
  10.*) ;;
  *) die "expected a .NET 10 SDK (global.json pins 10.0.102, rollForward latestFeature); found '${SDK_VERSION:-none}'. Install .NET 10 and re-run." ;;
esac

DOCKER_OK=0
if command -v docker >/dev/null 2>&1 && docker info >/dev/null 2>&1; then
  echo "  docker: available (Cosmos emulator and E2E tests can run)"
  DOCKER_OK=1
else
  echo "  docker: NOT available — unit and contract tests still work; E2E tests will not."
fi

if command -v pwsh >/dev/null 2>&1; then
  echo "  pwsh: available"
else
  echo "  pwsh: not found — scripts/check-ai-bom-safety.ps1 and infra helpers will not run."
fi

if command -v python >/dev/null 2>&1; then
  echo "  python: available (agent post-edit hooks parse hook JSON with it)"
else
  echo "  python: not found — scripts/agent-post-edit.sh falls back to grep/sed parsing."
fi

if command -v gitleaks >/dev/null 2>&1; then
  echo "  gitleaks: available (pre-commit secret scan is active)"
else
  echo "  gitleaks: not found — the pre-commit secret scan will be SKIPPED."
  echo "            Install: https://github.com/gitleaks/gitleaks#installing"
fi

# --- 2. Git hooks -------------------------------------------------------------
step "Installing git hooks"

# The value must stay relative. The devcontainer bind-mounts the repo, so .git
# is shared with the Windows host; an absolute Linux path would break host git.
CURRENT_HOOKS_PATH="$(git config --get core.hooksPath || true)"
if [ "$CURRENT_HOOKS_PATH" = ".githooks" ]; then
  echo "  core.hooksPath already set to .githooks"
else
  git config core.hooksPath .githooks
  echo "  core.hooksPath set to .githooks"
fi

# Executable bits do not survive every Windows checkout.
chmod +x .githooks/* scripts/*.sh 2>/dev/null || true
echo "  hook and script executable bits set"

# --- 3. Harness-file manifest -------------------------------------------------
step "Checking harness manifest"

# Every document the routing map in AGENTS.md and docs/README.md links to. A
# missing entry here is a broken link an agent follows into nothing, so the
# check fails the bootstrap rather than degrading silently.
MANIFEST=(
  "AGENTS.md"
  "CLAUDE.md"
  ".github/copilot-instructions.md"
  "docs/README.md"
  "docs/product.md"
  "docs/architecture.md"
  "docs/ai-architecture.md"
  "docs/security.md"
  "docs/testing.md"
  "docs/development.md"
  "docs/infrastructure.md"
  "docs/harness-guide.md"
  "docs/quality-score.md"
  "docs/standards/commit-standards.md"
  "docs/standards/harness-governance.md"
)
MISSING=()
for f in "${MANIFEST[@]}"; do
  if [ -f "$f" ]; then
    echo "  ok       $f"
  else
    echo "  MISSING  $f"
    MISSING+=("$f")
  fi
done
if [ ${#MISSING[@]} -gt 0 ]; then
  die "harness file(s) missing: ${MISSING[*]}. Restore them from git (git checkout -- ${MISSING[*]}) before continuing."
fi

# --- 4. Restore all 14 services ----------------------------------------------
if [ "$DO_RESTORE" = "1" ]; then
  step "Restoring NuGet packages"
  FAILED_SERVICES=()
  for dir in src/*/; do
    svc="$(basename "$dir")"
    sln=""
    for candidate in "$dir"*.slnx "$dir"*.sln; do
      if [ -f "$candidate" ]; then sln="$candidate"; break; fi
    done
    [ -n "$sln" ] || continue
    printf '  %-32s' "$svc"
    if dotnet restore "$sln" --nologo -v:q >/dev/null 2>&1; then
      echo "ok"
    else
      echo "FAILED"
      FAILED_SERVICES+=("$svc")
    fi
  done
  if [ ${#FAILED_SERVICES[@]} -gt 0 ]; then
    die "restore failed for: ${FAILED_SERVICES[*]}. Run 'dotnet restore' in src/<service>/ to see the error."
  fi
fi

# --- 5. Optional: Cosmos DB emulator (bounded wait) ---------------------------
if [ "$DO_EMULATOR" = "1" ]; then
  step "Cosmos DB emulator"

  # In the devcontainer the emulator is a sibling compose service already
  # running; on the host it has to be started first.
  READY_URL="${COSMOS_EMULATOR_READY_URL:-}"
  if [ -z "$READY_URL" ]; then
    [ "$DOCKER_OK" = "1" ] || die "--emulator requires Docker, which is not available."
    echo "  starting cosmos-emulator via .devcontainer/docker-compose.yml"
    docker compose -f .devcontainer/docker-compose.yml up -d cosmos-emulator
    READY_URL="http://localhost:8080/ready"
  fi

  # Bounded, unlike the loop this replaces: 60 attempts x 5s = 5 minutes.
  MAX_ATTEMPTS=60
  attempt=1
  echo "  waiting for $READY_URL (max $((MAX_ATTEMPTS * 5))s)"
  until curl -f -s "$READY_URL" >/dev/null 2>&1; do
    if [ "$attempt" -ge "$MAX_ATTEMPTS" ]; then
      die "Cosmos DB emulator did not become ready at $READY_URL after $((MAX_ATTEMPTS * 5))s. Check 'docker compose -f .devcontainer/docker-compose.yml logs cosmos-emulator'."
    fi
    attempt=$((attempt + 1))
    sleep 5
  done
  echo "  emulator ready after $(( (attempt - 1) * 5 ))s"

  case "$(uname -s 2>/dev/null || echo unknown)" in
    MINGW*|MSYS*|CYGWIN*)
      echo "  NOTE: on Windows the emulator certificate must be trusted manually,"
      echo "        in an ELEVATED PowerShell:  .\\cosmos-emulator.ps1 cert"
      ;;
  esac
fi

step "Ready"
cat <<'EOF'
  Verify your edits:   bash scripts/verify.sh
  Verify one service:  bash scripts/verify.sh Biotrackr.Activity.Api
  Start the emulator:  bash scripts/init.sh --emulator
  Run the stack:       bash scripts/start-local.sh   (dev container only)
EOF
