# biotrackr

Welcome to my personal health platform! I use a Fitbit to log my workouts, so this application uses the API that Fitbit provides to run some analysis on that data, and provide me insights.

## Documentation

Check out the following markdown files that explains decisions made in this project:

- [GitHub Actions Workflow Templates](./docs/github-workflow-templates.md)
- [Bicep Modules Structure](./docs/bicep-modules-structure.md)

## Tech Stack Used

- Bicep
- GitHub Actions
- .NET

## Build Status

| Component | Status |
| --------- | ------ |
| Biotrackr.Infra | [![Deploy Core Biotrackr Infrastructure](https://github.com/willvelida/biotrackr/actions/workflows/deploy-core-infra.yml/badge.svg)](https://github.com/willvelida/biotrackr/actions/workflows/deploy-core-infra.yml) |
| Biotrackr.Auth.Svc| [![Deploy Auth Service](https://github.com/willvelida/biotrackr/actions/workflows/deploy-auth-service.yml/badge.svg)](https://github.com/willvelida/biotrackr/actions/workflows/deploy-auth-service.yml) |
| Biotrackr.Activity.Svc | [![Deploy Activity Service](https://github.com/willvelida/biotrackr/actions/workflows/deploy-activity-service.yml/badge.svg)](https://github.com/willvelida/biotrackr/actions/workflows/deploy-activity-service.yml) |
| Biotrackr.Activity.Api | [![Deploy Activity Api](https://github.com/willvelida/biotrackr/actions/workflows/deploy-activity-api.yml/badge.svg)](https://github.com/willvelida/biotrackr/actions/workflows/deploy-activity-api.yml) |
| Biotrackr.Sleep.Svc | [![Deploy Sleep Service](https://github.com/willvelida/biotrackr/actions/workflows/deploy-sleep-service.yml/badge.svg)](https://github.com/willvelida/biotrackr/actions/workflows/deploy-sleep-service.yml) |
| Biotrackr.Weight.Svc | [![Deploy Weight Service](https://github.com/willvelida/biotrackr/actions/workflows/deploy-weight-service.yml/badge.svg)](https://github.com/willvelida/biotrackr/actions/workflows/deploy-weight-service.yml) |