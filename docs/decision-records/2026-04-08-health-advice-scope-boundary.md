<!-- markdownlint-disable-file -->

# Decision Record: Health Advice Scope Boundary with Source Grounding

- **Status**: Accepted
- **Deciders**: Will Velida
- **Date**: 08 April 2026
- **Related Docs**: [Health Advice Grounding Plan](.copilot-tracking/plans/2026-04-08/health-advice-grounding-plan.instructions.md), [Health Advice Grounding Research](.copilot-tracking/research/2026-04-08/health-advice-grounding-research.md)

## Context

The Biotrackr chat agent restricted responses to health data queries only, prohibiting all health, fitness, medical, and nutritional advice. The system prompt explicitly stated: "You are not a medical professional. Never provide diagnoses, treatment recommendations, or medication guidance" and "Only answer questions related to the user's health and fitness data."

As a single-user personal health platform, the owner requested expanding the agent to provide general health, fitness, and nutrition advice grounded in reputable Australian and international health sources, with provenance links to authoritative publications. This expansion needed to maintain ASI09 (Human-Agent Trust Exploitation) trust boundaries.

Three implementation approaches were evaluated: system prompt extension with embedded source registry, external source fetching via a new MCP tool, and a static knowledge base in Blob Storage with RAG-style search.

## Decision

Extend the system prompt with:

1. **Tiered response model** — 4 tiers: data presentation (no citation), general health information (citation required), personalised observations (data + guidelines with disclaimers), prohibited clinical advice (must refuse).
2. **Embedded source registry** — 13 curated Australian and international health sources categorised by domain (general health, nutrition, fitness, mental health, sleep) with base URLs and attribution requirements. All sources verified safe for linking.
3. **Mandatory citation requirements** — every Tier 2/3 response must cite at least one source with URL from the registry.
4. **Mental health crisis resources** — Beyond Blue, Black Dog Institute, Lifeline with phone numbers and URLs.
5. **Enhanced trust boundaries** — personalisation boundary (cannot combine user data with guidelines to make specific prescriptive recommendations), periodic trust reminders, strengthened disclaimers.

Selected Approach A (system prompt extension) because it introduces zero new attack surface, requires no new tools or infrastructure, and leverages the LLM's training knowledge of authoritative sources with registry URLs as provenance pointers.

## Consequences

- Agent can answer general health, fitness, and nutrition questions with cited authoritative sources.
- No new code changes, tools, infrastructure, or Azure resources required.
- Advice is grounded in LLM training knowledge (subject to training data cutoff), not real-time content.
- Risk of hallucinated citations mitigated by embedding specific base URLs in the source registry.
- ASI09 trust boundaries maintained: tiered model prevents clinical advice, disclaimers reinforced, UI banner updated.
- Copyright compliance verified for all 13 sources — all safe for linking as provenance references.
- Mayo Clinic removed from registry due to restrictive deep-linking clause; replaced with Australian alternatives.

## Alternatives Considered

1. **External Source Fetching (MCP Tool)**: New tool to fetch content from allowlisted URLs at query time. Rejected due to SSRF risk, content injection from external sites, Azure AI Search infrastructure cost, URL maintenance burden, and disproportionate complexity for a single-user platform.

2. **Static Knowledge Base (Blob Storage + RAG)**: Pre-curated guidelines in Blob Storage with search. Rejected due to copyright restrictions on content reproduction (most sources prohibit verbatim reuse), manual curation overhead, staleness risk, and infrastructure cost.

## Follow-up Actions

- Implement ContentSafetyMiddleware for programmatic health claim validation (pending MAF middleware API investigation for SSE interception).
- Design adversarial prompt injection test cases targeting Tier 4 refusal boundaries.
- Remediate ReportReviewerService fail-open vulnerability (pre-existing ASI09 MEDIUM, higher risk with expanded scope).
- Investigate custom citation evaluator for Azure AI Foundry evaluation pipeline.

## Notes

- Regulatory assessment: Biotrackr is NOT a medical device under FDA, EU MDR, or TGA frameworks (general wellness exclusion).
- All 15 investigated health sources are safe for the selected approach (providing URLs as provenance references).
- 15 new evaluation test cases added covering all 4 response tiers.
