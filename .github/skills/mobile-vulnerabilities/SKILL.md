---
name: mobile-vulnerabilities
description: OWASP Mobile Top 10 vulnerability knowledge base for identifying, assessing, and remediating security risks in mobile application environments.
license: MIT
metadata:
  authors: "OWASP Mobile Security Project"
  spec_version: "1.0"
  framework_revision: "1.0.0"
  last_updated: "2026-02-16"
  skill_based_on: "https://github.com/chris-buckley/agnostic-prompt-standard"
  content_based_on: "https://owasp.org/www-project-mobile-top-10/"
---

# Mobile Vulnerabilities — Skill Entry

This `SKILL.md` is the **entrypoint** for the Mobile Vulnerabilities skill.

The skill encodes the **OWASP Mobile Top 10** as structured, machine-readable references
that an agent can query to identify, assess, and remediate mobile application security risks.

## Normative references (Mobile Top 10)

1. [00 Vulnerability Index](references/00-vulnerability-index.md)
2. [01 Improper Credential Usage](references/01-improper-credential-usage.md)
3. [02 Inadequate Supply Chain Security](references/02-inadequate-supply-chain-security.md)
4. [03 Insecure Authentication and Authorization](references/03-insecure-authentication-authorization.md)
5. [04 Insufficient Input/Output Validation](references/04-insufficient-input-output-validation.md)
6. [05 Insecure Communication](references/05-insecure-communication.md)
7. [06 Inadequate Privacy Controls](references/06-inadequate-privacy-controls.md)
8. [07 Insufficient Binary Protection](references/07-insufficient-binary-protection.md)
9. [08 Security Misconfiguration](references/08-security-misconfiguration.md)
10. [09 Insecure Data Storage](references/09-insecure-data-storage.md)
11. [10 Insufficient Cryptography](references/10-insufficient-cryptography.md)

## Skill layout

- `SKILL.md` — this file (skill entrypoint).
- `references/` — the Mobile Top 10 normative documents.
  - `00-vulnerability-index.md` — master index of all vulnerability identifiers, categories, and cross-references.
  - `01` through `10` — one document per vulnerability aligned with OWASP Mobile Top 10 numbering.
- `assets/` — reusable format and constants blocks.
  - `constants/` — vulnerability catalog and category definitions.
    - `constants-mobile-catalog-v1.0.0.md`
  - `formats/` — output contract examples.
    - `format-vulnerability-assessment-v1.0.0.md`
    - `format-remediation-checklist-v1.0.0.md`
