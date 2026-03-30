---
applyTo: '.copilot-tracking/changes/2026-03-27/foundry-genaiops-changes.md'
---
<!-- markdownlint-disable-file -->
# Implementation Plan: Azure AI Foundry GenAIOps Integration (Without Model Deployment)

## Overview

Integrate Azure AI Foundry for evaluation, monitoring, and tracing of Biotrackr's Claude-powered agents without deploying an inference model in Foundry, using a Tier 2 approach with a small GPT judge model for AI-assisted quality evaluators.

## Objectives

### User Requirements

* Use Foundry for prompt versioning, monitoring, evaluation, and tracing as outlined in the Operationalize GenAI Apps learning path — Source: user conversation
  * Note: Prompt versioning is already satisfied by the existing Git workflow (`scripts/chat-system-prompt/`, `scripts/reporting-api-prompts/`). Microsoft's recommendation is Git-based versioning; Prompt Flow is deprecated (classic-only). No Foundry implementation step needed for this requirement.
* Achieve this without deploying a production model within Foundry, since Anthropic models are not available in the subscription's Foundry Model Catalog — Source: user conversation
* Continue using Claude Sonnet 4.6 via direct Anthropic API as the production model — Source: user conversation

### Derived Objectives

* Deploy a Foundry resource and project via Bicep with no production model (only a GPT-4.1-mini judge for evaluation) — Derived from: research finding that AI-assisted quality evaluators require a GPT judge model, and evaluators evaluate data not endpoints
* Add `gen_ai.*` OpenTelemetry semantic convention spans to Chat.Api for Anthropic calls — Derived from: research finding that Foundry trace correlation and monitoring dashboard require gen_ai semantic conventions
* Create a dataset-based evaluation pipeline using the .NET `Azure.AI.Projects` `EvaluationClient` — Derived from: research finding that .NET SDK supports dataset-only evaluation with JSONL
* Register Chat.Api as a custom agent in Foundry Control Plane for production monitoring — Derived from: research finding that Foundry explicitly supports external agent monitoring via custom agent registration
* Build a GitHub Actions evaluation workflow following the existing template-driven CI/CD pattern — Derived from: codebase convention research showing 9 reusable workflow templates and no evaluation workflow

## Context Summary

### Project Files

* src/Biotrackr.Chat.Api/Program.cs - OpenTelemetry setup with Azure Monitor exporters (traces, metrics, logs)
* src/Biotrackr.Chat.Api/Biotrackr.Chat.Api/Configuration/Settings.cs - Configuration model bound to `Biotrackr:` App Config section
* src/Biotrackr.Chat.Api/Biotrackr.Chat.Api/Services/ChatAgentProvider.cs - AnthropicClient construction and AsAIAgent() call
* src/Biotrackr.Chat.Api/Biotrackr.Chat.Api/Services/ReportReviewerService.cs - Second Claude agent for report review
* src/Biotrackr.Mcp.Server/ - Existing ActivitySource/Activity usage for OTel spans in BaseTool
* infra/core/main.bicep - Core infrastructure (Log Analytics, App Insights, Container App Env, UAI, ACR, KV, App Config, Budget, Cosmos, APIM)
* infra/modules/monitoring/app-insights.bicep - Application Insights module (resource name: appins-biotrackr-dev)
* infra/apps/chat-api/main.bicep - Chat.Api deployment Bicep, passes App Insights connection string as env var
* scripts/chat-system-prompt/system-prompt.txt - Git-versioned system prompt (already follows Foundry recommended pattern)
* .github/workflows/deploy-chat-api.yml - Chat.Api CI/CD pipeline (tests → build → Bicep → deploy → E2E)

### References

