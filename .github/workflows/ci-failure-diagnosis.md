---
on:
  workflow_run:
    types: [completed]
    workflows:
      - Deploy Activity Api
      - Deploy Activity Service
      - Deploy Auth Fitbit Service
      - Deploy Auth Withings Service
      - Deploy Chat Api
      - Deploy Core Biotrackr Infrastructure
      - Deploy Food Api
      - Deploy Food Service
      - Deploy MCP Server
      - Deploy Reporting Api
      - Deploy Reporting Service (Monthly)
      - Deploy Reporting Service (Weekly)
      - Deploy Reporting Service (Yearly)
      - Deploy Sleep Api
      - Deploy Sleep Service
      - Deploy UI
      - Deploy Vitals Api
      - Deploy Vitals Service
    branches: [main]
engine:
  id: copilot
permissions:
  contents: read
  actions: read
  pull-requests: read
  issues: read
rate-limit:
  max: 5
  window: 60
safe-outputs:
  add-comment:
    max: 1
tools:
  github:
    toolsets: [default, actions]
timeout-minutes: 15
---

# CI Failure Diagnosis

When a workflow run fails, analyze the failure and provide a diagnostic comment on the associated pull request.

## Instructions

1. Check if the completed workflow run **failed** (conclusion is not "success" or "skipped")
2. If the workflow succeeded, call `noop` — only diagnose failures
3. Check if the failed workflow run is associated with a pull request
4. If not associated with a PR, call `noop` — only diagnose PR-related failures
5. Read the workflow run logs, focusing on failed jobs and steps
6. Identify the root cause category:
   - **Unit test failure**: Which test(s) failed? What assertion failed?
   - **Contract test failure**: DI registration issue? Missing service?
   - **Build failure**: Compilation error? Missing package?
   - **Bicep validation failure**: Template error? Parameter mismatch?
   - **E2E test failure**: Cosmos Emulator issue? API contract change?
   - **Coverage failure**: Below 70% threshold? Which files need tests?
   - **Container build failure**: Dockerfile issue? Base image problem?
   - **ACR push failure**: Authentication? Registry connectivity?
7. Post a comment on the PR with:
   - Failure category and affected stage (of the 10-stage pipeline)
   - Root cause analysis with specific error messages
   - Suggested fix or next steps
   - Link to the failed workflow run

## Important

- Be specific: include file names, test names, and error messages from logs
- Do not suggest fixes you are not confident about — say "investigate further" when uncertain
- The 10-stage pipeline is: env-setup → unit-tests → contract-tests → build-image → retrieve-image → lint → validate → preview → deploy-dev → e2e-tests
