---
title: AI Transparency Statement
description: Public transparency document describing how Biotrackr uses AI components, data handling practices, safety controls, and ethical considerations
ms.date: 2026-04-16
---

# AI Transparency Statement

> Biotrackr AI System Disclosure — Public Transparency Document

**Last Updated:** April 2026

<!-- AI-BOM safety check validates this file on every PR -->

---

## Table of Contents

* [System Overview](#system-overview)
* [AI Models](#ai-models)
* [Frameworks and Protocols](#frameworks-and-protocols)
* [Data Categories Processed by AI](#data-categories-processed-by-ai)
* [Safety and Security](#safety-and-security)
* [Human Oversight](#human-oversight)
* [Ethical Considerations](#ethical-considerations)
* [Health Data and AI Disclaimer](#health-data-and-ai-disclaimer)
* [Data Privacy](#data-privacy)
* [Known Limitations](#known-limitations)
* [AI Dependencies](#ai-dependencies)
* [Licensing](#licensing)
* [Responsible AI Contact](#responsible-ai-contact)
* [Technical Documentation](#technical-documentation)

---

## System Overview

Biotrackr is a personal health and fitness tracking platform that integrates with Fitbit and Withings wearable devices to collect, store, and analyze health data across four domains: physical activity, sleep, nutrition, and vitals.

The platform includes AI-powered features for:

* **Conversational health data querying** — Users interact with an AI assistant to ask natural language questions about their health data (e.g., "How did I sleep last week?" or "Show my step trends this month").
* **Automated health report generation** — The AI generates PDF reports with charts and analysis summarizing health trends over configurable time periods.
* **Independent report validation** — A separate AI reviewer validates generated reports against source data before delivery to the user.

AI features are supplementary to the platform's core functionality. All health data is accessible through the dashboard without AI involvement. AI capabilities can be disabled independently of the rest of the platform.

## AI Models

| Attribute | Details |
|-----------|---------|
| **Provider** | Anthropic |
| **Model Family** | Claude |
| **Access Method** | API-consumed (cloud-hosted by provider) |
| **Self-hosted** | No — models are not deployed or fine-tuned locally |

### How Models Are Used

* **Conversational Agent:** Interprets user questions about health data, determines which data to retrieve, and formulates natural language responses.
* **Report Generation:** Generates analysis narratives and code to produce charts and PDF reports from health data.
* **Report Review:** An independent model instance reviews generated reports for accuracy, consistency with source data, and appropriate disclaimers.

Biotrackr does not fine-tune, retrain, or modify the underlying models. All AI interactions use the provider's standard API.

## Frameworks and Protocols

Biotrackr uses the following AI and agent frameworks:

| Framework / Protocol | Purpose |
|---------------------|---------|
| **Microsoft Agent Framework** | Agent orchestration — manages AI agent lifecycle, tool integration, and session management |
| **Model Context Protocol (MCP)** | Standardized tool integration — exposes health data query capabilities as structured tools for AI consumption |
| **AGUI Protocol** | Streaming conversational responses — delivers real-time AI responses to the user interface |
| **Agent-to-Agent (A2A) Protocol** | Inter-service agent communication — enables structured communication between AI services |
| **GitHub Copilot SDK** | Code generation for report building — generates data analysis and visualization code for health reports |

## Data Categories Processed by AI

The following categories of personal health data may be processed by AI components:

| Category | Examples |
|----------|----------|
| **Physical Activity** | Daily steps, distance walked/run, calories burned, active minutes, exercise sessions |
| **Sleep** | Sleep duration, sleep stages (light, deep, REM), sleep efficiency, time asleep/awake |
| **Nutrition** | Daily calorie intake, macronutrient breakdown (protein, carbohydrates, fat), food log entries |
| **Vitals** | Body weight, body composition (muscle mass, bone mass, water mass, fat mass, fat-free mass), visceral fat index, blood pressure |

### Data Flow

* Health data originates from Fitbit and Withings devices and is stored in the user's own database.
* When a user asks a question or requests a report, relevant data is retrieved from the database and sent to the AI provider's API for processing.
* AI responses are streamed back to the user in real time.
* Conversation history is stored in the user's database and can be deleted by the user at any time.

> **Important:** All data categories listed above are considered **personal and sensitive health data**. See [Data Privacy](#data-privacy) for how this data is handled.

## Safety and Security

Biotrackr's AI safety approach is aligned with the **OWASP Agentic Security Top 10** (ASI01–ASI10), a comprehensive framework for securing AI agent systems. The following safety categories are addressed:

| Safety Category | Description |
|----------------|-------------|
| **Prompt Injection Defense** | Protections against attempts to manipulate AI behavior through crafted inputs |
| **Tool Access Policies** | Controls governing which tools the AI can invoke and usage budgets per session |
| **Identity Verification** | Authentication and authorization for inter-service AI communication |
| **Code Execution Validation** | Automated scanning of AI-generated code before execution |
| **Conversation Safeguards** | Limits on conversation length and content to prevent context manipulation |
| **Independent Report Review** | Separate AI reviewer validates report accuracy against source data |
| **Graceful Degradation** | Controlled fallback behavior when AI services are unavailable |
| **Kill Switch** | Administrative ability to disable AI features without affecting core platform functionality |
| **Supply Chain Security** | Dependency scanning and version management for AI-related packages |

> **Note:** Specific implementation details, thresholds, and configurations for these safety controls are intentionally omitted from this public document. Security auditors and compliance reviewers may request the full technical AI Bill of Materials (AI-BOM) — see [Technical Documentation](#technical-documentation).

For reporting security vulnerabilities, see [SECURITY.md](SECURITY.md).

## Human Oversight

Biotrackr incorporates multiple layers of human oversight over AI behavior:

* **User-initiated interactions:** All AI conversations are initiated by the user. The AI does not autonomously reach out, collect data, or take actions without a user request.
* **Transparent tool usage:** The user interface displays which data tools the AI invoked during a conversation, providing visibility into the AI's reasoning process.
* **Report review pipeline:** All AI-generated reports pass through an independent AI reviewer before delivery. If concerns are identified, they are flagged to the user alongside the report.
* **Conversation management:** Users can view, export, and permanently delete their conversation history at any time.
* **Feature control:** AI report generation can be administratively disabled via a configuration flag without affecting the rest of the platform.
* **Mandatory disclaimers:** All AI-generated content carries clear disclaimers about its nature and limitations.

## Ethical Considerations

### Intended Use

Biotrackr's AI features are designed for **personal health awareness and trend exploration** — helping users understand patterns in their own health data through natural language conversation and visual reports.

### Not Intended For

* **Medical diagnosis, treatment, or prevention** of any health condition
* **Clinical decision support** for healthcare providers
* **Emergency health monitoring** or real-time alerting
* **Comparison with population health data** or normative benchmarks
* **Predictions about future health outcomes** beyond simple trend observation

### Bias and Fairness

* The AI models are provided by Anthropic and may carry biases present in their training data. Biotrackr does not fine-tune or modify these models.
* Health data interpretation may not account for individual medical conditions, medications, or physiological differences.
* Report analysis is based solely on the data available from connected devices and may not represent a complete picture of the user's health.

### Transparency Commitment

This document is part of Biotrackr's commitment to transparent AI practices. The project maintains:

* This public AI Transparency Statement
* A detailed internal AI Bill of Materials (AI-BOM) available to security auditors
* Alignment with OWASP Agentic Security standards
* Open-source codebase for community review

## Health Data and AI Disclaimer

> **This platform does not provide medical advice.**
>
> AI-generated insights, reports, charts, and conversational responses about your health data are for **informational and personal awareness purposes only**. They are not intended to be a substitute for professional medical advice, diagnosis, or treatment.
>
> **Always seek the advice of your physician or other qualified health provider** with any questions you may have regarding a medical condition. Never disregard professional medical advice or delay in seeking it because of something generated by this platform's AI features.
>
> AI-generated content may contain inaccuracies. While reports undergo automated review for consistency with source data, the AI may misinterpret trends, draw incorrect conclusions, or present data in misleading ways. **Always verify AI-generated insights against your raw health data** available in the platform dashboard.
>
> If you think you may have a medical emergency, call your doctor or emergency services immediately.

## Data Privacy

### Health Data Sent to External AI Providers

When you use Biotrackr's AI features (conversational queries or report generation), relevant portions of your health data are transmitted to Anthropic's API for processing. The following applies:

* **Data transmitted:** Only the health data relevant to your specific query or report request is sent. Bulk data exports are not transmitted.
* **Provider data usage:** Per Anthropic's commercial terms of service, data sent through the API is not used to train or improve their models.
* **Encryption:** All data transmitted to the AI provider is encrypted in transit.
* **Account identifiers not sent:** The platform does not intentionally send your name, email, device identifiers, or account credentials to the AI provider. However, health metrics with dates and timestamps are personal data, and user-provided chat messages are forwarded without redaction — users should avoid including sensitive personal information in their queries.
* **Local storage:** Your conversation history and generated reports are stored in your own database instance. They are not stored by the AI provider beyond the duration needed to process your request.

### Your Controls

* You can use all platform features (dashboard, data viewing) without using AI features.
* You can delete individual conversations or your entire conversation history.
* AI features can be administratively disabled.
* Generated reports are stored in your own storage and can be deleted.

## Known Limitations

| Limitation | Description |
|-----------|-------------|
| **External AI dependency** | AI features require connectivity to Anthropic's API. If the provider experiences downtime, AI features will be temporarily unavailable while core platform functionality continues normally. |
| **Data completeness** | AI analysis is limited to data collected by connected Fitbit and Withings devices. Gaps in device wear, syncing issues, or unsupported metrics may lead to incomplete analysis. |
| **Temporal context** | The AI's conversational context is limited per session. Very long conversations may lose earlier context, potentially affecting response quality. |
| **Report accuracy** | While reports undergo automated review, AI-generated charts and narratives may occasionally misrepresent data trends. Users should verify against raw data. |
| **Language support** | The conversational AI primarily supports English. Responses in other languages may have reduced quality. |
| **No real-time data** | The AI works with synced historical data. It does not have access to real-time sensor readings from wearable devices. |
| **Single-user design** | The platform is designed for individual personal use. It does not support multi-user comparisons or household health tracking. |

## AI Dependencies

### AI-Related NuGet Packages (.NET)

| Package | Major Version | Purpose |
|---------|--------------|---------|
| Microsoft.Agents.AI | 1.x | Agent framework for AI orchestration |
| Microsoft.Extensions.AI | 10.x | AI service abstractions and integration |
| ModelContextProtocol | 1.x | MCP client and server implementation |
| GitHub.Copilot.SDK | 0.x | Copilot integration for report code generation |

### Python Dependencies (Report Generation)

| Package | Purpose |
|---------|---------|
| pandas | Health data analysis and transformation |
| matplotlib | Chart and visualization generation |
| seaborn | Statistical visualization |
| reportlab | PDF report generation |
| numpy | Numerical computation |

### Infrastructure

| Component | Purpose |
|-----------|---------|
| .NET 10 / C# 14 | Application runtime |
| Azure Container Apps | Service hosting |
| Azure Cosmos DB | Health data and conversation storage |
| Azure API Management | API gateway and access control |
| Azure Key Vault | Secure configuration storage |

## Licensing

Biotrackr is licensed under the **Apache License 2.0**. See [LICENSE](LICENSE) for full terms.

The AI models consumed via API are subject to Anthropic's terms of service and usage policies.

## Responsible AI Contact

For questions, concerns, or feedback about Biotrackr's AI features and this transparency statement:

* **GitHub Issues:** Open an issue in this repository with the label `responsible-ai`
* **Security Vulnerabilities:** See [SECURITY.md](SECURITY.md) for responsible disclosure

## Technical Documentation

This public transparency statement provides a high-level overview of Biotrackr's AI system. A comprehensive **AI Bill of Materials (AI-BOM)** is maintained internally with full technical details including:

* Complete dependency inventory with exact versions
* Detailed data flow diagrams
* Security control specifications
* Model configuration parameters
* Tool schemas and integration details
* Evaluation results and safety test outcomes

The full technical AI-BOM is **available on request** for:

* Security auditors conducting assessments
* Compliance reviewers evaluating data handling practices
* Regulatory bodies with appropriate jurisdiction

To request access, open a GitHub issue with the label `ai-bom-request` or contact the repository maintainers.
