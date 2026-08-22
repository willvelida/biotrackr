#!/usr/bin/env bash
# Biotrackr verification sensor.
#
# Builds and unit-tests only the services affected by a set of changes. This is
# the single shared sensor: the git hooks, the agent post-edit hooks, and the
# agent itself all call this script.
#
# Usage:
#   bash scripts/verify.sh                        # services with working-tree changes
#   bash scripts/verify.sh Biotrackr.Activity.Api # one service, explicitly
#   bash scripts/verify.sh --staged               # services with staged changes
#   bash scripts/verify.sh --range origin/main..HEAD
#   bash scripts/verify.sh --build-only           # skip tests (fast agent loop)
#   bash scripts/verify.sh --contract             # also run contract tests
#
# Exit codes:
#   0  verification passed (or nothing to verify)
#   1  could not run — environment problem, not a code problem
#   2  verification failed — the code under test is broken

set -uo pipefail

REPO_ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
cd "$REPO_ROOT" || exit 1

MODE="working"
RANGE=""
BUILD_ONLY="${VERIFY_BUILD_ONLY:-0}"
CONTRACT=0
SERVICES=()

while [ $# -gt 0 ]; do
  case "$1" in
    --staged)     MODE="staged" ;;
    --range)
      MODE="range"
      RANGE="${2:-}"
      if [ -z "$RANGE" ]; then
        echo "VERIFY CANNOT RUN: --range requires a revision range, e.g. --range origin/main..HEAD" >&2
        exit 1
      fi
      shift
      ;;
    --build-only) BUILD_ONLY=1 ;;
    --contract)   CONTRACT=1 ;;
    -h|--help)    sed -n '2,25p' "${BASH_SOURCE[0]}" | sed 's/^# \{0,1\}//'; exit 0 ;;
    --*)
      echo "VERIFY CANNOT RUN: unknown flag '$1'. Run 'bash scripts/verify.sh --help'." >&2
      exit 1
      ;;
    *)            SERVICES+=("$1"); MODE="explicit" ;;
  esac
  shift
done

fail_env() {
  echo "VERIFY CANNOT RUN: $1" >&2
  exit 1
}

command -v dotnet >/dev/null 2>&1 || fail_env \
  "the .NET SDK is not on PATH. global.json pins 10.0.102 (rollForward latestFeature). Install .NET 10 from https://dotnet.microsoft.com/download, then re-run: bash scripts/verify.sh"

# --- Resolve the affected service set ----------------------------------------

changed_paths() {
  case "$MODE" in
    staged)  git diff --cached --name-only --diff-filter=ACMR ;;
    range)   git diff --name-only --diff-filter=ACMR "$RANGE" ;;
    # A brand-new file is untracked, so git diff cannot see it. Without the
    # second command a new service class reports "nothing to verify".
    working) git diff --name-only --diff-filter=ACMR HEAD
             git ls-files --others --exclude-standard ;;
  esac
}

if [ "$MODE" != "explicit" ]; then
  PATHS="$(changed_paths 2>/dev/null)"

  # Root build config affects all 14 services; verifying them all is not a
  # sensible local gate. Say so explicitly and let CI cover it.
  if printf '%s\n' "$PATHS" | grep -qE '^(Directory\.Build\.props|global\.json)$'; then
    echo "verify: root build config changed (Directory.Build.props / global.json)."
    echo "verify: that affects all 14 services — skipping local verification, CI covers it."
    echo "verify: to verify anyway, name services explicitly: bash scripts/verify.sh Biotrackr.Activity.Api"
    exit 0
  fi

  while IFS= read -r svc; do
    [ -n "$svc" ] && SERVICES+=("$svc")
  done < <(printf '%s\n' "$PATHS" | grep -E '^src/[^/]+/' | cut -d/ -f2 | sort -u)
fi

