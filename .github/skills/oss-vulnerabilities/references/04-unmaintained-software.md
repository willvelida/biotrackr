# 04 Unmaintained Software

Identifier: OSS-RISK-4:2025
Category: Maintenance and Lifecycle

## Description

A component or component version may not be actively developed or supported any more. Patches
for new functional and security bugs may not be developed.

Patch development may therefore need to be done by downstream developers with potentially less
experience and knowledge regarding the affected component. This can result in increased efforts
and longer resolution times. During that time, system access or functionality may need to be
restricted to avoid continued exposure.

## Risk

- Security vulnerabilities in unmaintained components will not receive upstream patches, leaving
  consumers permanently exposed unless they develop patches themselves.
- Downstream developers who attempt to patch unfamiliar unmaintained code may introduce new
  defects or incomplete fixes.
- Resolution times for security issues increase significantly when upstream support is absent.
- System access or functionality may need to be restricted during extended remediation periods.
- The bus factor of single-maintainer or small-team projects creates lifecycle risk when
  maintainers become unavailable.

## Vulnerability checklist

- The component or its version has had no commit activity for an extended period.
- The project has been explicitly archived or marked as end-of-life by its maintainers.
- Issues and pull requests from external contributors receive no response.
- No long-term support (LTS) version or maintenance policy is documented.
- The project lacks corporate-affiliated or reputable account activity indicating reliable backing.
- The project has infrequent or no releases, and this is not attributable to feature completeness
  or maturity.

## Prevention controls

1. Check indicators for project liveliness and health before adopting a dependency, including
   recent issue and commit activity, ratio of externally-contributed issues, and release frequency.
2. Consider that low activity can be a sign of maturity for feature-complete projects; distinguish
   between dormant and mature projects.
3. Look for corporate-affiliated account activity that indicates the project has reliable backing
   and support.
4. Verify the presence of reputable account activity as an indicator that the repository is
   well-maintained.
5. Search for information on the project's maintenance or support strategy, such as the presence
   and dates of long-term support (LTS) versions.
6. Check the project page to see whether it has been archived or whether there are explicit
   statements regarding the project's maintenance status.

## Example attack scenarios

### Scenario A — Abandoned dependency with new CVE
A widely-used npm package whose sole maintainer has been jailed is no longer receiving updates.
A vulnerability is discovered in the package but no upstream patch is produced. Downstream
consumers must either develop their own patch with limited knowledge of the codebase, fork the
project, or migrate to an alternative, all while remaining exposed.

### Scenario B — Archived Go toolkit
A popular Go web toolkit is archived by its maintainers. Projects that depend on it continue
using the last published version. When a security issue is identified, no new release is
forthcoming. Teams must invest significant effort to either patch the archived dependency or
migrate their codebase to an actively maintained alternative.

## Detection guidance

- Monitor upstream repositories for commit, issue, and release activity frequency.
- Check project pages and READMEs for archival notices or end-of-life statements.
- Review maintainer profiles for signs of inactivity across their other projects.
- Compare the project's activity level against similar projects in the same ecosystem.
- Track whether external contributors' issues and pull requests receive timely responses.

## Remediation

- Evaluate actively maintained alternative components that provide equivalent functionality.
- Plan and execute migration to a maintained alternative, prioritizing components with documented
  support policies and LTS versions.
- If migration is not immediately feasible, fork the unmaintained project and assume internal
  maintenance responsibility.
- Establish internal monitoring to detect when any dependency transitions to an unmaintained
  state.
- Document risk acceptance decisions for any unmaintained dependencies that cannot be immediately
  replaced, including compensating controls.
