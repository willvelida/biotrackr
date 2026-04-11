# 03 Insecure Authentication and Authorization

Identifier: MOB03:2024
Category: Access Control

## Description

Insecure authentication and authorization occurs when mobile applications fail to properly
identify users or verify their permissions before granting access to functionality or data.
Authentication identifies an individual, while authorization verifies whether the identified
individual has the necessary permissions for a particular action.
Threat agents exploit these weaknesses through automated attacks, bypassing authentication
by directly submitting service requests to backend servers, or force-browsing to vulnerable
endpoints after passing authentication with low-privilege credentials. Mobile apps face
unique authentication challenges due to offline usability requirements, mobile input
constraints that encourage short passwords or PINs, and the need to support biometric
authentication features.

## Risk

- Unauthorized users gain access to sensitive functionality and data through authentication
  bypass.
- Over-privileged execution of remote or local administration functionality.
- Inability to log or audit user activity when authentication fails to establish identity.
- Inability to detect the source of attacks or understand the nature of exploits.
- Privilege escalation when authorization decisions are made on the mobile device rather
  than the server.
- Reputation damage, information theft, fraud, and unauthorized access to data.

## Vulnerability checklist

- The app can execute backend API service requests without providing an access token.
- Passwords or shared secrets are stored locally on the device.
- A simplified or weak password policy is used for authentication.
- Insecure Direct Object Reference (IDOR) vulnerabilities exist in API endpoints.
- Hidden endpoints lack authorization checks, relying on obscurity for protection.
- User roles or permissions are transmitted from the mobile device to the backend as part
  of requests.
- Client-side authentication or authorization can be bypassed on jailbroken or rooted
  devices.
- Biometric authentication (FaceID, TouchID) is used without properly securing underlying
  authentication material.

## Prevention controls

1. Perform all authentication requests server-side; only load application data after
   successful authentication.
2. Ensure mobile authentication requirements match or exceed those of equivalent web
   application components.
3. Never store user passwords on the device; use device-specific, revocable authentication
   tokens instead.
4. Use strong, unpredictable session identifiers and enforce proper session timeouts.
5. Backend systems must independently verify roles and permissions of the authenticated
   user; never rely on role or permission information sent from the mobile device.
6. Avoid 4-digit PINs as authentication passwords; enforce strong password policies.
7. Implement server-side authorization checks for all privileged functionality, regardless
   of what the client interface exposes.
8. Use biometric authentication (FaceID, TouchID) to unlock biometrically locked secrets
   and protect sensitive authentication materials such as session tokens.
9. Instrument local integrity checks to detect unauthorized code changes when offline
   authentication is required.

## Example attack scenarios

### Scenario A — Anonymous backend execution
Developers assume only authenticated users will generate service requests. The server code
does not verify that incoming requests are associated with known users. Adversaries submit
service requests to the backend and anonymously execute functionality that affects
legitimate users.

### Scenario B — Interface reliance privilege escalation
Developers assume only authorized users can see a particular function in the mobile app.
The backend code does not verify that the identity associated with the request is entitled
to execute the service. Adversaries perform remote administrative functionality using
low-privilege user accounts.

### Scenario C — Weak password exploitation
Due to usability requirements, the mobile app allows 4-digit passwords. The server stores
hashed passwords, but the severely short length allows an adversary to quickly deduce
original passwords using rainbow hash tables if the password file is compromised.

### Scenario D — Insecure direct object reference
A user makes an API request including their actor ID and an OAuth bearer token. The backend
validates the bearer token but fails to verify the actor ID matches the token. The user
tweaks the actor ID and accesses other users' account information.

### Scenario E — LDAP role spoofing
A user makes an API request with a bearer token and a header listing LDAP groups. The
backend validates the token but relies on the incoming LDAP group header rather than
independently verifying group membership. The user modifies the header to claim membership
in administrative groups and performs privileged operations.

## Detection guidance

- Attempt to execute backend functionality anonymously by removing session tokens from
  requests.
- Perform binary attacks against the mobile app while in offline mode to test for client-
  side authentication bypass.
- Test for IDOR vulnerabilities by modifying user identifiers in API requests.
- Audit backend endpoints for missing authorization checks on sensitive functionality.
- Verify that role and permission information originates from the server, not from client-
  submitted data.
- Review session management implementation for predictable tokens, missing timeouts, or
  improper invalidation.

## Remediation

- Implement server-side authentication and authorization checks on all API endpoints.
- Remove any client-side-only authentication or authorization logic.
- Enforce strong password policies and eliminate short PINs as sole authentication factors.
- Add IDOR protections by validating that the authenticated user owns the requested resource.
- Ensure role and permission lookups are performed server-side using trusted identity stores.
- Implement proper session management with secure, random, high-entropy session tokens.
- Validate remediation by performing penetration testing on authentication and authorization
  flows.
