# 06 Security Misconfiguration

Identifier: SLS06:2017
Category: Configuration Management

## Description

In serverless architectures, unused pages are replaced with unlinked triggers and unprotected
files are replaced with public cloud resources such as public storage buckets.
Attackers identify misconfigured functions with long timeout or low concurrency limits to
cause Denial of Service (DoS).
Functions with unprotected secrets such as keys and tokens in code or environment variables
result in sensitive information leakage.
Serverless reduces the need to patch the infrastructure, but human error remains the
biggest weakness.
Secrets can be accidentally uploaded to code repositories, placed on public storage, or
hardcoded in function code.
Functions with long timeout configurations give attackers more time to sustain exploits.
Functions with low concurrency limits are vulnerable to DoS, while functions with high
concurrency limits can result in Denial of Wallet (DoW).

## Risk

- Sensitive information leakage through publicly accessible cloud resources or exposed secrets.
- Financial loss from Denial of Wallet attacks exploiting high concurrency or long timeouts.
- Denial of Service against the application through concurrency limit exhaustion.
- Unauthorized access to cloud resources via misconfigured storage or event triggers.
- Manipulation of application execution flow through public upload access to storage that
  triggers internal functions.

## Vulnerability checklist

- Cloud storage buckets or containers have public read or write access enabled.
- Functions have timeout values set higher than necessary for the task.
- Functions have concurrency limits that are either too low (DoS risk) or too high (DoW risk).
- Secrets, keys, or tokens are hardcoded in function code or committed to repositories.
- Functions have unlinked triggers that appear in their policy but are not part of the
  application flow.
- Provider security recommendations for function configuration are not followed.
- No automated scanning detects misconfigured cloud resources.

## Prevention controls

1. Scan cloud accounts to identify public resources using built-in provider services such as
   AWS Trusted Advisor security checks.
2. Review cloud resources and verify each enforces access control.
3. Follow provider security best practices for storage (AWS S3, Azure Storage, Google Cloud
   Storage, IBM Data Security).
4. Check for functions with unlinked triggers and remove or restrict them.
5. Set function timeouts to the minimum value required by the task.
6. Follow provider function configuration guidance for memory, timeout, and concurrency.
7. Use automated tools that detect security misconfigurations in serverless applications.
8. Never commit secrets to source code repositories.

## Example attack scenarios

### Scenario A — Public storage triggering internal logic
A cloud storage bucket is misconfigured with public upload (write) access.
The upload event triggers an internal function that processes the file.
An attacker uploads a crafted file using their own credentials, bypassing the original
application flow and manipulating the internal function execution with attacker-controlled
input.

### Scenario B — Long timeout exploitation
A function has a timeout set to the maximum allowed value for convenience.
An attacker exploits a vulnerability in the function and uses the extended execution time
to perform reconnaissance, exfiltrate data, and establish persistence, actions that would
fail within a shorter timeout window.

## Detection guidance

- Use provider security scanning tools to identify publicly accessible cloud resources.
- Audit function configurations for excessive timeout and concurrency settings.
- Scan source code repositories for committed secrets using automated tools.
- Inventory function triggers and cross-reference against the application architecture to
  identify unlinked or orphaned triggers.
- Monitor billing and usage patterns for unexpected spikes that indicate DoW exploitation.

## Remediation

- Remove public access from all cloud storage and resource configurations.
- Reduce function timeouts to the minimum required for each task.
- Set concurrency limits appropriate to the expected load and budget constraints.
- Remove all hardcoded secrets and migrate to the provider secrets management service.
- Delete unlinked triggers and unused function resources.
- Deploy automated configuration scanning in the CI/CD pipeline.
- Rotate any credentials that may have been exposed through misconfiguration.
