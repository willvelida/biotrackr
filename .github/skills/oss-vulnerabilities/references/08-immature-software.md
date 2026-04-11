# 08 Immature Software

Identifier: OSS-RISK-8:2025
Category: Quality Assurance

## Description

An open source project may not apply development best practices, such as not using a standard
versioning scheme, having no regression test suite, no development or review guidelines, or no
documentation. As a result, a component may not work reliably or securely, in the sense of
having security weaknesses that result in exploitable vulnerabilities.

The dependency on an immature component or project comes with operational risks. The dependent
software may not work as expected and result in runtime reliability issues, or its use may be
overly complex and expensive for the dependent software development organization.

A component or project may lack documentation, may not use or comply with an established
versioning scheme (which can result in breaking changes during component updates), or may not
have a test suite to discover regressions introduced through pull or merge requests. Such cases
can increase the effort of developers depending on such components.

## Risk

- Lack of a test suite means regressions introduced through contributions go undetected until
  they affect downstream consumers.
- Non-standard or absent versioning schemes can introduce breaking changes in what appears to be
  a minor or patch update, disrupting dependent projects.
- Missing documentation increases integration effort and the likelihood of incorrect usage that
  leads to security weaknesses.
- Absence of CI practices means code quality is not systematically validated before merge.
- Binary files in the repository make it harder to analyze and assess the component's
  functionality and risks.
- Low project maturity correlates with a higher likelihood of undiscovered security weaknesses.

## Vulnerability checklist

- The project does not include a test suite or test code.
- No code coverage tooling or badge is present, indicating coverage measurement is not part of
  the development process.
- The repository lacks documentation for usage, integration, and contribution.
- The project does not use CI, or a high fraction of commits fail CI checks.
- The project does not follow an established versioning scheme such as Semantic Versioning.
- The repository contains binary files that cannot be easily analyzed for functionality or risk.
- The project has very few downstream dependents, which may indicate limited real-world validation.

## Prevention controls

1. Check quality indicators and whether a project follows development best practices before
   adopting it as a dependency.
2. Verify that the project includes test code and uses code coverage tools in its development
   process.
3. Confirm the repository includes documentation for usage, integration, and contribution.
4. Check that the project uses CI and that a high fraction of commits pass CI checks.
5. Verify the project follows an established versioning scheme to minimize surprise breaking
   changes.
6. Review whether the repository contains binary files that would make analysis difficult.
7. Use the number of downstream dependents as a proxy for project maturity and real-world
   validation.
8. Consult the OpenSSF Best Practices Badge Program for an independent assessment of project
   maturity.

## Example attack scenarios

### Scenario A — Breaking change in patch release
A project depends on a library that does not follow Semantic Versioning. The library publishes
a patch-level release that removes an API method used by the downstream project. The project's
build breaks in production when the updated dependency is pulled in automatically, causing a
service outage.

### Scenario B — Untested contribution introduces vulnerability
A library with no test suite accepts a pull request that introduces a security weakness in an
input validation routine. Without tests to catch the regression, the change is merged and
released. Downstream consumers inherit the vulnerability, which is only discovered months later
through an external security audit.

## Detection guidance

- Review the project repository for the presence of test directories, test configuration files,
  and code coverage badges.
- Check the CI configuration and recent build results to confirm that commits are systematically
  validated.
- Verify the existence of documentation files such as README, CONTRIBUTING, and API reference
  documentation.
- Inspect the version history and tagging scheme to determine whether the project follows
  Semantic Versioning or an equivalent standard.
- Search for binary files in the repository that cannot be source-audited.
- Check the project against the OpenSSF Best Practices Badge Program for an independent maturity
  assessment.

## Remediation

- Prioritize migration to more mature alternatives that demonstrate established development
  practices, test coverage, documentation, and versioning discipline.
- If the immature component must be retained, pin it to a specific tested version and perform
  manual validation before each update.
- Contribute test coverage, documentation, or CI configuration upstream to improve the maturity
  of components the project depends on.
- Establish internal integration tests that exercise the component's functionality to catch
  regressions regardless of upstream test coverage.
- Document the maturity risk for each immature dependency and define review triggers for
  re-evaluation.
