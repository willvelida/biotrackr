# 01 Injection

Identifier: SLS01:2017
Category: Input Validation

## Description

Injection flaws occur when untrusted data is sent to an interpreter as part of a command or query.
In serverless applications the attack surface increases beyond traditional API inputs.
Functions can be triggered from cloud storage events, stream data processing, database changes,
code modifications, notifications such as SMS, emails, and IoT events.
There is no control of the line between origin and resource; if a function is triggered via email
or a database change, there is no perimeter to place a firewall or input validation control.
Traditional SQL/NoSQL injection applies, together with OS command injection targeting source code
and secrets in the container, and code injection that leverages the provider API to interact with
other services in the account.

## Risk

- Source code and secrets in the function container can be extracted via OS command injection.
- Injected code can use the cloud provider API to scan and interact with other services in the
  account.
- Data deletion, corruption, or exfiltration from cloud storage and databases.
- Privilege escalation through role permissions assigned to the compromised function.
- Full account takeover if the function role grants user and permission creation.

## Vulnerability checklist

- Functions use `eval()` or equivalent dynamic code execution on untrusted input.
- Input from non-API event sources (cloud storage, streams, emails, IoT) is not validated.
- Functions construct OS commands by concatenating user-controlled strings.
- SQL or NoSQL queries are built using string interpolation instead of parameterized interfaces.
- Functions have overly permissive IAM roles that increase the blast radius of injection.
- No input validation or sanitization is applied to event payloads from any trigger type.

## Prevention controls

1. Never trust, pass, or make assumptions about the validity of input from any resource.
2. Use safe APIs that avoid the interpreter entirely or provide parameterized interfaces.
3. Migrate to Object Relational Mapping (ORM) tools where applicable.
4. Use positive or allowlist input validation when possible.
5. Identify trusted sources and resources and allowlist them.
6. Escape special characters using the specific escape syntax for each interpreter.
7. Consider all event types and entry points into the system as potential attack vectors.
8. Run functions with the least privileges required to perform the task.

## Example attack scenarios

### Scenario A — Code injection via deserialization
A function deserializes data using `eval()` without any validation.
An attacker sends a crafted payload that spawns a child process, archives the function source
code, base64-encodes it, and exfiltrates it to an attacker-controlled server.
The attacker then inspects the code and uses the provider API (found in environment variables)
to read directly from the database.

### Scenario B — Command injection via file upload
A function triggered by a storage upload event processes the uploaded file.
The function is vulnerable to command injection when a downloaded file does not match the
expected extension.
An attacker uploads a file whose name contains shell metacharacters that exit the `/tmp`
directory, navigate to the function code directory, read the source code with hardcoded
credentials, base64-encode it, and send it to an external endpoint.

## Detection guidance

- Monitor function logs for unexpected child process spawns or outbound network calls.
- Review event source configurations to identify untrusted or unvalidated trigger inputs.
- Audit function code for dynamic code execution patterns such as `eval()`, `exec()`,
  `child_process`, and string-concatenated queries.
- Inspect IAM roles attached to functions for overly broad permissions.
- Use static analysis tools to detect injection vulnerabilities across all supported languages.

## Remediation

- Replace all dynamic code execution with parameterized queries or safe API calls.
- Add input validation and sanitization at every function entry point regardless of event source.
- Restrict IAM roles to the minimum permissions required by each function.
- Remove hardcoded credentials and migrate to the provider key management service.
- Deploy runtime application self-protection (RASP) or commercial runtime defense solutions.
- Establish a code review process that covers all event source integrations.
