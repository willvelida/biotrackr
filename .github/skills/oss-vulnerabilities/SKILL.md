---
name: oss-vulnerabilities
description: OWASP Open Source Software Top 10 vulnerability knowledge base for identifying, assessing, and remediating security risks in open source software dependencies.
license: MIT
metadata:
  authors: "OWASP Open Source Software Top 10 Project"
  spec_version: "1.0"
  framework_revision: "1.0.0"
  last_updated: "2026-02-16"
  skill_based_on: "https://github.com/chris-buckley/agnostic-prompt-standard"
  content_based_on: "https://owasp.org/www-project-open-source-software-top-10/"
---

# OSS Vulnerabilities — Skill Entry

This `SKILL.md` is the **entrypoint** for the OSS Vulnerabilities skill.

The skill encodes the **OWASP Open Source Software Top 10** as structured, machine-readable references
that an agent can query to identify, assess, and remediate risks associated with open source software dependencies.

## Normative references (OSS Top 10)

1. [00 Vulnerability Index](references/00-vulnerability-index.md)
2. [01 Known Vulnerabilities](references/01-known-vulnerabilities.md)
3. [02 Compromise of Legitimate Package](references/02-compromise-legitimate-package.md)
4. [03 Name Confusion Attacks](references/03-name-confusion-attacks.md)
5. [04 Unmaintained Software](references/04-unmaintained-software.md)
6. [05 Outdated Software](references/05-outdated-software.md)
7. [06 Untracked Dependencies](references/06-untracked-dependencies.md)
8. [07 License and Regulatory Risk](references/07-license-regulatory-risk.md)
9. [08 Immature Software](references/08-immature-software.md)
10. [09 Unapproved Change](references/09-unapproved-change.md)
11. [10 Under/Over-sized Dependency](references/10-underoversized-dependency.md)

## Skill layout

- `SKILL.md` — this file (skill entrypoint).
- `references/` — the OSS Top 10 normative documents.
  - `00-vulnerability-index.md` — master index of all vulnerability identifiers, categories, and cross-references.
  - `01` through `10` — one document per vulnerability aligned with OWASP OSS Risk numbering.
- `assets/` — reusable format and constants blocks.
  - `constants/` — vulnerability catalog and category definitions.
    - `constants-oss-catalog-v1.0.0.md`
  - `formats/` — output contract examples.
    - `format-vulnerability-assessment-v1.0.0.md`
    - `format-remediation-checklist-v1.0.0.md`