if [ ${#SERVICES[@]} -eq 0 ]; then
  echo "verify: no service changes detected; nothing to verify."
  exit 0
fi

# --- Verify each service ------------------------------------------------------

FAILED=()

for svc in "${SERVICES[@]}"; do
  dir="src/$svc"
  if [ ! -d "$dir" ]; then
    # A named service that does not exist is a caller mistake, not a no-op.
    # A derived one can legitimately vanish (a deleted service directory).
    if [ "$MODE" = "explicit" ]; then
      fail_env "no such service '$svc' (expected $dir). Available: $(ls -1 src/ | tr '\n' ' ')"
    fi
    echo "verify: skipping '$svc' — no such directory ($dir)."
    continue
  fi

  # Two service directories break the Biotrackr.<Domain>.<Type> shape, so the
  # solution file is resolved by globbing the directory, never by rebuilding
  # the name from parts. 5 of 14 use .slnx, the rest .sln.
  sln=""
  for candidate in "$dir"/*.slnx "$dir"/*.sln; do
    if [ -f "$candidate" ]; then sln="$candidate"; break; fi
  done
  [ -n "$sln" ] || fail_env \
    "no .sln or .slnx found in $dir. Expected exactly one solution file per service."

  echo "verify: $svc"

  build_log="$(mktemp)"
  if ! dotnet build "$sln" --no-restore -v:q --nologo >"$build_log" 2>&1; then
    # Packages may have changed since the last restore; retry once with restore
    # so a stale obj/ does not masquerade as a compile error.
    if ! dotnet build "$sln" -v:q --nologo >"$build_log" 2>&1; then
      {
        echo ""
        echo "VERIFY FAILED: $svc (build)"
        echo "  what:    the solution did not compile"
        echo "  command: dotnet build $sln --no-restore -v:q"
        echo "  errors:"
        grep -E 'error [A-Z]+[0-9]+|: error' "$build_log" | head -40 | sed 's/^/    /'
        echo "  next:    fix the compile errors above, then re-run:"
        echo "           bash scripts/verify.sh $svc"
      } >&2
      FAILED+=("$svc:build")
      rm -f "$build_log"
      continue
    fi
  fi
  rm -f "$build_log"

  if [ "$BUILD_ONLY" = "1" ]; then
    continue
  fi

  unit="$dir/$svc.UnitTests/$svc.UnitTests.csproj"
  if [ -f "$unit" ]; then
    test_log="$(mktemp)"
    # No -v:q here: quiet verbosity discards the assertion messages, leaving the
    # agent with a bare pass/fail count and nothing to act on.
    if ! dotnet test "$unit" --no-build --nologo >"$test_log" 2>&1; then
      {
        echo ""
        echo "VERIFY FAILED: $svc (unit tests)"
        echo "  what:    one or more unit tests failed"
        echo "  command: dotnet test $unit --no-build"
        echo "  failures:"
        grep -E '(^|[[:space:]])(Failed |Error Message|Stack Trace|Assert\.|Expected|Actual|Failed!)' "$test_log" \
          | head -40 | sed 's/^[[:space:]]*/    /'
        echo "  next:    fix the failing tests or the code they cover, then re-run:"
        echo "           bash scripts/verify.sh $svc"
      } >&2
      FAILED+=("$svc:unit")
    fi
    rm -f "$test_log"
  fi

  if [ "$CONTRACT" = "1" ]; then
    integ="$dir/$svc.IntegrationTests/$svc.IntegrationTests.csproj"
    # Biotrackr.UI has no integration test project; absence is not a failure.
    if [ -f "$integ" ]; then
      test_log="$(mktemp)"
      if ! dotnet test "$integ" --no-build --nologo \
             --filter "FullyQualifiedName~Contract" >"$test_log" 2>&1; then
        {
          echo ""
          echo "VERIFY FAILED: $svc (contract tests)"
          echo "  what:    service startup or DI registration is broken"
          echo "  command: dotnet test $integ --no-build --filter \"FullyQualifiedName~Contract\""
          echo "  failures:"
          grep -E '(^|[[:space:]])(Failed |Error Message|Stack Trace|Assert\.|Expected|Actual|Failed!)' "$test_log" \
            | head -40 | sed 's/^[[:space:]]*/    /'
          echo "  next:    check DI registrations and service startup in $dir, then re-run:"
          echo "           bash scripts/verify.sh --contract $svc"
        } >&2
        FAILED+=("$svc:contract")
      fi
      rm -f "$test_log"
    fi
  fi
done

if [ ${#FAILED[@]} -gt 0 ]; then
  {
    echo ""
    echo "VERIFY FAILED (${#FAILED[@]}): ${FAILED[*]}"
    echo "  reproduce everything above with: bash scripts/verify.sh ${SERVICES[*]}"
  } >&2
  exit 2
fi

echo "verify: OK (${SERVICES[*]})"
exit 0
