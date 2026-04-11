# 04 XML External Entities

Identifier: SLS04:2017
Category: Input Validation

## Description

XML External Entity (XXE) attacks exploit XML processors that allow specification of an external
entity URI that is dereferenced and evaluated during XML processing.
In serverless, executing remote requests (out-of-band) may not be possible if the function runs
inside an internal virtual private network (VPC).
Scanning attacks are less effective in the few seconds the function has to execute.
Denial of Service is less of a concern because the function runs in a designated container that
affects only the current execution.
A successful XXE attack in a serverless application leads primarily to function code leakage and
access to other sensitive files in the environment such as environment variables and files under
`/tmp`.

## Risk

- Function source code leakage via file-read XXE payloads targeting `/var/task/` or equivalent
  paths.
- Environment variable exposure containing secrets and cloud credentials.
- Reading residual data from `/tmp` left by previous warm container executions.
- Limited but possible server-side request forgery if the function is not in a VPC.

## Vulnerability checklist

- Functions parse XML input from any event source without disabling external entity resolution.
- XML processing libraries have default entity resolution enabled.
- Functions triggered by cloud storage upload events process XML files without safe parser
  configuration.
- The provider SDK is not used for data exchange and XML parsing is performed manually.
- Input validation does not check for DTD declarations or entity references in XML payloads.

## Prevention controls

1. Use the cloud provider SDK for data exchange whenever possible to avoid manual XML parsing.
2. Scan the supply chain for XML-related libraries with known XXE vulnerabilities.
3. Disable external entity resolution and DTD processing in all XML parsers.
4. Test for XXE attacks via API calls during security assessments.
5. Use allowlist input validation to reject unexpected XML structures.
6. Prefer JSON or other non-XML data formats where the use case permits.

## Example attack scenarios

### Scenario A — Storage-triggered XXE
A function is triggered by a cloud storage upload event and parses files containing XML content.
An attacker uploads a file with an XXE payload that references `file:///var/task/handler.py`.
The function processes the XML, resolves the entity, and prints the source code to the log.
Exfiltrating the data out of the account requires an out-of-band or code execution XXE
technique, which may be disabled or unavailable depending on the VPC configuration.

### Scenario B — API Gateway XXE
A function behind an API Gateway endpoint accepts XML payloads.
An attacker submits a request with a crafted DTD that reads environment variables.
The function returns processed output that includes the resolved entity content, exposing
cloud credentials stored in environment variables.

## Detection guidance

- Audit function code for XML parsing without explicit entity resolution disabling.
- Review imported libraries for known XXE vulnerabilities.
- Monitor function logs for unexpected file read operations or outbound requests during XML
  processing.
- Test XML-accepting endpoints with XXE payloads during penetration testing.
- Check VPC configuration to assess whether out-of-band XXE exfiltration is possible.

## Remediation

- Disable external entity resolution and DTD processing in all XML parser configurations.
- Replace XML parsing with the provider SDK for data exchange where applicable.
- Update or replace XML libraries with known XXE vulnerabilities.
- Add input validation that rejects XML payloads containing DTD declarations.
- Move functions that process untrusted XML into a VPC to limit outbound exfiltration.
- Remove any residual sensitive data from `/tmp` and environment variables that are not required.
