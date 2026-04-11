# 02 Broken Authentication

Identifier: SLS02:2017
Category: Identity and Access

## Description

Serverless functions run in stateless compute containers with no continuous flow managed by a
single server.
Hundreds of different functions run separately, each triggered from a different event source
with no notion of the other moving parts.
Attackers target forgotten resources such as public cloud storage or open APIs.
Internal-facing resources are also at risk; if a function is triggered by organizational emails,
attackers can send spoofed emails to execute internal functionality without any authentication.
Broken authentication results from poor design of identity and access controls, and the
complexity increases in serverless architectures with multiple entry points, services, events,
and triggers.

## Risk

- Access to functions without authentication leads to sensitive data leakage.
- Unauthorized execution can break the system business logic and flow of execution.
- Spoofed event sources can trigger internal functions with attacker-controlled input.
- Bypass of authentication on non-API entry points grants access to internal services.

## Vulnerability checklist

- Functions triggered by non-API events (emails, storage uploads, notifications) lack sender
  verification.
- API endpoints are deployed without authentication or authorization middleware.
- Internal functions are reachable from unauthenticated event sources.
- Functions rely on client-side tokens without server-side validation.
- No federated identity or token-based authentication is enforced between internal services.
- Event source mappings do not restrict which principals can publish triggering events.

## Prevention controls

1. Use the authentication services provided by the infrastructure: AWS Cognito or Single Sign-On,
   Azure Active Directory B2C or Azure App Service, Google Firebase Authentication.
2. Require authentication and access control on all external-facing resources according to the
   provider best practices for API Gateway access control.
3. Use federated identity (SAML, OAuth2, security tokens) for service-to-service authentication.
4. Follow security best practices: encrypted channels, password and key management, client
   certificates, and multi-factor authentication.
5. Restrict event source mappings so that only authorized principals can publish triggering events.
6. Validate the origin of all non-API triggers such as emails, notifications, and storage events.

## Example attack scenarios

### Scenario A — Spoofed email trigger
A pull request approval workflow sends an email to a designated manager.
The manager replies to approve or decline; an SES service triggers a function with the relevant
permissions.
An attacker who discovers the email address and the required format sends a malicious email
directly to the designated address, approving a pull request that inserts a backdoor into the
codebase.

### Scenario B — Unauthenticated API endpoint
A function behind an API Gateway endpoint is deployed without an authorizer.
An attacker discovers the endpoint URL through reconnaissance and invokes the function directly,
gaining access to backend resources the function is permitted to reach.

## Detection guidance

- Audit API Gateway configurations for missing authorizers or authentication requirements.
- Review event source mappings for functions triggered by emails, notifications, or storage
  events to confirm sender verification is in place.
- Monitor authentication failure logs for patterns indicating brute-force or credential stuffing
  attempts.
- Inventory all function triggers and classify each as authenticated or unauthenticated.
- Test non-API entry points for the ability to trigger functions without valid credentials.

## Remediation

- Add authentication to all API Gateway endpoints using the provider authorizer features.
- Implement sender verification on all email and notification-triggered functions.
- Restrict event source policies so only authorized principals can invoke functions.
- Migrate to provider-managed identity services for all user and service authentication.
- Audit and remove any functions with open or unauthenticated triggers in production.
- Establish automated checks in the deployment pipeline that reject unauthenticated endpoints.
