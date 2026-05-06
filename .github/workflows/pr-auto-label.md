---
on:
  pull_request:
    types: [opened, synchronize]
engine:
  id: copilot
permissions:
  contents: read
  pull-requests: read
rate-limit:
  max: 5
  window: 60
safe-outputs:
  add-labels:
    allowed: [activity, auth, chat, food, mcp, reporting, sleep, ui, vitals, infrastructure, documentation, testing, ai-agent, security, dependencies]
    max: 5
timeout-minutes: 10
---

# PR Auto-Labeling

Analyze the pull request's changed files and add appropriate labels.

## Labeling Rules

- Changes to `src/Biotrackr.Activity.*/**` → `activity`
- Changes to `src/Biotrackr.Auth.*/**` → `auth`
- Changes to `src/Biotrackr.Chat.*/**` → `chat`, `ai-agent`
- Changes to `src/Biotrackr.Food.*/**` → `food`
- Changes to `src/Biotrackr.Mcp.*/**` → `mcp`, `ai-agent`
- Changes to `src/Biotrackr.Reporting.*/**` → `reporting`, `ai-agent`
- Changes to `src/Biotrackr.Sleep.*/**` → `sleep`
- Changes to `src/Biotrackr.UI/**` → `ui`
- Changes to `src/Biotrackr.Vitals.*/**` → `vitals`
- Changes to `infra/**` or `.github/workflows/**` → `infrastructure`
- Changes to `docs/**` or `*.md` (excluding `.copilot-tracking/`) → `documentation`
- Changes to `**/*Tests*/**` → `testing`
- Changes to `SECURITY.md` or files containing `vulnerability` → `security`

Do not remove existing labels. Only add new labels. If no labels apply, call `noop`.
