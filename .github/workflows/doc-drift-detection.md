---
on:
  schedule: weekly
  workflow_dispatch:
engine:
  id: copilot
permissions:
  contents: read
safe-outputs:
  create-issue:
    title-prefix: "[doc-drift] "
    labels: [documentation, automated]
    close-older-issues: true
    max: 1
timeout-minutes: 15
---

# Documentation Drift Detection

Analyze the Biotrackr repository for documentation that has drifted from the actual codebase.

## Checks

1. **Service inventory**: Compare service directories in `src/` against the service tables in `AGENTS.md` and `.github/copilot-instructions.md`. Flag services that exist in code but not in docs, or vice versa.

2. **Workflow inventory**: Compare `.github/workflows/deploy-*.yml` files against the Service Reference table in `.github/copilot-instructions.md`.

3. **Infrastructure inventory**: Compare `infra/apps/` directories against documented services.

4. **Agent/Skill/Prompt inventory**: Verify the counts in documentation match actual files in `.github/agents/`, `.github/skills/`, `.github/instructions/`, and `.github/prompts/`.

5. **Architecture overview**: Check that the service count (currently "14 independently-deployable services") in AGENTS.md and copilot-instructions.md matches the actual `src/Biotrackr.*` directory count.

## Output

If drift is found, create an issue listing each discrepancy with:
- What is documented vs. what exists
- Specific file paths that need updating
- Suggested corrections

If no drift is detected, call `noop` with a confirmation message.
