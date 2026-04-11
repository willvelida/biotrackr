# 02 Inadequate Supply Chain Security

Identifier: MOB02:2024
Category: Supply Chain Security

## Description

Inadequate supply chain security occurs when vulnerabilities are introduced into a mobile
application through third-party software libraries, SDKs, vendors, or during the build and
distribution process. An attacker can manipulate application functionality by inserting
malicious code into the codebase, compromising app signing keys or certificates, or
exploiting vulnerabilities in third-party components.
This can allow unauthorized data access or manipulation, introduction of backdoors or
spyware, denial of service, or complete takeover of the mobile app or device. The
vulnerability arises from a lack of secure coding practices, insufficient code reviews, an
insecure app signing and distribution process, and insufficient security controls over
third-party dependencies.

## Risk

- Data breaches through malicious code injected during development or build processes.
- Malware infection of user devices via compromised application packages.
- Unauthorized access to backend systems through backdoors introduced in the supply chain.
- Complete system compromise if app signing keys or certificates are stolen.
- Financial losses from investigating breaches, notifying affected users, and legal
  settlements.
- Reputational damage and loss of customer trust from publicly disclosed supply chain
  incidents.
- Legal and regulatory consequences including fines and government investigations.
- Supply chain disruption leading to delays in delivery of services.

## Vulnerability checklist

- Third-party libraries or SDKs are included without security vetting or validation.
- The app signing and distribution process lacks integrity verification controls.
- Source code reviews are insufficient or absent during the development lifecycle.
- Third-party components are not regularly updated or patched for known vulnerabilities.
- No monitoring or detection mechanisms exist for supply chain security incidents.
- Build pipelines lack integrity checks or reproducible build verification.
- Hardcoded credentials exist within third-party dependencies.

## Prevention controls

1. Implement secure coding practices, code review, and testing throughout the mobile app
   development lifecycle to identify and mitigate vulnerabilities.
2. Ensure secure app signing and distribution processes to prevent attackers from signing
   and distributing malicious code.
3. Use only trusted and validated third-party libraries or components, and maintain a
   software bill of materials.
4. Establish security controls for app updates, patches, and releases to prevent attackers
   from exploiting vulnerabilities.
5. Monitor and detect supply chain security incidents through security testing, scanning,
   or other techniques to detect and respond to incidents in a timely manner.
6. Verify the integrity of build artifacts and enforce reproducible builds where possible.
7. Implement dependency pinning and hash verification for all third-party packages.

## Example attack scenarios

### Scenario A — Malware injection during development
An attacker injects malware into a popular mobile app during the development phase. The
attacker signs the app with a valid certificate and distributes it to the app store,
bypassing the store's security checks. Users download and install the infected app, which
steals login credentials and sensitive data. The attacker uses the stolen data to commit
fraud or identity theft.

### Scenario B — Compromised third-party library
A widely used third-party analytics library is compromised by an attacker who publishes a
malicious update. The mobile app automatically pulls the updated dependency during the
build process. The compromised library exfiltrates user data to an attacker-controlled
server without the app developer's knowledge.

### Scenario C — Stolen signing keys
An attacker compromises the CI/CD pipeline of a mobile app development team and extracts
the app signing keys. The attacker creates a trojanized version of the app signed with the
legitimate keys and distributes it through unofficial channels. Users who install the
trojanized version are fully compromised.

## Detection guidance

- Perform software composition analysis to identify known vulnerabilities in third-party
  dependencies.
- Monitor third-party library repositories for unexpected updates or ownership changes.
- Verify app signing certificate integrity and monitor for unauthorized use of signing keys.
- Audit build pipelines for unauthorized modifications or access.
- Use binary analysis tools to compare release builds against expected baselines.
- Review third-party SDKs for excessive permissions or suspicious network activity.

## Remediation

- Conduct a full audit of all third-party components and remove any that are untrusted,
  unmaintained, or contain known vulnerabilities.
- Rotate app signing keys and certificates if a compromise is suspected.
- Implement automated dependency scanning in CI/CD pipelines to catch vulnerable components
  before release.
- Establish an incident response process specific to supply chain compromise scenarios.
- Enforce code review requirements for all changes, including dependency updates.
- Validate remediation by performing a complete software composition analysis on the
  release build.
