# GitHub Actions Workflow Templates

This project implements the use of GitHub Actions workflow templates (as best as I can). Workflow templates helps create reusable templates within the scope of this monorepo. For more information on workflow templates, check out the [documentation](https://docs.github.com/en/actions/writing-workflows/using-workflow-templates).

## Conventions

For every template, a suffix of `template-{purpose-of-template}.yml` has been applied. For example:

```
- .github
  - workflows
    - template-bicep-deploy.yml
    - template-bicep-linter.yml
    - template-bicep-validate.yml
    - template-bicep-whatif.yml
```

To implement the workflow template within a workflow, you can do so like this:

```yaml
jobs:
    lint:
        name: Run Bicep Linter
        uses: willvelida/biotrackr/.github/workflows/template-bicep-linter.yml@main
        with:
          template-file: './infra/core/main.bicep'
```

**Wherever possible, you should target the main branch for your workflow template**. If you are making changes to the workflow templates and wish to test them out as part of your branch, change the target to your feature branch, and commit the template change to your feature branch.

Once you have merged the PR, you should raise another PR to target the main branch once your changes have been accepted into main.