# 09 Unapproved Change

Identifier: OSS-RISK-9:2025
Category: Integrity Assurance

## Description

A component may change without developers being able to notice, review or approve such changes,
because the download link points to an unversioned resource, because a versioned resource has
been modified or tampered with, or due to an insecure data transfer.

Using components that are not guaranteed to be identical when downloaded at different points in
time are primarily a security risk. Attacks such as the one on the Codecov Bash Uploader
demonstrate the risk of piping downloaded scripts directly to bash without checking their
integrity beforehand. Mutable components also threaten the stability and reproducibility of
software builds.

## Risk

- Mutable resources can be silently modified by attackers between downloads, introducing
  malicious code without any change being visible to consumers.
- Piping downloaded scripts directly to a shell without integrity verification grants arbitrary
  code execution to whoever controls the resource at download time.
- Insecure transfer protocols (HTTP) expose downloads to man-in-the-middle attacks that can
  substitute malicious content.
- Non-versioned references in CI/CD pipelines mean that builds are not reproducible and any
  historical build could have used a different version of the resource.
- Tampered versioned resources undermine trust in version pinning as a security control.

## Vulnerability checklist

- CI/CD pipelines reference non-versioned shell scripts or resources via download URLs.
- Dependencies or tools are fetched over insecure HTTP rather than HTTPS.
- Downloaded components are executed or installed without verifying their digest or signature.
- Git repository references in dependency manifests lack a specific commit identifier.
- Versioned resources on package registries or download servers can be overwritten or modified
  after publication.
- No reproducible build process exists to confirm that builds produce identical outputs from
  identical inputs.

## Prevention controls

1. Use resource identifiers that provide guarantees or assurance of always pointing to the same
   immutable artifact, such as version-pinned URLs with integrity hashes.
2. Verify digests or signatures after component download and before installation or use.
3. Use secure protocols (HTTPS) for all component downloads to prevent man-in-the-middle attacks.
4. Reference Git repositories by specific commit hash rather than branch or tag names that can
   be moved.
5. Use lock files with integrity hashes to ensure that resolved dependencies are identical across
   installations.
6. Implement reproducible builds to detect any discrepancy between expected and actual build
   outputs.

## Example attack scenarios

### Scenario A — Tampered CI script
A CI/CD pipeline downloads a bash script from an external URL and pipes it directly to bash on
every build. An attacker compromises the server hosting the script and replaces it with a version
that exfiltrates environment variables and secrets from the build environment. Because the
pipeline does not verify the script's integrity, the tampered version executes undetected across
multiple builds, similar to the Codecov bash uploader incident.

### Scenario B — Insecure Maven dependency resolution
A Java project resolves dependencies over HTTP due to a misconfigured or vulnerable Maven client
(CVE-2021-26291). An attacker on the network path intercepts the request and substitutes a
malicious artifact. The project builds and deploys with the attacker's code included, and the
compromise is not detected because no artifact verification is performed.

## Detection guidance

- Audit CI/CD pipeline configurations for references to unversioned or non-integrity-checked
  external resources.
- Scan dependency manifests and lock files for HTTP URLs or references lacking integrity hashes.
- Review Git dependency references for the use of branch or tag names rather than commit hashes.
- Monitor published artifacts on package registries for unexpected modifications after initial
  publication.
- Compare downloaded artifacts against known-good checksums from a trusted secondary source.

## Remediation

- Replace all non-versioned resource references in CI/CD pipelines with versioned and
  integrity-verified alternatives.
- Add digest or signature verification steps for all externally downloaded components.
- Migrate any HTTP dependency sources to HTTPS.
- Convert Git references from branch or tag names to specific commit hashes.
- Enable lock file integrity checking in the project's package manager configuration.
- Review historical builds for potential exposure during the period when mutable or unverified
  resources were in use.
