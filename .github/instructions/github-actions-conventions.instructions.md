---
description: "GitHub Actions workflow conventions for Biotrackr CI/CD pipelines. Use when: writing or editing GitHub Actions workflow files, reusable templates, or composite actions."
applyTo: "**/*.yml"
---

# GitHub Actions Conventions

## Action Pinning

- Pin third-party actions to full commit SHA (e.g., `actions/checkout@b4ffde65f46336ab88eb53be808477a3936bae11`)
- Pin internal reusable templates to `@main` (e.g., `.github/workflows/template-build.yml@main`)
- Never use `@latest` or floating tags on third-party actions

## Permissions

- Default to `contents: read` at workflow level
- Override permissions only at job level, not step level
- Add `id-token: write` only for jobs requiring OIDC authentication
- Never grant `write` permissions at workflow level unless every job needs it

## Concurrency

- Require `concurrency` group on deployment workflows to prevent parallel deploys
- Use pattern: `group: deploy-{service}-${{ github.ref }}` with `cancel-in-progress: false`
- CI-only workflows may use `cancel-in-progress: true` to save runner minutes

## Path Filters

- Require `paths:` filter on `pull_request` triggers scoped to the service directory
- Include shared infrastructure paths when relevant (e.g., `infra/apps/{service}/**`)
- Always include the workflow file itself in paths to catch self-modifications

## OIDC Authentication

- Use federated identity credentials over long-lived secrets for Azure access
- Authenticate with `azure/login` action using OIDC (`client-id`, `tenant-id`, `subscription-id`)
- Never store Azure credentials as repository secrets when OIDC is available

## Reusable Templates

- Store reusable workflow templates in `.github/workflows/` with `template-` prefix
- Call templates with `uses: ./.github/workflows/template-{name}.yml`
- Pass service-specific values via `with:` inputs, not hardcoded in templates

## Secret Handling

- Never echo or print secrets in workflow steps
- Use `GITHUB_TOKEN` over Personal Access Tokens (PATs)
- Mask outputs containing sensitive data with `::add-mask::`
- Access secrets only via `${{ secrets.NAME }}` — never pass as command-line arguments

## Environment Naming

- Use `dev` and `prod` environments
- Require reviewers on `prod` environment deployments
- Deploy to `dev` automatically on PR merge to `main`
