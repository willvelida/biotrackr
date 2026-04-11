# 10 Insufficient Logging and Monitoring

Identifier: SLS10:2017
Category: Observability

## Description

Attackers rely on the lack of monitoring and timely response to achieve their goals without
detection.
Serverless auditing is more difficult than in traditional applications where teams use their
own logging systems rather than the one provided by the infrastructure.
Applications that rely solely on the service provider monitoring tools probably have
insufficient means of security auditing.
The ephemeral nature of serverless functions makes exploits less persistent; an infection
may resolve itself when the container is recycled.
However, identifying security incidents late can still cause significant damage.
Provider logging services have limitations: log volume capacity, memory allocation consumed
by high-volume logging, and some services not monitored at all.
Deploying an auditing mechanism for serverless applications may require additional
permissions and can cause unexpected performance impact on functions designed to run for
milliseconds.

## Risk

- Security incidents go undetected, allowing attackers extended dwell time.
- Attacker activity masked by exploiting provider log size limits (e.g., CloudWatch 1 MB
  per event limit, causing truncation of audit entries).
- Loss of forensic evidence due to ephemeral container recycling.
- Inability to reconstruct attack timelines when logging is incomplete.
- Delayed incident response because monitoring does not cover all event sources and
  function triggers.

## Vulnerability checklist

- The application relies solely on provider-default logging without supplemental auditing.
- Function logs are not monitored for security-relevant events such as failed
  authentication, unauthorized access, or excessive execution time.
- Log volume limits of the provider logging service are not accounted for in the logging
  strategy.
- Functions do not log input validation failures or unusual event source activity.
- No alerting mechanism is configured for anomalous function behavior.
- Audit logs are not preserved beyond the provider default retention period.
- Functions with high-volume data processing can exhaust their memory allocation through
  logging overhead.

## Prevention controls

1. Use the monitoring tools provided by the service provider (Azure Monitor, AWS CloudTrail)
   to identify and report unwanted behavior: wrong credentials, unauthorized access,
   excessive function execution, unusually long execution time.
2. Deploy a supplemental auditing and monitoring mechanism for data and events not covered
   by the provider default tools.
3. Account for provider log size limits when designing the logging strategy.
4. Configure alerts for anomalous function behavior including unusual invocation patterns,
   unexpected execution duration, and error rate spikes.
5. Preserve audit logs beyond the provider default retention period using long-term storage.
6. Monitor all event source types, not only API Gateway requests.

## Example attack scenarios

### Scenario A — Log truncation bypass
A serverless firewall function validates incoming input against a blacklist and logs
malicious inputs to CloudWatch.
A monitoring function reads CloudWatch events and notifies on detection.
An attacker sends a payload larger than the CloudWatch log event size limit (1 MB).
The malicious input is not logged, and the function does not write the END and REPORT
log entries.
The monitoring function never detects the attack because only the START event entry
appears in the logs.

### Scenario B — Ephemeral exploit with no trail
An attacker exploits a vulnerability in a function to exfiltrate data.
The function does not log the data access or the outbound request.
The container is recycled before any manual investigation occurs.
No forensic evidence remains because the application did not implement supplemental
logging beyond the provider defaults.

## Detection guidance

- Verify that security-relevant events (authentication failures, authorization denials,
  input validation failures) are logged by each function.
- Test logging behavior with payloads that approach or exceed provider log size limits.
- Audit log retention policies against the organization incident response timeline
  requirements.
- Confirm that alerting is configured for all monitored events and that alerts reach the
  security operations team.
- Review function memory and timeout configurations to ensure logging overhead does not
  cause resource exhaustion.

## Remediation

- Implement supplemental logging for security events not covered by the provider defaults.
- Configure log forwarding to a centralized log aggregation service with appropriate
  retention.
- Set up automated alerting for authentication failures, authorization denials, and
  anomalous execution patterns.
- Add input validation failure logging to all function entry points.
- Validate that log entries are complete and not truncated by testing with boundary-sized
  payloads.
- Establish an incident response playbook that accounts for the ephemeral nature of
  serverless containers.
