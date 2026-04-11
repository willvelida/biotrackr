# 10 Under/Over-sized Dependency

Identifier: OSS-RISK-10:2025
Category: Dependency Management

## Description

A component may provide very little functionality (such as npm micro packages) or a lot of
functionality of which only a fraction may be used.

Very small components, containing few lines of code, are subject to the same supply chain risks
as large ones, including account takeover, malicious pull requests, and CI/CD vulnerabilities,
for comparably little functionality. In other words, in exchange for very few lines of code used,
the consumer's security becomes dependent on the upstream project's security and development
posture.

Very large components, on the other hand, may have accumulated many features that are not needed
in standard use cases but contribute to the component's attack surface. Additionally, such unused
features may also bring in additional unused dependencies (bloated dependencies).

## Risk

- Micro packages introduce the full spectrum of supply chain risks (account takeover, malicious
  contributions, CI/CD compromise) for a disproportionately small amount of functionality.
- Removal or breakage of a micro package can cascade into build failures across numerous
  downstream consumers.
- Oversized components include unused capabilities that enlarge the attack surface without
  providing value to the consumer.
- Unused features in large components may use security-sensitive APIs such as network connections
  or filesystem access, creating unnecessary exposure.
- Bloated dependencies pulled in by unused features of an oversized component further expand the
  dependency tree and associated risks.

## Vulnerability checklist

- The project depends on micro packages that implement trivial functionality achievable with a
  few lines of application code.
- Oversized dependencies are included when only a small fraction of their functionality is used.
- No analysis has been performed to identify unused capabilities in dependencies, especially
  those using security-sensitive APIs.
- The project has not evaluated whether micro package functionality could be redeveloped
  internally.
- Oversized components have not been assessed for the availability of smaller, more focused
  alternatives.
- Unused transitive dependencies pulled in by oversized components are not identified or tracked.

## Prevention controls

1. Become aware of unused component capabilities, especially those that use security-sensitive
   APIs such as establishing network connections or accessing the filesystem.
2. Evaluate possibilities to disable unused capabilities in oversized components, or move to
   smaller alternative open source components with fewer capabilities.
3. Become aware of micro packages and consider redeveloping their functionality internally when
   the implementation is trivial.
4. Use capability analysis tools such as Google Capslock (Go) or Microsoft Application Inspector
   to identify security-sensitive API usage in dependencies.
5. Review the transitive dependency tree for bloated dependencies introduced by unused features
   of oversized components.
6. Establish dependency selection criteria that consider the ratio of used functionality to total
   component scope and supply chain exposure.

## Example attack scenarios

### Scenario A — Micro package removal cascade
A widely-used npm package consisting of 11 lines of code for string padding is removed from the
registry by its author. Builds of numerous downstream projects break immediately because they
depend on this trivial functionality through a published package rather than implementing it
directly. The incident causes widespread disruption across the JavaScript ecosystem.

### Scenario B — Oversized logging framework with remote execution
A project includes an enterprise logging framework for basic log output. The framework contains
functionality to download and execute arbitrary code from remote servers, a feature the project
never uses. When a vulnerability is disclosed in this remote execution feature (as occurred with
Log4Shell), the project is exposed despite never having used the affected functionality.

## Detection guidance

- Analyze the dependency tree to identify micro packages with trivially small codebases.
- Use capability analysis tools to discover security-sensitive API usage in dependencies,
  especially capabilities that the project does not exercise.
- Compare the set of features used from each dependency against the full set of features the
  dependency provides.
- Review transitive dependencies for packages pulled in by unused features of direct dependencies.
- Monitor for ecosystem events (package removal, ownership changes) that disproportionately
  affect micro packages.

## Remediation

- Replace micro packages with internal implementations when the functionality is trivial to
  reproduce.
- Evaluate and migrate from oversized dependencies to smaller, more focused alternatives that
  provide only the required functionality.
- Where migration from an oversized component is not feasible, disable or restrict unused
  capabilities through configuration.
- Remove unused transitive dependencies by excluding them from the dependency resolution or
  by switching to a more minimal direct dependency.
- Document the rationale for retaining any micro or oversized dependencies, including the
  assessed supply chain risk and compensating controls.