* .copilot-tracking/research/2026-03-27/foundry-genaiops-without-model-deployment-research.md - Primary research document
* .copilot-tracking/research/subagents/2026-03-27/foundry-genaiops-modules-research.md - Module-by-module learning path analysis
* .copilot-tracking/research/subagents/2026-03-27/foundry-evaluation-monitoring-external-models-research.md - Evaluation SDK external model compatibility
* .copilot-tracking/research/subagents/2026-03-27/foundry-project-without-model-research.md - Foundry project architecture, cost, evaluator requirements
* .copilot-tracking/research/subagents/2026-03-27/foundry-dotnet-integration-research.md - .NET SDK capabilities, OTel semantic conventions
* .copilot-tracking/research/subagents/2026-03-27/foundry-prompt-mgmt-eval-dataset-research.md - Dataset-only evaluation, custom agent registration
* .copilot-tracking/research/subagents/2026-03-27/foundry-integration-codebase-conventions.md - Codebase conventions, infrastructure, workflows
* .copilot-tracking/research/subagents/2026-03-27/biotrackr-current-ai-telemetry-setup-research.md - Current AI and telemetry setup

### Standards References

* docs/standards/commit-standards.md — Commit message conventions
* docs/decision-records/decision-record-template.md — Decision record format for new ADRs

## Implementation Checklist

### [ ] Implementation Phase 1: Foundry Infrastructure (Bicep)

<!-- parallelizable: false -->

* [ ] Step 1.1: Create Foundry Bicep module (`infra/modules/ai/foundry.bicep`)
  * Details: .copilot-tracking/details/2026-03-27/foundry-genaiops-details.md (Lines 15-64)
* [ ] Step 1.2: Create GPT judge model deployment Bicep module (`infra/modules/ai/foundry-model-deployment.bicep`)
  * Details: .copilot-tracking/details/2026-03-27/foundry-genaiops-details.md (Lines 66-104)
* [ ] Step 1.3: Wire Foundry modules into `infra/core/main.bicep`
  * Details: .copilot-tracking/details/2026-03-27/foundry-genaiops-details.md (Lines 106-140)
* [ ] Step 1.4: Add RBAC role assignments for managed identity on Foundry resource
  * Details: .copilot-tracking/details/2026-03-27/foundry-genaiops-details.md (Lines 142-172)
* [ ] Step 1.5: Add Foundry project endpoint to App Configuration
  * Details: .copilot-tracking/details/2026-03-27/foundry-genaiops-details.md (Lines 174-194)
* [ ] Step 1.6: Validate Bicep with lint, validate, and what-if
  * Run `az bicep lint` on new modules
  * Run `az deployment group validate` with dev parameters
  * Skip if validation conflicts with parallel phases

### [ ] Implementation Phase 2: OpenTelemetry gen_ai Semantic Conventions

<!-- parallelizable: true -->

* [ ] Step 2.1: Create `AnthropicTelemetry` instrumentation class in Chat.Api
  * Details: .copilot-tracking/details/2026-03-27/foundry-genaiops-details.md (Lines 168-233)
* [ ] Step 2.2: Integrate instrumentation into ChatAgentProvider and ReportReviewerService
  * Details: .copilot-tracking/details/2026-03-27/foundry-genaiops-details.md (Lines 235-275)
* [ ] Step 2.3: Register `gen_ai.anthropic` ActivitySource with OpenTelemetry in Program.cs
  * Details: .copilot-tracking/details/2026-03-27/foundry-genaiops-details.md (Lines 277-305)
* [ ] Step 2.4: Validate phase changes
  * Run `dotnet build` and `dotnet test` for Chat.Api
  * Verify traces emit gen_ai.* attributes in local console exporter

### [ ] Implementation Phase 3: Evaluation Pipeline

<!-- parallelizable: true -->

* [ ] Step 3.1: Add `Azure.AI.Projects` NuGet package to Chat.Api test project
  * Details: .copilot-tracking/details/2026-03-27/foundry-genaiops-details.md (Lines 311-332)
* [ ] Step 3.2: Create evaluation dataset JSONL files for Chat.Api scenarios
  * Details: .copilot-tracking/details/2026-03-27/foundry-genaiops-details.md (Lines 334-377)
* [ ] Step 3.3: Build evaluation runner class using .NET `EvaluationClient`
  * Details: .copilot-tracking/details/2026-03-27/foundry-genaiops-details.md (Lines 379-443)
* [ ] Step 3.4: Create GitHub Actions evaluation workflow (`evaluation.yml`)
  * Details: .copilot-tracking/details/2026-03-27/foundry-genaiops-details.md (Lines 445-493)
