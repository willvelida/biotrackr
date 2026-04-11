# 07 License and Regulatory Risk

Identifier: OSS-RISK-7:2025
Category: Compliance

## Description

A component or project may not have a license at all, one that is incompatible with the intended
use by a downstream consumer, or one whose requirements are not or cannot be met by a downstream
user.

A component may also violate license terms independent from downstream use, for example if it is
licensed as GPL but includes files licensed under the original four-clause BSD license.

A component may also conflict with legal and regulatory requirements, such as those related to
FedRAMP certification or export control.

It is important to use components in compliance with their license terms. The absence of a
license or non-compliant use can result in copyright or license infringements, which the
copyright holder can take legal action against.

The violation of legal and regulatory requirements can constrain or hamper addressing certain
verticals or markets.

## Risk

- Absence of a license means the component is under default copyright, making any use potentially
  infringing.
- Incompatible licenses between components or between a component and the downstream project can
  create unresolvable legal conflicts.
- Non-compliance with license requirements such as attribution, source disclosure, or copyleft
  obligations exposes the organization to legal action from copyright holders.
- Regulatory conflicts, such as export control restrictions or certification requirements, can
  prevent the software from being deployed in target markets or verticals.
- Components containing files under multiple incompatible licenses create hidden compliance
  burdens.

## Vulnerability checklist

- Components are used without identifying or recording their license.
- The intended use of the component (linking method, deployment model, distribution scheme) has
  not been evaluated against the component's license terms.
- Components without any license are included in the project.
- No review is performed to detect components containing files under multiple or incompatible
  licenses.
- License compliance requirements such as attribution or source disclosure are not tracked or
  fulfilled.
- No evaluation is performed against legal and regulatory requirements such as FedRAMP
  certification or export control.

## Prevention controls

1. Identify acceptable licenses for the intended use of each component, considering how the
   component is linked, the software's deployment model (cloud or on-premise), and the intended
   distribution scheme.
2. Comply with requirements stated in open source licenses, including attribution, source
   disclosure, and copyleft obligations.
3. Avoid components without a license, as they are under default copyright protection.
4. Scrutinize component files for multiple and incompatible licenses.
5. Maintain a license inventory aligned with the project's SBOM and review it during each
   dependency update.
6. Evaluate components against applicable legal and regulatory requirements before adoption.

## Example attack scenarios

### Scenario A — GPL violation in proprietary product
An organization incorporates a GPL-licensed component into a proprietary product distributed to
customers without releasing the source code as required by the GPL. The copyright holder
discovers the violation and files a lawsuit, resulting in legal costs, mandatory source
disclosure, or product withdrawal.

### Scenario B — Unlicensed component in regulated environment
A development team includes a component that has no license in a product targeting FedRAMP
certification. During the certification audit, the absence of a license is flagged as a
compliance failure. The team must either obtain a license from the copyright holder, replace the
component, or accept delays in certification.

## Detection guidance

- Use license scanning tools such as Fossology to identify licenses declared in component files
  and detect multi-license situations.
- Cross-reference detected licenses against the OSI Approved Licenses list and the SPDX License
  List for standardized identification.
- Review component repositories and package metadata for license declarations, noting that some
  metadata may be incomplete or inaccurate.
- Audit the project's license inventory against its SBOM to ensure all components have a recorded
  and evaluated license.
- Check for REUSE compliance to confirm that every file in the project carries clear license and
  copyright information.

## Remediation

- Remove or replace components that have no license or whose license is incompatible with the
  project's intended use.
- Fulfill outstanding license compliance obligations such as attribution notices or source
  disclosure.
- Update the project's license inventory to reflect the current state of all dependencies.
- Where license conflicts exist between components, evaluate alternative components with
  compatible licenses.
- Engage legal counsel when license or regulatory compliance questions cannot be resolved through
  technical analysis alone.
- Implement automated license scanning in the CI/CD pipeline to prevent introduction of
  non-compliant dependencies.
