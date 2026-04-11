# 09 Insecure Data Storage

Identifier: MOB09:2024
Category: Data Protection

## Description

Insecure data storage occurs when a mobile application stores sensitive information without
adequate protection on the device. This includes weak or nonexistent encryption, storage in
easily accessible filesystem locations such as plain text files or unprotected databases,
insufficient access controls, and improper handling of user credentials.
Threat agents include skilled adversaries targeting mobile apps to extract valuable data,
malicious insiders, state-sponsored actors, cybercriminals seeking financial gain,
script kiddies using pre-built tools, data brokers, competitors, and hacktivists. Attack
vectors include unauthorized access to the device's file system through physical or remote
means, exploiting weak encryption, intercepting data transmissions, leveraging malware, and
exploiting rooted or jailbroken devices to bypass security measures.

## Risk

- Data breaches exposing sensitive information to unauthorized access and extraction.
- Compromised user accounts through access to insecurely stored login credentials or
  personal information.
- Data tampering and integrity issues through modification of unprotected stored data.
- Unauthorized access to critical application resources including sensitive files,
  configuration files, and cryptographic keys.
- Reputation and trust damage leading to decreased user adoption.
- Compliance violations with industry regulations and data protection standards.
- Financial implications from breach investigation, customer notification, and legal
  settlements.

## Vulnerability checklist

- Sensitive data is stored in plain text on the device filesystem.
- Encryption is weak, nonexistent, or improperly implemented for stored data.
- Access controls within the application are insufficient to prevent unauthorized data
  access.
- Sensitive data is exposed through application logs, error messages, or debug features.
- Session management is weak, allowing interception or manipulation of session tokens.
- Cloud storage services used by the app have misconfigured permissions.
- Third-party libraries used by the app have storage-related vulnerabilities.
- Sensitive data is shared with unintended recipients through improper data sharing
  features.
- Temporary files containing sensitive data are not properly deleted after use.
- Data caching includes sensitive information such as authentication tokens without
  adequate security.

## Prevention controls

1. Implement robust encryption algorithms for sensitive data both at rest and in transit,
   using industry-standard algorithms with securely stored and managed encryption keys.
2. Use secure communication protocols (HTTPS, SSL/TLS) for data transmission between the
   mobile application and backend servers.
3. Store sensitive data in platform-specific secure storage mechanisms such as Keychain
   (iOS) or Keystore (Android).
4. Implement strong access controls including secure user authentication, role-based access
   controls, and user permission validation before granting access to sensitive information.
5. Apply input validation and data sanitization to prevent injection attacks and ensure only
   valid data is stored.
6. Implement secure session management with randomly generated session tokens, proper
   session timeouts, and secure session data storage.
7. Keep all libraries, frameworks, and third-party dependencies up to date and apply
   security patches regularly.
8. Monitor the latest security threats and vulnerabilities in the mobile application
   landscape.

## Example attack scenarios

### Scenario A — Passwords stored in plain text
The mobile application stores user passwords in plain text format within a local database or
file. An attacker gains unauthorized access to the device and retrieves the credentials
directly, using them to access user accounts.

### Scenario B — Unsecured local storage
The mobile application stores personally identifiable information (PII) locally without
proper access controls or encryption. Anyone with physical access to the device can extract
and view the data.

### Scenario C — Insecure data caching
The mobile application caches sensitive data such as authentication tokens or session
information without adequate security measures. An attacker with device access obtains
these credentials from the cache and impersonates the user.

### Scenario D — Unprotected logging
The mobile application logs sensitive data including user actions, API responses, or error
messages without security controls. An attacker gains access to the device or intercepts
the log files, exposing sensitive information.

### Scenario E — Insecure cloud storage configuration
The mobile application uses cloud storage services with misconfigured permissions, allowing
unauthorized access to stored user data and resulting in data leakage.

### Scenario F — Improper handling of temporary files
The mobile application creates temporary files to process sensitive data but fails to
properly delete them afterward, leaving sensitive information exposed to unauthorized access.

## Detection guidance

- Inspect local storage, databases, and shared preferences on the device for plaintext
  sensitive data.
- Review application logs and error messages for inadvertent exposure of sensitive
  information.
- Check data caching mechanisms for unprotected sensitive content such as session tokens.
- Inspect temporary file handling to verify proper cleanup after use.
- Audit cloud storage configurations for overly permissive access settings.
- Test file permissions on application storage to identify accessible files.
- Review third-party library data storage practices for known vulnerabilities.

## Remediation

- Encrypt all sensitive data at rest using industry-standard encryption algorithms.
- Migrate sensitive data storage to platform-provided secure storage (Keychain, Keystore).
- Remove sensitive data from application logs, error messages, and debug output.
- Implement proper cleanup of temporary files containing sensitive data.
- Secure cloud storage configurations to prevent unauthorized access.
- Implement proper data caching policies that exclude sensitive information or protect it
  with encryption.
- Apply access controls to restrict data access to authorized users and processes.
- Validate remediation by inspecting device storage, logs, and cloud configurations for
  residual sensitive data exposure.
