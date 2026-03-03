# Decision Record: Make tenantId Injection Conditional in Bicep Workflow Templates

- **Status**: Accepted
- **Deciders**: Will Velida
- **Date**: 04 March 2026
- **Related Docs**: [Issue #158](https://github.com/willvelida/biotrackr/issues/158), [2025-11-12-apim-named-values-for-jwt-config.md](2025-11-12-apim-named-values-for-jwt-config.md)

## Context

The reusable workflow templates `template-bicep-validate.yml`, `template-bicep-whatif.yml`, and `template-bicep-deploy.yml` unconditionally injected a `tenantId` parameter into the Bicep deployment parameters via a "Prepare Parameters with Tenant ID" step. However, only 5 of the 12 Bicep app templates (`activity-api`, `food-api`, `weight-api`, `sleep-api`, `mcp-server`) actually declare `param tenantId string`. The remaining 7 templates (all `*-service` apps, `ui`, and `core`) do not use this parameter.

This caused Bicep validation to fail with error **BCP259** ("The parameter 'tenantId' is assigned in the params file without being declared in the Bicep file") for any workflow targeting a template that doesn't declare `tenantId`.

## Decision

Add a boolean `inject-tenant-id` input (default: `false`) to all three reusable Bicep workflow templates. When `true`, the template merges `tenantId` from the `tenant-id` secret into the deployment parameters. When `false` (default), the template passes through the caller's parameters unchanged.

The 5 API/MCP deploy workflows that need `tenantId` (for APIM named values / JWT configuration) explicitly set `inject-tenant-id: true`. All other deploy workflows use the default `false` and require no changes.

## Consequences

- Bicep validation, what-if, and deploy operations now succeed for all templates regardless of whether they declare a `tenantId` parameter.
- The `tenantId` injection is opt-in rather than implicit, making the template behaviour more predictable.
- Adding a new Bicep template that needs `tenantId` requires the caller to explicitly set `inject-tenant-id: true`, which is a clear and discoverable pattern.

## Alternatives Considered

1. **Add `param tenantId string` to all Bicep templates** — Rejected because it introduces unused parameters in templates that have no use for `tenantId`, which is misleading and violates the principle of minimal surface area.

2. **Remove injection entirely; callers pass `tenantId` via `parameters` JSON** — Rejected because it would require callers to embed the tenant ID secret directly in the `parameters` input, which the centralised "Prepare Parameters" step was designed to avoid.

3. **Auto-detect whether the Bicep file declares `tenantId` at workflow runtime** — Rejected as overly complex; it would require parsing Bicep files in the workflow, adding fragility and maintenance overhead for marginal benefit.

## Follow-up Actions

- None required. All affected workflows are updated in this change.

## Notes

The `tenantId` parameter is used by Bicep templates that configure APIM named values for JWT authentication (see decision record `2025-11-12-apim-named-values-for-jwt-config.md`).
