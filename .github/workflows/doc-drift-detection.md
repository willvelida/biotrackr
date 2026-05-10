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

5. **Architecture overview**: Check that the service count (currently "14 independently-deployable services") in `AGENTS.md` and `.github/copilot-instructions.md` matches the actual `src/Biotrackr.*` directory count.

6. **Workflow category counts**: Verify the workflow counts in the repository structure tree ("N workflows + N reusable templates + N agentic workflows") in both `AGENTS.md` and `.github/copilot-instructions.md` match actual files in `.github/workflows/`: regular `.yml` files (excluding `*.lock.yml` and `template-*.yml`), `template-*.yml` templates, and `*.lock.yml` agentic workflow locks paired with `.md` prompt bodies.

7. **Section heading counts**: Verify the documented section counts for `.github/copilot-instructions.md` ("N sections") and `AGENTS.md` ("N sections") in the repository structure tree of both files, and the `AGENTS.md` section count in the Cross-References section of `.github/copilot-instructions.md`, match the actual count of `##` headings in each file.

8. **Bicep module count**: Verify the "N reusable Bicep modules" count in the repository structure tree of both `AGENTS.md` and `.github/copilot-instructions.md` matches the actual number of `.bicep` files across `infra/modules/` subdirectories, and the Module Domains table row count in `.github/copilot-instructions.md` matches the number of `infra/modules/` subdirectories.

## Output

If drift is found, create an issue listing each discrepancy with:
- What is documented vs. what exists
- Specific file paths that need updating
- Suggested corrections

If no drift is detected, call `noop` with a confirmation message.
