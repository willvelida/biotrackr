---
title: Security Policy
description: Security vulnerability reporting policy for the Biotrackr project
ms.date: 2026-04-16
---

# Security Policy

## Supported Versions

Biotrackr follows a rolling release model. Only the latest deployed version receives security updates.

| Branch | Supported |
|--------|-----------|
| `main` | Yes |
| Feature branches | No |
| Previous releases | No |

## Reporting a Vulnerability

**Please do not open public GitHub issues for security vulnerabilities.**

### Preferred: GitHub Private Security Advisory

Report vulnerabilities through [GitHub Private Security Advisories](https://github.com/willvelida/biotrackr/security/advisories/new). This is the preferred method because it:

* Keeps the report confidential until a fix is available.
* Allows collaborative triage between reporter and maintainer.
* Generates a CVE identifier when appropriate.
* Does not require PGP keys or special tooling.

### Alternative: Email

If you are unable to use GitHub Security Advisories, email **willvelida [at] hotmail [dot] co [dot] uk** with the subject line `[SECURITY] Biotrackr — <brief description>`.

Include as much of the following as possible:

* Description of the vulnerability
* Steps to reproduce or proof of concept
* Affected component(s) and version(s)
* Potential impact assessment
* Any suggested remediation

## Response Timeline

| Action | Timeframe |
|--------|-----------|
| Acknowledgment of report | Within 72 hours |
| Initial assessment and triage | Within 7 days |
| Status update to reporter | At least every 14 days |
| Fix development and testing | Best effort, varies by severity |
| Coordinated disclosure | 90 days from report, negotiable |

For critical vulnerabilities affecting deployed services, the maintainer will prioritize an expedited fix.

## What Constitutes a Security Issue

The following are considered security issues and should be reported privately:

* **Data exposure:** Unauthorized access to personal health data (activity, sleep, food, vitals, weight)
* **Authentication/authorization bypass:** Circumventing Azure API Management JWT validation, managed identity controls, or subscription key requirements
* **Secrets exposure:** Leakage of API keys, Azure Key Vault secrets, managed identity credentials, or connection strings
* **Injection attacks:** SQL injection, NoSQL injection (Cosmos DB), prompt injection against AI components, or command injection
* **AI/Agent security:** Exploitation of AI agent components including prompt injection, tool misuse, agent goal hijacking, or unauthorized code execution (see OWASP Agentic Security ASI01-ASI10)
* **MCP Server exploitation:** Unauthorized tool invocation, rate limit bypass, or data exfiltration through MCP tools
* **Supply chain attacks:** Compromised dependencies, container image tampering, or CI/CD pipeline manipulation
* **Infrastructure misconfiguration:** Azure resource misconfigurations that could lead to unauthorized access

## Out of Scope

The following are NOT security vulnerabilities and should be reported as regular GitHub issues:

* Denial of service against your own local development environment
* Social engineering attacks against contributors
* Issues in third-party dependencies with existing CVEs (use Dependabot alerts instead)
* Feature requests for additional security controls
* Questions about the security architecture (open a Discussion instead)
* Requests for AI Bill of Materials (AI-BOM) data (use the `ai-bom-request` issue label)
* Vulnerabilities in services Biotrackr integrates with (Fitbit API, Withings API, Anthropic API) — report those to the respective vendors

## AI-Specific Security Concerns

Biotrackr includes AI components powered by Claude (Anthropic) via the Microsoft Agent Framework. The project implements OWASP Agentic Security controls (ASI01-ASI10). AI-specific security concerns include:

* **Prompt injection** (ASI01): Attempts to manipulate the AI agent's behavior through crafted inputs
* **Tool misuse** (ASI02): Exploitation of MCP tools beyond intended functionality
* **Identity abuse** (ASI03): Compromise of agent identity tokens used for inter-service authentication
* **Context poisoning** (ASI06): Manipulation of conversation history or cached data to influence AI behavior
* **Unauthorized code execution** (ASI05): Bypassing code validation gates in the Reporting API

For transparency about AI components used in this project, see [AI-TRANSPARENCY.md](AI-TRANSPARENCY.md).

## Disclosure Policy

* The maintainer follows coordinated disclosure with a default 90-day timeline.
* Security fixes are released as soon as practical after a fix is developed and tested.
* Security advisories are published via GitHub Security Advisories after fixes are deployed.
* Credit is given to reporters in the advisory unless anonymity is requested.

## Security Controls

This project implements the following security measures:

* **Azure API Management** with JWT validation on all external endpoints
* **Azure Key Vault** for secrets and system prompt storage
* **User-assigned managed identity** for Azure service access (no stored credentials)
* **Dependabot** for automated dependency updates
* **CodeQL** scanning for static analysis
* **Trivy** container vulnerability scanning in CI/CD
* **Dockle** container best-practice linting
* **SBOM generation** (CycloneDX) for all container images
* **OWASP Agentic Security** controls (ASI01-ASI10) for AI components
