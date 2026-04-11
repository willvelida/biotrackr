# 02 Compromise of Legitimate Package

Identifier: OSS-RISK-2:2025
Category: Supply Chain Security

## Description

Attackers may compromise resources that are part of an existing legitimate project or of the
distribution infrastructure in order to inject malicious code into a component. This can occur
through hijacking the accounts of legitimate project maintainers or exploiting vulnerabilities
in package repositories.

Malicious code can be executed on end-user systems or on systems belonging to the organization
that develops and operates the dependent software, such as build systems or developer
workstations. The confidentiality, integrity and availability of systems and the data processed
or stored thereon is at risk.

## Risk

- Malicious code injected into a trusted package executes with the same trust level as the
  legitimate component, bypassing many security controls.
- Build systems and developer workstations become compromised, enabling supply chain pivots to
  production environments.
- End-user systems that consume the compromised package are exposed to data exfiltration,
  credential theft, or backdoor installation.
- Account takeover of a maintainer can go undetected for extended periods, allowing sustained
  malicious modifications.
- Distribution infrastructure compromise can affect all consumers who download from the affected
  repository.

## Vulnerability checklist

- Components are consumed directly from public repositories without provenance verification.
- No process exists to verify component signatures or attestations according to SLSA.
- Components are not built from source by the organization or a trusted third party.
- Code review (manual or automated) is not performed on dependency updates.
- No secured internal artifact store is used to mirror and validate external components.
- Maintainer account security posture of upstream projects is not evaluated.

## Prevention controls

1. Verify component provenance according to SLSA (Supply chain Levels for Software Artifacts).
2. Build components from source, either internally or through a trusted third party.
3. Review dependency code manually and automatically, especially for pre/post installation hooks
   and obfuscated payloads.
4. Retrieve all components from a secured internal store that hosts home-made binaries and mirrors
   external components after validation.
5. Monitor for unexpected changes in package behavior, such as new network connections, filesystem
   access, or process execution introduced between versions.
6. Subscribe to security advisories for upstream projects and distribution platforms.

## Example attack scenarios

### Scenario A — Maintainer account takeover
An attacker compromises the npm account of a legitimate maintainer of a widely-used package.
The attacker publishes a new patch version containing code that exfiltrates cryptocurrency wallet
credentials from downstream consumers.
The malicious version is automatically pulled in by projects using compatible version ranges in
their dependency manifests.

### Scenario B — Build infrastructure compromise
An attacker exploits a vulnerability in a project's CI/CD pipeline, similar to the SolarWinds
attack. Malicious code is injected into the build artifacts during the compilation and packaging
step. The signed, published artifacts contain the backdoor, and downstream consumers trust the
package based on its legitimate origin and valid signature.

## Detection guidance

- Compare package checksums and signatures against known-good values and provenance attestations.
- Monitor dependency updates for unexpected behavioral changes using sandboxed installation
  analysis.
- Review pre-install and post-install scripts in package manifests for suspicious operations.
- Track maintainer account changes and ownership transfers on upstream package registries.
- Use binary analysis or reproducible build verification to confirm that published artifacts match
  their declared source code.

## Remediation

- Pin dependencies to verified versions with known-good checksums or signatures.
- Revoke and rotate any credentials that may have been exposed through the compromised package.
- Remove the compromised package version and replace with a verified clean version or alternative.
- Audit build systems and developer workstations for indicators of compromise resulting from the
  malicious package.
- Report the compromised package to the relevant package registry for takedown.
- Review and strengthen internal artifact validation processes to prevent recurrence.