* [ ] Step 3.5: Validate phase changes
  * Run evaluation locally against Foundry project
  * Verify results appear in Foundry portal

### [ ] Implementation Phase 4: Custom Agent Registration and Monitoring

<!-- parallelizable: false -->

Depends on Phase 1 (Foundry infrastructure deployed) and Phase 2 (gen_ai spans emitting to App Insights).

* [ ] Step 4.1: Deploy Foundry infrastructure to dev environment
  * Details: .copilot-tracking/details/2026-03-27/foundry-genaiops-details.md (Lines 499-520)
* [ ] Step 4.2: Register Chat.Api as custom agent in Foundry Control Plane
  * Details: .copilot-tracking/details/2026-03-27/foundry-genaiops-details.md (Lines 522-552)
* [ ] Step 4.3: Configure continuous evaluation rules on monitoring dashboard
  * Details: .copilot-tracking/details/2026-03-27/foundry-genaiops-details.md (Lines 554-580)
* [ ] Step 4.4: Validate monitoring dashboard shows traces and metrics
  * Verify token usage, latency, and error rate charts populate
  * Verify continuous evaluation triggers on sampled responses

### [ ] Implementation Phase 5: Decision Record and Documentation

<!-- parallelizable: true -->

* [ ] Step 5.1: Create ADR for Foundry GenAIOps adoption strategy
  * Details: .copilot-tracking/details/2026-03-27/foundry-genaiops-details.md (Lines 586-618)
* [ ] Step 5.2: Update infrastructure documentation
  * Details: .copilot-tracking/details/2026-03-27/foundry-genaiops-details.md (Lines 620-640)

### [ ] Implementation Phase 6: Validation

<!-- parallelizable: false -->

* [ ] Step 6.1: Run full project validation
  * Execute `dotnet build` across all affected projects
  * Execute `dotnet test` with coverage gates
  * Run Bicep linter on all modified infrastructure files
  * Run GitHub Actions workflow dry-run (`workflow_dispatch` or act)
* [ ] Step 6.2: Fix minor validation issues
  * Iterate on lint errors and build warnings
  * Apply fixes directly when corrections are straightforward
* [ ] Step 6.3: Report blocking issues
  * Document issues requiring additional research
  * Provide user with next steps and recommended planning
  * Avoid large-scale fixes within this phase

## Planning Log

See .copilot-tracking/plans/logs/2026-03-27/foundry-genaiops-log.md for discrepancy tracking, implementation paths considered, and suggested follow-on work.

## Dependencies

* `Azure.AI.Projects` NuGet package (latest prerelease) — .NET evaluation SDK
* `Azure.Monitor.OpenTelemetry.Exporter` (existing, v1.4.0) — Azure Monitor trace export
* `OpenTelemetry.Extensions.Hosting` (existing, v1.12.0) — OTel hosting extensions
* Azure subscription with Foundry resource creation permissions
* Azure OpenAI quota for GPT-4.1-mini in `australiaeast` region (or nearest supported region)
* Foundry project endpoint (output of Phase 1 Bicep deployment)
* AI Gateway / API Management (existing `apim-biotrackr-dev` or Foundry-managed)

## Success Criteria

* Foundry resource and project deploy via Bicep with zero production model deployments — Traces to: user requirement (no Foundry model deployment)
* Chat.Api emits `gen_ai.*` OpenTelemetry spans with Anthropic-specific semantic conventions to Application Insights — Traces to: research finding on Foundry trace correlation requirements
* Evaluation pipeline runs against pre-computed Claude JSONL datasets using both safety evaluators (no judge) and quality evaluators (GPT judge) — Traces to: user requirement (evaluation)
* Chat.Api registered as custom agent in Foundry with monitoring dashboard showing latency, token usage, and error rates — Traces to: user requirement (monitoring)
* GitHub Actions evaluation workflow runs on PR and produces evaluation results viewable in Foundry portal — Traces to: learning path module 3 (automated evaluations)
* Decision record documents the Tier 2 adoption approach and alternatives considered — Traces to: project convention (docs/decision-records/)
