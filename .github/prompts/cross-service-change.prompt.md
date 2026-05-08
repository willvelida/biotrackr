---
description: "Coordinate changes across multiple Biotrackr services with per-service validation"
agent: "C# Expert"
argument-hint: "Change description and affected services (e.g., 'Add TTL field to all document models in Activity, Food, Sleep')"
---

Coordinate a change that spans multiple Biotrackr services, validating each service independently before moving to the next.

## Workflow

### 1. Identify Affected Services

Review the change request and identify all affected services from the Biotrackr architecture:

| Service | Directory |
|---------|-----------|
| Activity API | `src/Biotrackr.Activity.Api` |
| Activity Svc | `src/Biotrackr.Activity.Svc` |
| Auth Svc | `src/Biotrackr.Auth.Svc` |
| Chat API | `src/Biotrackr.Chat.Api` |
| Food API | `src/Biotrackr.Food.Api` |
| Food Svc | `src/Biotrackr.Food.Svc` |
| MCP Server | `src/Biotrackr.Mcp.Server` |
| Reporting API | `src/Biotrackr.Reporting.Api` |
| Reporting Svc | `src/Biotrackr.Reporting.Svc` |
| Sleep API | `src/Biotrackr.Sleep.Api` |
| Sleep Svc | `src/Biotrackr.Sleep.Svc` |
| UI | `src/Biotrackr.UI` |
| Vitals API | `src/Biotrackr.Vitals.Api` |
| Vitals Svc | `src/Biotrackr.Vitals.Svc` |

List the specific files to modify in each service.

### 2. Create Execution Plan

Produce an ordered list of per-service changes. Identify dependencies between services and determine the correct order of operations.

### 3. Per-Service Loop

For each affected service, execute the following sequence:

#### 3a. Implement Changes

Apply the planned modifications for this service.

#### 3b. Build Check (deterministic)

```bash
cd src/Biotrackr.{Domain}.{Type} && dotnet build --no-restore -v:q
```

#### 3c. Test Check (deterministic)

```bash
dotnet test --no-build
```

#### 3d. Fix Issues (bounded retry)

If build or tests fail, diagnose and fix. **Maximum 2 retries per service.**

**STOP** if a service fails after 2 retries — report all service statuses before proceeding.

### 4. Cross-Service Consistency Check

After all services pass individually, verify consistency across the modified services:

- Same patterns and naming conventions used in every service
- Same approach applied uniformly (no service-specific divergence without justification)
- No cross-service contract breakages

### 5. Escalation

**STOP** if any service cannot pass build and test checks after 2 retries. Present:

- Per-service status (pass/fail)
- Exact errors for failing services
- What was attempted
- Assessment of the root cause

## Deliverables

- Consistent changes applied across all affected services
- Per-service build and test verification passing
- Summary of changes per service
