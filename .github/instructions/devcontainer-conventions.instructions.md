---
description: "Dev container conventions for Biotrackr. Use when: adding or changing dev container features, editing devcontainer.json, the lock file, or the container lifecycle scripts."
applyTo: ".devcontainer/**"
---

# Dev Container Conventions

The dev container defines what every contributor's machine ships. A change here
reaches all fourteen services and every future clone, so it is reviewed as a
shared-environment change, not a local convenience edit.

## Features

- Pin every feature to a digest in `devcontainer-lock.json`. An unpinned feature is
  an unreviewed supply-chain dependency that can change under a floating major tag.
- Resolve the digest from the registry, never from a search result or from memory:

  ```bash
  t=$(curl -s "https://ghcr.io/token?scope=repository:devcontainers/features/<id>:pull&service=ghcr.io" | jq -r .token)
  curl -sI -H "Authorization: Bearer $t" \
    -H "Accept: application/vnd.oci.image.manifest.v1+json" \
    "https://ghcr.io/v2/devcontainers/features/<id>/manifests/<version>" | grep -i docker-content-digest
  ```

- Confirm the feature exists and read its `devcontainer-feature.json` before adding it.
  Check whether it declares its own `postCreateCommand`, `postStartCommand`, or
  `containerEnv`, because those merge with the ones already in `devcontainer.json`
  and the interaction is not obvious from the diff.
- Prefer a feature from `ghcr.io/devcontainers/features/` over an imperative
  `apt-get` line in `on-create.sh`. Reach for the script only when no feature exists.
- `scripts/check-devcontainer.sh` enforces the json/lock pairing. It runs from
  `.githooks/pre-commit` and from the Dev Container Check workflow.

## Documentation

Adding or removing a feature that ships a user-facing tool means updating both
tooling lists in the same commit:

- `docs/devcontainer-setup.md` — the "container automatically" list and the
  architecture diagram box.
- `docs/development.md` — the one-line provisioning summary.

The check script fails a commit that adds a feature without touching either file.

## Secrets and environment

- Declare host-supplied values under `secrets` with a `description`, and surface them
  through `remoteEnv`. Never inline a literal value in any `.devcontainer/` file.
- `remoteEnv` reads from `${localEnv:NAME}`. A missing host variable resolves to an
  empty string rather than failing, so a service that requires one validates at
  startup instead of assuming it is set.

## Lifecycle scripts

- `on-create.sh` installs tooling that belongs to the image; `post-create.sh` does
  one-time workspace setup; `postStartCommand` runs `scripts/init.sh --no-restore`
  on every start and must stay idempotent.
- Keep the `|| true` tolerance on tooling installs, but never on a step whose
  failure would leave the container silently unusable.

## Ports

Every port added to `forwardPorts` needs a matching `portsAttributes` entry with a
`label`. An unlabelled forwarded port is indistinguishable from a stray listener.
