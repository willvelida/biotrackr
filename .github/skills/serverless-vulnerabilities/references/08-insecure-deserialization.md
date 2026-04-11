# 08 Insecure Deserialization

Identifier: SLS08:2017
Category: Input Validation

## Description

Dynamic languages like Python and Node.js, together with the common use of JSON as a
serialized data type, make deserialization attacks more likely in serverless environments.
Most functions use third-party libraries to handle serialization and deserialization,
introducing known vulnerabilities into the application.
Deserialization vulnerabilities are common in Python (pickle), JavaScript (node-serialize),
.NET, and Java.
In serverless, deserialization attacks are limited to the time and space of the function
execution, providing a reduced attack surface compared to persistent servers.
Importing vulnerable libraries that handle deserialization is very common, and the combined
use of JSON types with dynamic languages can result in code injection that leads to resource
takeover.

## Risk

- Arbitrary code execution during deserialization of attacker-controlled input.
- Data leakage through code that accesses function environment variables and source code.
- Cloud resource and account control if the attacker extracts environment credentials
  (AWS_SESSION_TOKEN, AWS_SECRET_ACCESS_KEY, AWS_SECURITY_TOKEN).
- Unauthorized function invocation using stolen credentials with attacker-controlled input.
- Complete takeover of cloud resources depending on the permissions of the compromised
  function.

## Vulnerability checklist

- Functions deserialize data from untrusted sources (cloud storage, databases, emails,
  notifications, APIs) without validation.
- Third-party libraries used for deserialization have known vulnerabilities (e.g.,
  jackson.databind, node-serialize, pickle).
- Functions use language-native deserialization (pickle.loads, eval, readValue) on
  attacker-controlled input.
- No strict type constraints are enforced before processing serialized objects.
- Deserialization usage and exceptions are not monitored for anomalous patterns.
- Dependency scanning does not flag known deserialization vulnerabilities.

## Prevention controls

1. Validate serialized objects originating from any untrusted data by enforcing strict type
   constraints before processing.
2. Review third-party libraries for known deserialization vulnerabilities.
3. Monitor deserialization usage and exceptions to identify possible attacks.
4. Use safe deserialization methods that do not allow arbitrary object instantiation.
5. Prefer data-only formats (JSON parsed with safe parsers) over object serialization formats.
6. Implement input validation on all data that enters deserialization paths.

## Example attack scenarios

### Scenario A — Telegram bot deserialization
A function triggered by API Gateway provides movie information via a Telegram bot.
The function uses a JsonMapper class with jackson.databind.ObjectMapper.readValue(), which
is known to be vulnerable to deserialization attacks.
An unauthenticated Telegram user sends a Java serialized object in the text message.
The crafted payload extracts AWS environment data (AWS_SESSION_TOKEN,
AWS_SECRET_ACCESS_KEY) which the attacker uses to create an AssumeRole and access
AWS resources.
The attacker then invokes the function manually with attacker-controlled input, leading
to a complete takeover of cloud resources.

### Scenario B — Python pickle injection
A function processes configuration data stored in cloud storage using Python pickle.
An attacker gains write access to the storage bucket and replaces the configuration file
with a crafted pickle payload.
When the function loads the file, the pickle deserialization executes arbitrary code that
reads environment variables and exfiltrates cloud credentials.

## Detection guidance

- Audit function code for unsafe deserialization patterns across all supported languages.
- Scan dependencies for known deserialization vulnerabilities (CVE databases, Snyk, OWASP
  Dependency-Check).
- Monitor function logs for deserialization exceptions or unexpected object instantiations.
- Review data flow from untrusted sources to deserialization entry points.
- Test deserialization endpoints with crafted payloads during security assessments.

## Remediation

- Replace unsafe deserialization methods with safe alternatives that enforce type constraints.
- Update or replace third-party libraries with known deserialization vulnerabilities.
- Add strict input validation before all deserialization operations.
- Restrict function IAM roles to minimize the impact of credential theft.
- Implement deserialization monitoring and alerting for anomalous patterns.
- Establish dependency scanning in the CI/CD pipeline to detect new deserialization CVEs.
