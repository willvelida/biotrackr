# 01 Known Vulnerabilities

Identifier: OSS-RISK-1:2025
Category: Dependency Security

## Description

A component version may contain vulnerable code, accidentally introduced by its developers.
Vulnerability details are publicly disclosed, for example through CVE, GitHub Security Advisories
or other, more informal communication channels. Exploits and patches may or may not be available.

The vulnerability may be exploitable in the context of the downstream software, which could
compromise the confidentiality, integrity or availability of the respective system or its data,
allow lateral movements in the target environment or have other negative effects.

## Risk

- Exploitation of known vulnerabilities in dependencies can compromise confidentiality, integrity
  or availability of the downstream system and its data.
- Lateral movement within the target environment becomes possible through exploited dependencies.
- Publicly disclosed vulnerabilities with available exploit code increase the likelihood of
  opportunistic and targeted attacks.
- Transitive dependencies with known vulnerabilities may go unnoticed without proper monitoring,
  expanding the attack surface silently.
- Delayed patching of known vulnerabilities extends the window of exposure.

## Vulnerability checklist

- Direct or transitive open source dependencies contain publicly disclosed vulnerabilities (CVEs).
- No monitoring is in place for vulnerability disclosures affecting project dependencies.
- Vulnerability prioritization does not consider exploit maturity, CVSS scores, or EPSS data.
- DAST and SAST tools are not used to determine whether vulnerable code is reachable in the
  dependent software context.
- Dependencies are not tracked against threat intelligence feeds such as the CISA KEV catalog.
- No process exists to evaluate and act on newly disclosed vulnerabilities in a timely manner.

## Prevention controls

1. Monitor applications, containers and systems for the presence of direct and transitive open
   source dependencies with known vulnerabilities.
2. Prioritize analysis and mitigation using scores and metrics such as CVSS and EPSS.
3. Incorporate threat intelligence including exploit code availability and maturity, and attacks
   observed in the wild such as those tracked by the CISA KEV catalog.
4. Use DAST and SAST tools to determine if vulnerable code can be executed in the context of the
   dependent software.
5. Establish a vulnerability response process with defined SLAs for triage, assessment, and
   remediation based on severity.
6. Maintain a current software bill of materials (SBOM) so that newly disclosed vulnerabilities
   can be quickly correlated to deployed components.

## Example attack scenarios

### Scenario A — Unpatched Struts dependency
An organization uses a version of Apache Struts affected by CVE-2017-5638.
The vulnerability is publicly known and exploit code is widely available.
An attacker sends a crafted Content-Type header to the application, achieving remote code
execution on the server.
Sensitive customer data is exfiltrated, resulting in a major data breach.

### Scenario B — Transitive Log4j exposure
A project does not directly depend on Apache Log4j, but includes it transitively through another
library.
When CVE-2021-44228 (Log4Shell) is disclosed, the development team is unaware of the transitive
dependency.
An attacker exploits the vulnerability through user-controlled input that reaches the logging
subsystem, downloading and executing arbitrary code on the server.

## Detection guidance

- Run software composition analysis (SCA) tools against project manifests and lock files to
  identify dependencies with known CVEs.
- Cross-reference discovered vulnerabilities against the CISA KEV catalog for active exploitation
  status.
- Review CVSS and EPSS scores to gauge severity and likelihood of exploitation.
- Use reachability analysis to determine whether vulnerable code paths are exercised by the
  downstream application.
- Monitor GitHub Security Advisories and NVD feeds for new disclosures affecting project
  dependencies.

## Remediation

- Update affected dependencies to patched versions that resolve the disclosed vulnerability.
- Where a patch is not yet available, evaluate and apply workarounds or mitigating controls
  recommended by the vulnerability advisory.
- Remove or replace dependencies that are no longer maintained and will not receive patches.
- Re-run SCA and reachability analysis after remediation to confirm the vulnerability is resolved.
- Document the vulnerability response including timeline, decisions, and residual risk acceptance.
