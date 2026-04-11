# 03 Name Confusion Attacks

Identifier: OSS-RISK-3:2025
Category: Supply Chain Security

## Description

Attackers may create components whose names resemble names of legitimate open-source or system
components (typo-squatting), suggest trustworthy authors (brand-jacking) or play with common
naming patterns in different languages or ecosystems.

Malicious code can be executed on end-user systems or on systems belonging to the organization
that develops and operates the dependent software, such as build systems or developer
workstations. The confidentiality, integrity and availability of systems and the data processed
or stored thereon is at risk.

## Risk

- Developers accidentally install a malicious package instead of the intended legitimate one
  due to a similar name.
- Typo-squatting attacks exploit common misspellings, making detection difficult without
  deliberate verification.
- Brand-jacking leverages trust in well-known authors or organizations to trick consumers into
  adopting malicious packages.
- Malicious packages can execute arbitrary code during installation, compromising build systems
  and developer workstations before any application code runs.
- Naming pattern exploits across ecosystems target developers working in multiple languages who
  may assume consistent naming conventions.

## Vulnerability checklist

- Dependencies are added by manual name entry without verifying the package against its official
  source repository or author.
- No pre-installation review of package code characteristics, such as pre/post installation hooks
  or encoded payloads, is performed.
- Project characteristics like source code repository, maintainer accounts, release frequency, and
  number of downstream users are not checked before adoption.
- Component metadata from package repositories is trusted without independent verification.
- Component signatures from trusted parties are not verified in ecosystems that support or require
  them.
- No internal policy or tooling exists to approve new dependencies before they enter the project.

## Prevention controls

1. Check code characteristics before installing a component, including pre/post installation hooks,
   encoded payloads, and obfuscated code.
2. Verify project characteristics including source code repository, maintainer accounts, release
   frequency, and number of downstream users as leading risk indicators.
3. Verify that the component carries a signature from a trusted party for ecosystems that support
   or require signatures.
4. Use an internal allowlist of approved packages and namespaces to prevent accidental installation
   of similarly-named malicious packages.
5. Cross-reference package names against known-good registries and official project documentation
   before adoption.
6. Note that component metadata on package repositories is not always verified and can be forged
   by attackers; rely on multiple signals rather than metadata alone.

## Example attack scenarios

### Scenario A — Typo-squatting on PyPI
An attacker publishes a package named "colourama" on PyPI, resembling the popular "colorama"
package. The malicious package contains a cryptocurrency clipboard hijacker. Developers who
mistype the package name during installation unknowingly install the attacker's package, which
redirects Bitcoin transfers to an attacker-controlled wallet.

### Scenario B — Brand-jacking a popular namespace
An attacker creates packages under a namespace that mimics a well-known organization. The
packages appear legitimate based on naming conventions and forged metadata. When developers
install these packages, post-install scripts exfiltrate environment variables and credentials
from the build system without producing any visible errors.

## Detection guidance

- Audit dependency manifests for packages that closely resemble but do not exactly match known
  legitimate package names.
- Review installation logs for unexpected pre-install or post-install script execution.
- Compare package download counts, author profiles, and repository links against expected values
  for legitimate packages.
- Monitor for newly published packages with names similar to your project's existing
  dependencies.
- Use namespace reservation and scoped packages where supported by the ecosystem to reduce
  confusion risk.

## Remediation

- Remove any identified typo-squatted or brand-jacked packages from the project immediately.
- Rotate credentials and secrets that may have been exposed during the installation of the
  malicious package.
- Audit build systems and developer machines for indicators of compromise.
- Report the malicious package to the package registry for removal.
- Replace with the correct legitimate package, verifying its identity through official project
  channels.
- Implement dependency allowlists and review processes to prevent future name confusion incidents.
