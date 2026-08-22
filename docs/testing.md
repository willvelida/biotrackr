---
title: Testing
description: Test tier structure, fixture hierarchy, coverage policy, and the correct coverage command for Biotrackr services
ms.date: 2026-08-21
ms.topic: concept
---

Every Biotrackr service is verified at up to three tiers, selected by test-name filter rather than by project. Edit-time conventions such as naming and the AAA pattern live in `.github/instructions/testing-conventions.instructions.md`, which loads automatically when you edit a test file.

## Three tiers

| Tier     | Location                       | Filter                        | Database        | Coverage collected |
|----------|--------------------------------|-------------------------------|-----------------|--------------------|
| Unit     | `*.UnitTests/`                 | none, the default             | Mocked          | Yes, 70% gate      |
| Contract | `*.IntegrationTests/Contract/` | `FullyQualifiedName~Contract` | None            | No                 |
| E2E      | `*.IntegrationTests/E2E/`      | `FullyQualifiedName~E2E`      | Cosmos emulator | No                 |

Contract and E2E tests share one project. The filter is what separates them, which means running `dotnet test` in the integration test project with no filter runs both and fails without an emulator. Always pass a filter there.

```bash
cd src/Biotrackr.Activity.Api

dotnet test --no-build
dotnet test --no-build --filter "FullyQualifiedName~Contract"
dotnet test --no-build --filter "FullyQualifiedName~E2E"
```

Twelve of the fourteen services carry all three tiers. `Biotrackr.Reporting.Svc` has contract tests but no E2E, and `Biotrackr.UI` has unit tests only. `Biotrackr.Chat.Api` adds a fourth category for agent evaluation.

## Coverage

The gate is 70% line coverage, enforced in CI and failing the build below it. 80% is the healthy target. Coverage is collected from unit tests only; contract and E2E tests are excluded deliberately, so adding integration tests will not lift a failing number.

Run coverage from the service directory:

```bash
cd src/Biotrackr.Activity.Api
dotnet test --no-build --collect:"XPlat Code Coverage" --settings coverage.runsettings --results-directory ./TestResults

reportgenerator -reports:"./TestResults/**/coverage.cobertura.xml" -targetdir:"./CoverageReport" -reporttypes:TextSummary
cat ./CoverageReport/Summary.txt
```

`reportgenerator` is a one-time global install:

```bash
dotnet tool install -g dotnet-reportgenerator-globaltool
```

### The settings path depends on where you are standing

There are fourteen `coverage.runsettings` files, one per service directory at `src/<Service>/coverage.runsettings`. There is no `src/coverage.runsettings`.

* From the service directory, the path is `coverage.runsettings`.
* From a test project directory, the path is `../coverage.runsettings`. This is what the CI workflows use, because their working directory is the test project.

`--settings ../coverage.runsettings` run from the service directory points at a file that does not exist. The run still succeeds and still emits a coverage file, but with default settings, so the generated-code exclusions in the real settings file are ignored and the reported number is wrong.

Each settings file excludes source-generated OpenAPI code. `[ExcludeFromCodeCoverage]` covers other generated types.

## Fixtures

Integration tests share fixtures through xUnit collections rather than per-class setup:

* `IntegrationTestFixture` implements `IAsyncLifetime` and owns the `WebApplicationFactory`. It clears the container and seeds data in `InitializeAsync`, and removes test documents in `DisposeAsync`.
* `ContractTestFixture` derives from it and overrides `InitializeDatabase` to `false`, which is what lets contract tests run with no emulator.

Contract test classes carry `[Collection(nameof(ContractTestCollection))]`; E2E classes carry `[Collection(nameof(IntegrationTestCollection))]`. Omitting the attribute gives each class its own fixture instance, which starts a second `WebApplicationFactory` and races the first over container state.

Contract tests cover three things: the app starts and serves health and OpenAPI, DI registrations resolve, and service lifetimes are correct. They are the cheapest way to catch a broken `Program.cs`.

E2E tests force `ConnectionMode.Gateway`. Direct mode fails against the emulator over TCP and TLS, and the resulting error does not mention connection mode.

## Before you push

Run the affected service's unit tests and check coverage locally. CI rejects pull requests below 70%, and a coverage regression discovered in CI costs a full pipeline run to confirm.

## Related documents

* `.github/instructions/testing-conventions.instructions.md` for naming, AAA structure, and assertion style
* [development.md](development.md) for starting the Cosmos emulator that E2E tests need
* `docs/decision-records/2025-10-28-contract-test-architecture.md` and `2025-10-28-integration-test-project-structure.md` for why the tiers are shaped this way
* `docs/decision-records/2025-10-29-coverlet-extension-method-coverage-anomaly.md` for a known coverage reporting quirk
