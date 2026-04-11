# 09 Using Components with Known Vulnerabilities

Identifier: SLS09:2017
Category: Supply Chain

## Description

Serverless functions are typically small and designed for microservices.
To execute their tasks, they rely on many dependencies and third-party libraries.
Vulnerabilities introduced through the supply chain are among the most common risks.
Attackers target code that uses vulnerable libraries as an entry point to the application.
In "Poisoning the Well" attacks, adversaries compromise upstream packages and wait for the
new version to propagate into cloud applications.
Component-heavy development patterns lead to teams not knowing which components their
application uses or keeping them up to date.
Each function brings its own set of dependencies, increasing the total number of
third-party packages across the application.
Security updates for these dependencies are not always straightforward and may require
code changes and testing.

## Risk

- Exploitation of known CVEs in third-party libraries to gain code execution.
- Supply chain compromise through poisoned upstream packages that persist in the
  application.
- Data leakage, server-side request forgery, open redirect, or authentication bypass
  through exploitable library vulnerabilities.
- Increased attack surface because each function independently brings its own
  dependencies.
- Delayed detection due to teams not tracking which components are deployed.

## Vulnerability checklist

- Functions include third-party libraries with known CVEs.
- Dependencies are not monitored or updated on a regular schedule.
- Packages are obtained from unofficial sources or without signature verification.
- No dependency scanning tool is integrated into the development or deployment pipeline.
- Functions bundle dependencies that are not actually used in the code.
- No software bill of materials (SBOM) is maintained for the application.
- Upstream package integrity is not verified before deployment.

## Prevention controls

1. Continuously monitor dependencies and their versions throughout the system.
2. Only obtain packages from official sources over secure links and prefer signed packages.
3. Monitor vulnerability databases (CVE, NVD) and platform-based advisories (npm audit,
   pip audit, OWASP SafeNuGet) for known issues.
4. Scan dependencies for known vulnerabilities using tools such as OWASP Dependency-Check,
   OWASP Dependency-Track, Snyk, or equivalent commercial solutions.
5. Remove unused dependencies from function packages.
6. Maintain a software bill of materials for each function and the overall application.
7. Pin dependency versions and verify package integrity before deployment.

## Example attack scenarios

### Scenario A — Vulnerable URL parsing library
A function uses the url-parse library.
A vulnerable version of the library returns an incorrect hostname.
An attacker passes a crafted URL (e.g., `http://google.com:80%5c%5cyahoo.com//`) to the
function, causing it to redirect the user to a malicious website.
The vulnerability enables server-side request forgery, open redirect, or authentication
bypass depending on how the parsed URL is used.

### Scenario B — Poisoned upstream package
An attacker compromises a popular npm package used by several functions.
The compromised version includes code that reads environment variables and exfiltrates
cloud credentials to an external endpoint.
The development team pulls the updated package during a routine dependency update without
verifying the package diff.
All functions using this dependency begin leaking credentials on each invocation.

## Detection guidance

- Run dependency scanning tools as part of every build and deployment pipeline.
- Monitor vulnerability databases and advisory feeds for packages used by the application.
- Compare deployed dependency versions against known vulnerable version ranges.
- Audit function packages for unused or unnecessary dependencies.
- Review package changelogs and diffs before accepting version updates.
- Generate and review SBOMs for each function on a regular schedule.

## Remediation

- Update all dependencies with known vulnerabilities to patched versions.
- Remove unused dependencies from function deployment packages.
- Integrate automated dependency scanning into the CI/CD pipeline with build-breaking
  policies for critical CVEs.
- Pin dependency versions and enable lock files for reproducible builds.
- Verify package signatures and integrity hashes before deployment.
- Establish a regular dependency review cadence across all functions.
- Generate and publish SBOMs for each release.
