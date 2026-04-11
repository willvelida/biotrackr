# 06 Inadequate Privacy Controls

Identifier: MOB06:2024
Category: Privacy

## Description

Inadequate privacy controls concern the failure to properly protect Personally Identifiable
Information (PII) within mobile applications. PII includes names and addresses, credit card
information, email and IP addresses, and information about health, religion, sexuality, and
political opinions.
This information is valuable to attackers for impersonation, payment fraud, blackmail, or
harm through data destruction or manipulation. PII can be leaked (violation of
confidentiality), manipulated (violation of integrity), or destroyed/blocked (violation of
availability). Almost all apps process some kind of PII, and many collect more than they
need to fulfill their purpose. Risks of privacy violations increase due to careless handling
of PII by developers, particularly when PII flows into logs, error messages, URL query
parameters, clipboard content, backups, or analytics.

## Risk

- Violation of data protection regulations (GDPR, CCPA, PDPA, PIPEDA, LGPD, POPIA, PDPL)
  with associated fines and sanctions.
- Financial damage from victims' lawsuits for privacy violations.
- Reputational damage from large-scale privacy incidents published in media.
- Loss or theft of PII enabling social engineering attacks against the organization.
- Identity theft and fraud using stolen personal information.
- Data manipulation rendering systems unusable for affected users.

## Vulnerability checklist

- The app collects more PII than necessary for its stated purpose.
- PII appears in application logs, error messages, or debug output.
- PII is transmitted as URL query parameters visible in server logs and analytics.
- PII is exposed through clipboard content accessible to other applications.
- Backup configurations include sensitive PII without explicit developer intent.
- The `hasFragileUserData` flag (Android) is not explicitly configured.
- PII is shared with third-party analytics or crash reporting services without sanitization.
- PII is stored or transmitted via channels protected by other insufficient OWASP Mobile
  Top 10 controls (insecure storage, insecure communication, weak authentication).

## Prevention controls

1. Minimize the amount and variety of PII collected; only process data necessary for the
   app's purpose.
2. Replace fine-grained PII with less critical information where possible (e.g., coarse-
   grained location instead of precise coordinates).
3. Reduce PII collection frequency (e.g., location updates every hour instead of every
   minute).
4. Anonymize or blur PII using techniques such as hashing, bucketing, or adding noise.
5. Implement PII expiration policies to delete data after its useful lifetime.
6. Allow users to consent to optional PII usage with clear awareness of additional risk.
7. Sanitize all log messages and error output to remove PII before reporting to servers or
   displaying to users.
8. Never transmit PII as URL query parameters; use request headers or body instead.
9. Explicitly configure backup inclusion settings and the `hasFragileUserData` flag to
   control PII flow through backups and uninstallation.
10. Apply defense-in-depth encryption for particularly critical PII beyond sandbox and
    transport protections.

## Example attack scenarios

### Scenario A — PII leakage through logs and error messages
Crash reports and usage logs include PII because developers included user data in log or
error messages. Third-party libraries also emit PII in their exception messages. This data
becomes visible to platform providers used for collecting crash reports, to users if errors
are displayed on screen, or to attackers who can read device logs.

### Scenario B — PII in URL query parameters
URL query parameters are used to transmit request arguments containing PII to a server.
These parameters are visible in server logs, website analytics, and potentially the local
browser history, exposing sensitive information to unauthorized access.

### Scenario C — PII exposure through backups and uninstallation
An app does not explicitly configure backup settings, causing PII in the app's sandbox to
be included in device backups. An attacker obtains a backup and extracts the sandbox content.
Alternatively, on Android, `hasFragileUserData` is set to true, preserving app data on
uninstallation. An attacker installs a malicious app with the same package ID and accesses
the residual data.

## Detection guidance

- Audit application logs and crash reports for the presence of PII.
- Inspect network traffic for PII transmitted as URL query parameters.
- Review clipboard handling to determine if PII is exposed to other applications.
- Check backup and `hasFragileUserData` configurations for unintended PII inclusion.
- Use static analysis tools to trace PII data flows from collection to storage, logging,
  and transmission.
- Review third-party SDK integrations for PII forwarding to analytics or crash reporting
  services.

## Remediation

- Conduct a full PII audit to inventory all personal data collected, processed, and stored
  by the application.
- Remove unnecessary PII collection and reduce granularity where possible.
- Sanitize all log and error message output to strip PII before emission.
- Move PII out of URL query parameters into request headers or body.
- Explicitly configure backup inclusion and `hasFragileUserData` settings.
- Implement PII expiration and deletion policies.
- Apply defense-in-depth encryption (e.g., TPM-sealed keys) for critical PII.
- Validate remediation by auditing data flows, logs, and backups for residual PII exposure.
