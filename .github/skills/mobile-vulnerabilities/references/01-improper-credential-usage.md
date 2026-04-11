# 01 Improper Credential Usage

Identifier: MOB01:2024
Category: Credential Management

## Description

Improper credential usage occurs when mobile applications hardcode credentials within the
application source code or configuration files, or when credentials are mishandled during
storage, transmission, or authentication processes.
Threat agents exploiting hardcoded credentials and improper credential usage can include
automated attacks using publicly available or custom-built tools to locate and exploit
hardcoded credentials or weaknesses due to improper credential handling.
Once identified, an attacker can use hardcoded credentials to gain unauthorized access to
sensitive functionalities of the mobile app or bypass the need for legitimate access by
exploiting improperly validated or stored credentials.

## Risk

- Unauthorized users gain access to sensitive information or functionality within the mobile
  app or its backend systems.
- Data breaches and loss of user privacy through exposed credentials.
- Fraudulent activity conducted using compromised credentials.
- Potential access to administrative functionality via hardcoded secrets.
- Reputation damage to the organization from credential compromise.
- Information theft through intercepted or extracted credentials.

## Vulnerability checklist

- The mobile app contains hardcoded credentials within the source code or configuration files.
- Credentials are transmitted without encryption or through insecure channels.
- The mobile app stores user credentials on the device in an insecure manner.
- User authentication relies on weak protocols or allows for easy bypassing.
- API keys or tokens are embedded directly in the application binary.
- Credentials are not rotated or revoked on a regular schedule.

## Prevention controls

1. Never use hardcoded credentials in the mobile app's code or configuration files.
2. Encrypt credentials during transmission using secure protocols such as TLS.
3. Do not store user credentials on the device; use secure, revocable access tokens instead.
4. Implement strong user authentication protocols with server-side validation.
5. Regularly update and rotate any API keys or tokens used by the application.
6. Use platform-provided secure storage mechanisms such as Keychain (iOS) or Keystore
   (Android) for any secrets that must reside on the device.
7. Perform static analysis on the codebase to detect hardcoded secrets before release.

## Example attack scenarios

### Scenario A — Hardcoded credentials in source code
An attacker discovers hardcoded credentials within the mobile app's source code by
decompiling the application binary. They use these credentials to gain unauthorized access
to sensitive functionality within the app or backend systems, escalating to administrative
operations.

### Scenario B — Insecure credential transmission
An attacker intercepts insecurely transmitted credentials between the mobile app and its
backend systems using a man-in-the-middle position on an open Wi-Fi network. They use the
intercepted credentials to impersonate a legitimate user and gain unauthorized access to
the user's account and data.

### Scenario C — Insecure credential storage
An attacker gains physical access to a user's device and extracts stored credentials from
the mobile app's local storage. The plaintext or weakly protected credentials allow the
attacker to gain unauthorized access to the user's account and associated services.

## Detection guidance

- Perform static analysis of the application source code and configuration files to identify
  hardcoded credentials, API keys, or secrets.
- Use dynamic analysis tools to monitor credential transmission and verify encryption is
  applied to all authentication traffic.
- Inspect local storage, shared preferences, and databases on the device for plaintext or
  weakly protected credential material.
- Review authentication flows to confirm that credentials are validated server-side and not
  solely on the client.
- Audit third-party libraries for embedded credentials or insecure authentication defaults.

## Remediation

- Remove all hardcoded credentials from the application source code and configuration files.
- Replace embedded secrets with secure retrieval mechanisms such as server-side token
  issuance or secure vault integrations.
- Encrypt all credential transmissions using current TLS standards.
- Migrate credential storage to platform-provided secure storage (Keychain, Keystore).
- Implement credential rotation policies for API keys and service tokens.
- Validate remediation by scanning the application binary for residual credential material.
