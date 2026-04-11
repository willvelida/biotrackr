---
name: ml-vulnerabilities
description: OWASP Machine Learning Top 10 vulnerability knowledge base for identifying, assessing, and remediating security risks in machine learning systems.
license: MIT
metadata:
  authors: "OWASP Machine Learning Security Project"
  spec_version: "1.0"
  framework_revision: "1.0.0"
  last_updated: "2026-02-16"
  skill_based_on: "https://github.com/chris-buckley/agnostic-prompt-standard"
  content_based_on: "https://owasp.org/www-project-machine-learning-security-top-10/"
---

# ML Vulnerabilities — Skill Entry

This `SKILL.md` is the **entrypoint** for the ML Vulnerabilities skill.

The skill encodes the **OWASP Machine Learning Security Top 10** as structured, machine-readable references
that an agent can query to identify, assess, and remediate machine learning security risks.

## Normative references (ML Top 10)

1. [00 Vulnerability Index](references/00-vulnerability-index.md)
2. [01 Input Manipulation Attack](references/01-input-manipulation-attack.md)
3. [02 Data Poisoning Attack](references/02-data-poisoning-attack.md)
4. [03 Model Inversion Attack](references/03-model-inversion-attack.md)
5. [04 Membership Inference Attack](references/04-membership-inference-attack.md)
6. [05 Model Theft](references/05-model-theft.md)
7. [06 AI Supply Chain Attacks](references/06-ai-supply-chain-attacks.md)
8. [07 Transfer Learning Attack](references/07-transfer-learning-attack.md)
9. [08 Model Skewing](references/08-model-skewing.md)
10. [09 Output Integrity Attack](references/09-output-integrity-attack.md)
11. [10 Model Poisoning](references/10-model-poisoning.md)

## Skill layout

- `SKILL.md` — this file (skill entrypoint).
- `references/` — the ML Top 10 normative documents.
  - `00-vulnerability-index.md` — master index of all vulnerability identifiers, categories, and cross-references.
  - `01` through `10` — one document per vulnerability aligned with OWASP ML Security Top 10 numbering.
- `assets/` — reusable format and constants blocks.
  - `constants/` — vulnerability catalog and category definitions.
    - `constants-ml-catalog-v1.0.0.md`
  - `formats/` — output contract examples.
    - `format-vulnerability-assessment-v1.0.0.md`
    - `format-remediation-checklist-v1.0.0.md`
