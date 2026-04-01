<!-- markdownlint-disable-file -->

# Decision Record: Adopt Azure AI Foundry for GenAIOps Without Model Deployment

- **Status**: Accepted
- **Deciders**: Will Velida
- **Date**: 31 March 2026
- **Related Docs**: [Foundry GenAIOps Plan](.copilot-tracking/plans/2026-03-27/foundry-genaiops-plan.instructions.md), [Foundry GenAIOps Research](.copilot-tracking/research/2026-03-27/foundry-genaiops-without-model-deployment-research.md), [Evaluation API Research](.copilot-tracking/research/subagents/2026-03-31/azure-ai-projects-evaluation-api-research.md)

## Context

Biotrackr uses Claude Sonnet 4.6 (via Anthropic API) for two AI agents: BiotrackrChatAgent (conversational health data queries) and BiotrackrReportReviewer (report validation). The "Operationalize GenAI Apps" learning path recommended using Azure AI Foundry for evaluation, monitoring, and tracing. However, Anthropic models are not available in the Foundry Model Catalog for the subscription's region, so the standard Foundry deployment pattern (deploy model and use GPT judge evaluators) does not apply.

## Decision

Adopt Azure AI Foundry for GenAIOps without deploying any model in Foundry:

1. Deploy Foundry resource and project via Bicep (East US 2 for RAI evaluator support).
2. Add OpenTelemetry `gen_ai.*` semantic convention spans for Anthropic calls.
3. Build a dataset-based evaluation pipeline using the OpenAI-compatible evals REST API with safety and groundedness evaluators (no GPT judge model).
4. Defer custom agent registration (requires APIM v2 at approximately $175/month).

Key technical decisions:

- Use safety evaluators (violence, self-harm, sexual, hate_unfairness) and GroundednessProEvaluator, which run on the Content Safety backend with no GPT model dependency.
- Skip GPT-judge quality evaluators (coherence, fluency, relevance) because Chat.Api is a data-grounded agent where these would score near-perfect and fail to discriminate between good and bad outputs.
- Use the OpenAI-compatible REST API (`/openai/evals`) directly from C# since the .NET SDK v2.0.0-beta.2 has evaluations listed as a Known Issue.
- Deploy Foundry to East US 2 (not Australia East where other resources reside) because the RAI safety evaluation backend only supports 5 regions.
- Manage the blob storage connection out-of-band via CLI due to an ARM/data-plane category mismatch (ARM accepts `AzureBlob`, data plane expects `AzureBlobStorage`).

## Consequences

- All evaluation infrastructure is in IaC (Bicep) except the blob storage connection.
- Evaluations run automatically via GitHub Actions on prompt changes or workflow_dispatch.
- Safety evaluators provide meaningful protection against harmful content in health data responses.
- No ongoing model deployment costs in Foundry.
- Custom agent monitoring dashboard deferred until APIM v2 becomes cost-viable or Consumption SKU is supported.

## Alternatives Considered

1. **Deploy GPT model in Foundry for judge evaluators**: Rejected because Anthropic models are not available in the catalog, and GPT-judge quality evaluators add noise for this data-grounded agent.

2. **Use Microsoft.Extensions.AI.Evaluation**: Rejected because these are local/code-based evaluators, not Foundry-side safety evaluations.

3. **Use Python SDK for evaluations**: Considered as fallback. The Python SDK works reliably but adds a Python dependency to a .NET-only project. Direct REST from C# achieves the same result.

4. **Deploy Foundry in Australia East**: Rejected because the RAI safety evaluation backend does not support Australia East.

5. **Use APIM v2 for AI Gateway and custom agent registration**: Deferred due to cost (approximately $175/month for Basic v2). Consumption SKU is not compatible.

## Follow-up Actions

- Monitor `Azure.AI.Projects` v2.0.0-beta.3+ for evaluation SDK fixes and migrate from REST to SDK when stable.
- Monitor Foundry AI Gateway support for APIM Consumption SKU and re-evaluate Phase 4 when viable.
- Expand evaluation datasets beyond initial 10 records per file based on production trace sampling.
- Consider adding NLP evaluators (BLEU, ROUGE, F1) for data-faithfulness scoring alongside safety evaluators.

## Notes

- Research documents: `.copilot-tracking/research/2026-03-27/foundry-genaiops-without-model-deployment-research.md`, `.copilot-tracking/research/subagents/2026-03-31/azure-ai-projects-evaluation-api-research.md`
- Plan: `.copilot-tracking/plans/2026-03-27/foundry-genaiops-plan.instructions.md`
- PRs: #215 (OTel spans), #216 (evaluation pipeline), #217 (runtime skip fix), #218 (evaluation storage), #219 (connection category fix), #220 (OpenAI evals REST path), #221 (East US 2 migration)
