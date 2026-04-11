# 07 Cross-Site Scripting

Identifier: SLS07:2017
Category: Input Validation

## Description

Cross-Site Scripting (XSS) attacks target the browser and the attack vectors are similar
to traditional applications.
The variation in serverless comes from the source of the stored attack.
Traditional XSS attacks originate from databases or reflective inputs.
In serverless, attacks can also originate from emails, cloud storage, logs, IoT devices,
and other non-traditional event sources.
XSS occurs when untrusted input is used to generate data that enters the DOM without
proper escaping.
For web services, untrusted data is typically extracted from JSON payloads.
Serverless is stateless by default, making session cookie theft less likely.
Sensitive data such as API keys stored in browser local or session storage remains at risk.
Attacks targeting user privacy (camera, speaker, location) are unaffected by the
architecture.

## Risk

- Execution of arbitrary code in the user browser.
- Theft of API keys or tokens stored in browser local or session storage.
- User privacy compromise through access to camera, microphone, and location.
- Phishing and credential harvesting through injected content.
- Reduced but still present risk of session hijacking when tokens are stored client-side.

## Vulnerability checklist

- Functions return user-supplied data to the client without output encoding.
- Functions process untrusted input from non-API sources (emails, cloud storage, IoT) and
  render it in web responses.
- Client-side code inserts function response data into the DOM without escaping.
- Content-Security-Policy headers are not configured on API Gateway responses.
- No output encoding framework is used in the response rendering pipeline.
- Functions accept data from SNS, SQS, or similar messaging services and forward it to
  web clients without sanitization.

## Prevention controls

1. Encode all untrusted data before sending it to the client.
2. Use known frameworks that automatically escape output by design.
3. Set Content-Security-Policy and X-XSS-Protection headers on API Gateway responses.
4. Validate and sanitize data from all event sources before including it in web responses.
5. Apply context-sensitive output encoding (HTML, JavaScript, URL, CSS) based on where the
   data is rendered.
6. Avoid inserting untrusted data directly into the DOM via `innerHTML` or equivalent methods.

## Example attack scenarios

### Scenario A — Email subject XSS via notification
A support application alerts operators when emails arrive via SNS.
A function triggered by the SNS event pushes a notification to the operator dashboard.
The client listens to the topic via MQTT-WebSocket and prints the email subject without
output encoding.
An attacker sends an email with a malicious script in the subject line, resulting in
XSS execution in the operator browser.

### Scenario B — Cloud storage metadata XSS
A function reads file metadata from cloud storage and displays it in a web dashboard.
An attacker uploads a file with a crafted filename containing an XSS payload.
The dashboard renders the filename without encoding, executing the payload in the
browser of any user viewing the file listing.

## Detection guidance

- Review function code for responses that include user-supplied data without output encoding.
- Audit client-side code for unsafe DOM insertion patterns such as `innerHTML`.
- Test all web-facing endpoints with XSS payloads during penetration testing.
- Check API Gateway response configurations for missing security headers.
- Trace data flow from non-API event sources (email, storage, IoT) through to web responses.

## Remediation

- Add output encoding to all function responses that include user-supplied data.
- Deploy a content security policy on all API Gateway endpoints.
- Replace unsafe DOM insertion with text-only methods or framework-managed rendering.
- Add server-side sanitization for data from non-API event sources before web rendering.
- Establish automated XSS testing in the CI/CD pipeline.
- Audit and update client-side code to use safe rendering patterns.
