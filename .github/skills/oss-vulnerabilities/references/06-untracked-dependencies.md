# 06 Untracked Dependencies

Identifier: OSS-RISK-6:2025
Category: Inventory Management

## Description

Project developers may not be aware of a dependency on a component at all, because it is not
part of an upstream component's SBOM, because SCA tools are not run or do not detect it, or
because the dependency is not established using a package manager.

Flying under the radar, the respective component cannot be checked or monitored for any of the
other deficiencies such as known vulnerabilities, outdated versions, or license non-compliance.

## Risk

- Untracked dependencies cannot be monitored for known vulnerabilities, leaving blind spots in
  the project's security posture.
- Vulnerability disclosures affecting untracked components go unnoticed, as no inventory entry
  exists to correlate against.
- License compliance violations may occur when untracked components carry restrictive or
  incompatible licenses.
- Untracked transitive dependencies inherited through vendored or rebundled code evade standard
  SCA analysis.
- Developer tools, IDE plugins, build scripts, and test dependencies that are untracked still
  pose security and operational risks to development infrastructure.

## Vulnerability checklist

- SBOMs received for upstream components are incomplete or not validated for accuracy.
- SCA tools are not run, or do not detect all dependency types in the project.
- Third-party code is included as vendored source files, copied code snippets, or rebundled
  compiled artifacts outside of package manager manifests.
- Dependencies are installed manually or through system package managers (such as brew or apt-get)
  without being recorded in project dependency manifests.
- IDE plugins, build scripts, and test dependencies are not inventoried despite having access to
  development and build infrastructure.
- No process exists to compare SCA tool output against actual deployed artifacts for completeness.

## Prevention controls

1. Evaluate and compare SCA tools for their capability to produce accurate bills of materials at
   both coarse-granular level (dependencies declared through package managers) and fine-granular
   level (artifacts like single files included without using package managers).
2. Generate and maintain SBOMs for all projects and validate received SBOMs from upstream
   components for completeness.
3. Track all dependency installation methods, including manual installations, system package
   manager installs, and vendored code, in a centralized inventory.
4. Include developer tools, IDE plugins, build scripts, and test dependencies in the dependency
   inventory.
5. Perform periodic reconciliation between SCA tool output and actual deployed or built artifacts
   to identify gaps.
6. Establish policies requiring all new dependencies to be declared through managed package
   manifests rather than out-of-band inclusion.

## Example attack scenarios

### Scenario A — Vendored vulnerable library
A project includes a platform-specific binary of libwebp directly in its source tree rather than
declaring it as a managed dependency. When a critical vulnerability is disclosed in libwebp, SCA
tools do not flag the project because the dependency is not in any package manifest. The project
remains exposed until a manual audit discovers the vendored copy.

### Scenario B — Phantom transitive dependency
A Python project relies on a dependency that is resolved at install time through implicit
platform-specific resolution. The transitive dependency does not appear in standard lock files.
When a vulnerability is disclosed in the phantom dependency, automated tooling does not detect
it, and the development team is unaware of their exposure.

## Detection guidance

- Run multiple SCA tools and compare their output to identify dependencies that one tool detects
  but another misses.
- Search the codebase for vendored source files, copied code snippets, and embedded compiled
  artifacts that are not declared in package manifests.
- Audit build scripts, CI/CD pipelines, and developer environment setup scripts for dependency
  installations that bypass package managers.
- Compare generated SBOMs against actual runtime dependencies observed in deployed environments.
- Review upstream component SBOMs for completeness, particularly for rebundled or repackaged
  content.

## Remediation

- Add all discovered untracked dependencies to the project's package manifest or internal
  dependency inventory.
- Replace vendored or rebundled code with properly declared package manager dependencies where
  possible.
- Update SBOMs to reflect the complete set of direct, transitive, vendored, and development
  dependencies.
- Configure SCA tools to detect the full range of dependency inclusion methods used by the
  project.
- Establish a review process requiring that all new code inclusions, whether through package
  managers or direct file inclusion, are recorded and tracked.
