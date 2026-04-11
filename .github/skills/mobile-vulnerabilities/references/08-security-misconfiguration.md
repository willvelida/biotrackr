# 08 Security Misconfiguration

Identifier: MOB08:2024
Category: Configuration Management

## Description

Security misconfiguration in mobile apps refers to the improper configuration of security
settings, permissions, and controls that can lead to vulnerabilities and unauthorized access.
Mobile apps often come with default configurations that may have weak security settings or
unnecessary permissions enabled. These misconfigurations are common due to time constraints,
lack of awareness, or human error during development.
Threat agents include attackers with physical access to the device or malicious apps on the
device that exploit security misconfigurations to execute unauthorized actions within the
target application's context. Misconfigurations span insecure default settings, improper
access controls, weak encryption settings, lack of secure communication enforcement,
unprotected storage, insecure file permissions, and improper session management.

## Risk

- Unauthorized access to sensitive data including user credentials, personal data, and
  confidential business information.
- Account hijacking or impersonation through weak or misconfigured authentication.
- Data breaches exposing sensitive data to unauthorized individuals.
- Compromise of backend systems through misconfigurations that provide attackers a foothold.
- Financial loss including legal penalties, regulatory fines, and reputational damage.
- App downtime, service disruption, or compromised functionality affecting user experience.

## Vulnerability checklist

- Default settings are used without reviewing security configurations and permissions.
- Debugging features remain enabled in release builds.
- Cleartext traffic (HTTP) is allowed instead of enforcing HTTPS.
- Default usernames and passwords are left unchanged.
- Access controls allow unauthorized users to perform privileged actions.
- Application files are stored with world-readable or world-writable permissions.
- File content providers intended for internal use are exported and accessible to other apps.
- Activities intended for internal use are exported or browsable.
- The application requests excessive permissions beyond what is necessary for core
  functionality.
- Backup mode is not explicitly disabled on Android, allowing sensitive data in backups.
- Security updates and patches are not applied to the app or underlying components.

## Prevention controls

1. Review and secure all default settings and configurations before release.
2. Do not use hardcoded default credentials in the application.
3. Avoid storing application files with world-readable or world-writable permissions.
4. Apply the least privilege principle by requesting only permissions necessary for proper
   functioning.
5. Configure network security to disallow cleartext traffic and use certificate pinning
   where possible.
6. Disable debugging features in production builds.
7. Disable backup mode on Android to prevent inclusion of sensitive app data in device
   backups.
8. Limit the application's attack surface by only exporting activities, content providers,
   and services that are necessary to be exported.
9. Configure file provider paths to prevent exposure of internal resources to other apps.
10. Apply security updates and patches promptly to the app and all underlying components.

## Example attack scenarios

### Scenario A — Insecure default settings
A mobile app is released with weak security configurations including insecure communication
protocols, unchanged default usernames and passwords, and debugging features enabled in
release builds. Attackers exploit these misconfigurations to gain unauthorized access to
sensitive data or perform malicious actions.

### Scenario B — Insecure file provider path settings
A mobile app exposes its root path in an exported file content provider, allowing other apps
to access its internal resources and potentially read sensitive data or modify application
files.

### Scenario C — Overly permissive storage
A mobile app stores shared preferences with world-readable permissions, allowing any other
app on the device to read them and access sensitive configuration data or user preferences.

### Scenario D — Exported internal activity
A mobile app exports an activity meant for internal use, providing attackers with additional
attack surface to trigger internal functionality or access protected screens directly.

### Scenario E — Unnecessary permissions
A simple flashlight app requests access to the user's contacts, location, and camera. These
excessive permissions expose user data to unnecessary risks, as the app could misuse the
granted permissions or unintentionally leak sensitive information.

## Detection guidance

- Review the application manifest for exported activities, content providers, and services
  that should be internal-only.
- Inspect file permissions on application storage to identify world-readable or world-
  writable files.
- Check for debugging features and development flags in release builds.
- Audit the application's requested permissions against its core functionality requirements.
- Verify network security configuration for cleartext traffic allowances.
- Test for unchanged default credentials in the application and its backend services.
- Review backup and `hasFragileUserData` configuration settings.

## Remediation

- Secure all default configurations and remove or change default credentials.
- Disable debugging features and development flags in all release builds.
- Enforce HTTPS for all network communication and implement certificate pinning.
- Restrict file permissions to prevent world-readable or world-writable access.
- Remove unnecessary exported activities, content providers, and services from the manifest.
- Reduce requested permissions to the minimum required for core functionality.
- Explicitly configure backup settings to exclude sensitive data.
- Validate remediation by performing a comprehensive configuration review and security scan
  of the release build.
