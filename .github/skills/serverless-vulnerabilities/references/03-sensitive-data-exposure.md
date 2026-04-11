# 03 Sensitive Data Exposure

Identifier: SLS03:2017
Category: Data Protection

## Description

Sensitive data exposure is a concern in serverless architecture as in any other architecture.
Traditional methods such as stealing keys, performing man-in-the-middle attacks, and stealing
readable data at rest or in transit still apply.
The data sources differ: instead of stealing data from a server, attackers target cloud storage
(S3, Blob) and database tables (DynamoDB, CosmosDB).
Leaked keys can lead to unauthenticated and unauthorized actions in the cloud account.
The function runtime environment is read-only except for the `/tmp` directory.
Attackers target `/tmp` for leftovers from previous executions in warm containers.
Source code and environment variables are accessible to an attacker who gains code execution
within the function container.
Storing sensitive data in plaintext or using weak cryptography on any storage is common and
persists in serverless applications.

## Risk

- Exposure of sensitive personal information (PII), health records, credentials, and credit
  card data.
- Leaked cloud credentials grant unauthorized access to other resources in the account.
- Data left in `/tmp` from previous warm container executions can be read by subsequent
  invocations.
- Environment variables containing secrets are accessible to an attacker with code execution.
- Weak or missing encryption on stored data allows bulk exfiltration.

## Vulnerability checklist

- Secrets or API keys are hardcoded in function source code.
- Environment variables contain plaintext credentials without encryption.
- Data at rest in cloud storage or databases is not encrypted.
- Functions do not use HTTPS-only endpoints for API communication.
- Sensitive data is written to the `/tmp` directory and not deleted after use.
- Key management relies on application-level storage rather than provider key management
  services.
- Functions do not use the provider encryption services for environment variable protection.

## Prevention controls

1. Identify and classify all sensitive data processed by each function.
2. Minimize storage of sensitive data to only what is absolutely necessary.
3. Protect data at rest and in transit according to provider best practices.
4. Use HTTPS-only endpoints for all API communication.
5. Use the provider key management service for encryption of stored data, secrets, and
   environment variables (AWS KMS, Azure Key Vault, Google Cloud KMS).
6. Encrypt environment variables using the provider encryption helpers.
7. Clean up any data written to `/tmp` before the function execution completes.

## Example attack scenarios

### Scenario A — Hardcoded management credentials
A function contains hardcoded keys for a management AWS account.
An attacker gains access to the code through the repository or the runtime environment.
The attacker uses the stolen credentials with the AWS CLI to list and access resources in
the management account, including storage buckets and other services depending on the IAM
role associated with the stolen credentials.

### Scenario B — Warm container /tmp leakage
A function writes intermediate processing data to `/tmp` and does not clean up on completion.
A subsequent invocation reuses the warm container.
An attacker who achieves code execution in a later invocation reads residual files from
`/tmp` containing data from a previous user request.

## Detection guidance

- Scan source code repositories for hardcoded secrets using tools such as TruffleHog,
  git-secrets, or KeyNuker.
- Audit function environment variables for plaintext credentials.
- Verify that all cloud storage buckets and database tables have encryption enabled.
- Monitor API endpoint configurations for non-HTTPS listeners.
- Review function code for writes to `/tmp` that are not followed by cleanup.
- Check provider key management service usage against the inventory of sensitive data stores.

## Remediation

- Remove all hardcoded credentials from function source code and environment variables.
- Migrate secrets to the provider key management or secrets management service.
- Enable encryption at rest on all cloud storage and database resources.
- Enforce HTTPS-only on all API endpoints.
- Add cleanup logic for any temporary files written to `/tmp`.
- Rotate all credentials that may have been exposed.
- Establish automated secret scanning in the CI/CD pipeline.
