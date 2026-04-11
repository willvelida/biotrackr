---
name: serverless-vulnerabilities
description: OWASP Serverless Top 10 vulnerability knowledge base for identifying, assessing, and remediating security risks in serverless application environments.
license: MIT
metadata:
  authors: "OWASP Serverless Security Project"
  spec_version: "1.0"
  framework_revision: "1.0.0"
  last_updated: "2026-02-13"
  skill_based_on: "https://github.com/chris-buckley/agnostic-prompt-standard"
  content_based_on: "https://owasp.org/www-project-serverless-top-10/"
---

# Serverless Vulnerabilities — Skill Entry

This `SKILL.md` is the **entrypoint** for the Serverless Vulnerabilities skill.

The skill encodes the **OWASP Top 10 Serverless Interpretation** as structured, machine-readable
references that an agent can query to identify, assess, and remediate serverless application
security risks.

## Normative references (Serverless Top 10)

1. [00 Vulnerability Index](references/00-vulnerability-index.md)
2. [01 Injection](references/01-injection.md)
3. [02 Broken Authentication](references/02-broken-authentication.md)
4. [03 Sensitive Data Exposure](references/03-sensitive-data-exposure.md)
5. [04 XML External Entities](references/04-xml-external-entities.md)
6. [05 Broken Access Control](references/05-broken-access-control.md)
7. [06 Security Misconfiguration](references/06-security-misconfiguration.md)
8. [07 Cross-Site Scripting](references/07-cross-site-scripting.md)
9. [08 Insecure Deserialization](references/08-insecure-deserialization.md)
10. [09 Using Components with Known Vulnerabilities](references/09-using-components-with-known-vulnerabilities.md)
11. [10 Insufficient Logging and Monitoring](references/10-insufficient-logging-and-monitoring.md)

## Skill layout

- `SKILL.md` — this file (skill entrypoint).
- `references/` — the Serverless Top 10 normative documents.
  - `00-vulnerability-index.md` — master index of all vulnerability identifiers, categories, and cross-references.
  - `01` through `10` — one document per vulnerability aligned with OWASP Top 10 2017 numbering.
- `assets/` — reusable format and constants blocks.
  - `constants/` — vulnerability catalog and category definitions.
    - `constants-serverless-catalog-v1.0.0.md`
  - `formats/` — output contract examples.
    - `format-vulnerability-assessment-v1.0.0.md`
    - `format-remediation-checklist-v1.0.0.md`
