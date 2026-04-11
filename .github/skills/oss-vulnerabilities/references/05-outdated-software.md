# 05 Outdated Software

Identifier: OSS-RISK-5:2025
Category: Maintenance and Lifecycle

## Description

A project may use an old, outdated version of a component even though newer versions exist.

Falling too much behind the latest releases of a dependency can make it difficult to perform
timely updates in emergency situations, such as when a vulnerability is disclosed for the version
in use. Old releases may also not receive the same level of security assessment as recent
versions, especially whether they are affected by newly disclosed vulnerabilities. If a new
version is syntactically or semantically incompatible with the current version in use, application
developers may require significant update and migration efforts to resolve the incompatibility.

## Risk

- Emergency security updates become difficult or impossible when the project is many versions
  behind the current release.
- Older versions may not be assessed for newly disclosed vulnerabilities, leaving exposure unknown.
- Semantic or syntactic breaking changes between the current and latest version create significant
  migration effort that delays remediation.
- Vendors and upstream maintainers may explicitly stop checking older branches for vulnerabilities
  or producing patches for them.
- Accumulated technical debt from deferred updates compounds the cost and risk of each subsequent
  upgrade.

## Vulnerability checklist

- Project dependencies are multiple major or minor versions behind the latest available release.
- No recurring process or backlog item exists for evaluating and applying dependency updates.
- Automated tools for discovering and suggesting dependency updates are not in use.
- Change impact analysis is not performed to detect breaking changes before upgrading.
- Dependency version pins lack a review cadence, causing versions to stale over time.
- Upstream support time frames for the versions in use have expired or are approaching expiration.

## Prevention controls

1. Make dependency updates recurring backlog items with defined review cadences.
2. Automate the discovery and suggestion of updates through merge or pull requests using tools
   such as Dependabot, Renovate, or the Versions Maven Plugin.
3. Detect the introduction of breaking changes through change impact analysis tools such as AexPy.
4. Track upstream support time frames and plan upgrades before versions reach end-of-support.
5. Establish a policy defining the maximum acceptable version lag for each dependency criticality
   tier.
6. Run automated test suites against upgraded dependencies to detect incompatibilities early.

## Example attack scenarios

### Scenario A — Stale Spring Boot version
An organization uses a Spring Boot version whose support time frame has expired. A critical
vulnerability is disclosed, but the vendor explicitly states that the affected branch will not
receive a patch. The team must perform an emergency migration across multiple major versions
under time pressure, introducing risk of regressions and service disruption.

### Scenario B — Outdated Tomcat branch
A project depends on Apache Tomcat 8.0.x. The Apache project states that vulnerabilities are no
longer checked against the 8.0.x branch and will not be fixed. The project remains exposed to
any vulnerabilities discovered after branch support ended, with no upstream remediation path.

## Detection guidance

- Compare dependency versions in manifests and lock files against the latest available versions
  in their respective registries.
- Review upstream project release notes and support documentation for end-of-life or
  end-of-support announcements affecting versions in use.
- Use automated dependency update tools that surface available updates and their changelogs.
- Monitor the gap between the current dependency version and the latest release as a metric over
  time.

## Remediation

- Update outdated dependencies to current supported versions, following a test and validation
  cycle.
- Where direct update introduces breaking changes, plan a phased migration with intermediate
  compatible versions if available.
- Adopt automated dependency update tooling to prevent future version drift.
- Establish and enforce a policy for maximum acceptable version lag based on dependency
  criticality.
- Document any dependencies that cannot be immediately updated, including the rationale and
  compensating controls applied.
