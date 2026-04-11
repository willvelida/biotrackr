# 05 Broken Access Control

Identifier: SLS05:2017
Category: Identity and Access

## Description

A serverless application can consist of hundreds of microservices with different functions,
resources, services, and events orchestrated together to create complete system logic.
The stateless nature of serverless architecture requires careful access control configuration
for each resource.
Attackers target over-privileged functions to gain unauthorized access to resources in the
cloud account.
Access control weaknesses are common due to the lack of automated detection and lack of
testing by application developers.
Any function that does not follow the least privilege principle is subject to potential broken
access control.

## Risk

- Data leakage from cloud storage or databases when a function is compromised.
- Unauthorized creation or deletion of cloud resources leading to significant financial loss.
- Full control over resources or the entire cloud account if the compromised function has
  broad IAM permissions.
- Denial of Wallet (DoW) via cost-consuming unauthorized actions such as high-volume uploads
  or bandwidth-intensive downloads.
- Lateral movement to other services and functions through over-privileged roles.

## Vulnerability checklist

- Functions have IAM policies that grant wildcard actions (e.g., `*`) on resources.
- Functions have IAM policies that grant access to all resources (e.g., `arn:aws:s3:::*`).
- IAM roles are shared across multiple functions with different operational needs.
- No automated tooling validates function permissions against the least privilege principle.
- Functions have permissions to create other resources, users, or roles.
- Access control reviews are not performed before each function delivery.

## Prevention controls

1. Examine each function and apply the least privilege principle to its IAM role.
2. Review each function before delivery to identify excessive permissions.
3. Automate the process of permission configuration and validation for functions.
4. Create a dedicated IAM role per function rather than sharing roles.
5. Follow the provider best practices: AWS IAM Best Practices, Azure Identity Management
   Best Practices, Google Secure IAM, IBM IAM Security.
6. Remove wildcard permissions and replace them with specific resource ARNs and actions.

## Example attack scenarios

### Scenario A — Over-privileged storage function
A function designed to write to a specific cloud storage bucket is assigned an IAM policy
that authorizes any action on any bucket in the account.
An attacker exploits a vulnerability in the function and performs unauthorized actions:
reading or deleting other users orders, uploading unvalidated files, deleting other storage
buckets, executing functions triggered by storage events with malicious input, and consuming
costs through high-volume uploads.

### Scenario B — Role with resource creation permissions
A function has an IAM role that includes permissions to create other Lambda functions and
IAM roles.
An attacker exploits the function and uses the credentials to create a new function with
an administrative role, establishing persistent access to the cloud account.

## Detection guidance

- Audit IAM policies attached to all functions for wildcard actions or resources.
- Use provider tools (AWS IAM Access Analyzer, Azure AD Access Reviews) to identify
  over-privileged roles.
- Monitor CloudTrail or equivalent audit logs for unauthorized resource access patterns.
- Compare the actions a function actually performs against the permissions it has been granted.
- Run automated least privilege analysis as part of the deployment pipeline.

## Remediation

- Replace wildcard IAM policies with specific actions and resource ARNs for each function.
- Create individual IAM roles per function scoped to the exact resources needed.
- Remove permissions to create users, roles, or other resources unless explicitly required.
- Deploy automated IAM policy validation in the CI/CD pipeline.
- Establish periodic access control reviews for all functions.
- Rotate credentials and audit access logs after any suspected compromise.
