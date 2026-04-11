# 05 Insecure Communication

Identifier: MOB05:2024
Category: Network Security

## Description

Insecure communication occurs when mobile applications exchange data with remote servers or
local devices without adequate protection of the transport channel. When data transmission
takes place through the device's carrier network and the internet, a threat agent listening
on the wire can intercept and modify data if it is transmitted in plaintext or using a
deprecated encryption protocol.
While modern applications rely on cryptographic protocols such as SSL/TLS, they can have
flaws in their implementations including using deprecated protocols, accepting bad SSL
certificates (self-signed, revoked, expired, wrong host), or applying SSL/TLS
inconsistently across workflows. This risk covers all communication technologies a mobile
device might use: TCP/IP, WiFi, Bluetooth/Bluetooth-LE, NFC, audio, infrared, GSM, 3G,
SMS, and more.

## Risk

- User data exposure leading to account takeover and user impersonation.
- Interception of user credentials, session tokens, and two-factor authentication codes.
- PII data leaks through unencrypted transmission channels.
- Man-in-the-middle attacks allowing data modification in transit.
- Identity theft, fraud, and reputational damage from intercepted communications.
- Privacy violations from confidential data exposure over insecure channels.

## Vulnerability checklist

- The app transmits sensitive data over plaintext channels (HTTP instead of HTTPS).
- SSL/TLS certificate validation is disabled or improperly implemented.
- The app accepts self-signed, expired, revoked, or wrong-host certificates.
- SSL/TLS is applied inconsistently, covering only select workflows such as authentication.
- Weak cipher suites are negotiated during TLS handshakes.
- Certificate pinning is not implemented for critical connections.
- Sensitive data is sent over alternate channels such as SMS, MMS, or notifications.
- Development overrides that allow untrusted certificates persist into production builds.
- SSL chain verification is not enforced.

## Prevention controls

1. Assume the network layer is not secure and is susceptible to eavesdropping.
2. Apply SSL/TLS to all transport channels used to transmit data to backend APIs or web
   services.
3. Use strong, industry-standard cipher suites with appropriate key lengths.
4. Use certificates signed by a trusted CA provider and never allow bad certificates.
5. Implement certificate pinning for critical connections.
6. Always require SSL chain verification.
7. Only establish secure connections after verifying endpoint server identity using trusted
   certificates in the key chain.
8. Alert users through the UI if the mobile app detects an invalid certificate.
9. Do not send sensitive data over alternate channels such as SMS, MMS, or notifications.
10. Apply a separate layer of encryption to sensitive data before it enters the TLS channel
    as defense in depth.
11. On iOS, ensure `NSURL` calls do not allow self-signed certificates and use
    `NSStreamSocketSecurityLevelTLSv1` for higher cipher strength.
12. On Android, remove code that accepts all certificates such as
    `AllowAllHostnameVerifier` or `SSLSocketFactory.ALLOW_ALL_HOSTNAME_VERIFIER`, and
    avoid overriding `onReceivedSslError`.
13. During development, use self-signed certificates or a local development CA rather than
    disabling SSL verification entirely.

## Example attack scenarios

### Scenario A — Lack of certificate inspection
The mobile app and endpoint successfully perform a TLS handshake, but the app fails to
inspect the certificate offered by the server and unconditionally accepts any certificate.
This destroys mutual authentication capability and makes the app susceptible to man-in-the-
middle attacks through a TLS proxy.

### Scenario B — Weak handshake negotiation
The mobile app negotiates a weak cipher suite with the server during the connection
handshake. The weak encryption can be easily decrypted by an adversary, jeopardizing the
confidentiality of the channel.

### Scenario C — Privacy information leakage
The mobile app transmits personally identifiable information to an endpoint via non-secure
channels instead of over SSL/TLS, jeopardizing the confidentiality of privacy-related data.

### Scenario D — Two-factor authentication bypass
The mobile app receives a session identifier from an endpoint via non-secure channels. An
adversary intercepts the session identifier and uses it to bypass two-factor
authentication.

## Detection guidance

- Observe network traffic from the mobile app to identify plaintext transmissions of
  sensitive data.
- Use a TLS proxy to test whether the app accepts invalid, self-signed, or expired
  certificates.
- Inspect the negotiated cipher suites during TLS handshakes for weak or deprecated
  algorithms.
- Review the application's network security configuration for certificate pinning and
  SSL chain verification settings.
- Audit the codebase for development-time SSL verification overrides that may persist in
  release builds.
- Test all communication channels (not just primary API calls) for consistent TLS usage.

## Remediation

- Enable and enforce SSL/TLS on all communication channels used by the application.
- Implement proper certificate validation and reject all invalid certificates.
- Configure certificate pinning for connections to critical backend services.
- Remove all development-time SSL verification overrides from release builds.
- Upgrade deprecated cipher suites to current industry standards.
- Apply defense-in-depth encryption to sensitive payloads before TLS transmission.
- Validate remediation by performing network traffic analysis and TLS configuration
  testing on the release build.
