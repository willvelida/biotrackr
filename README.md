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

| Component | Status | Code Coverage |
| --------- | ------ | ------------- |
| Biotrackr.Infra | [![Deploy Core Biotrackr Infrastructure](https://github.com/willvelida/biotrackr/actions/workflows/deploy-core-infra.yml/badge.svg)](https://github.com/willvelida/biotrackr/actions/workflows/deploy-core-infra.yml) | N/A|
| Biotrackr.Auth.Svc| [![Deploy Auth Service](https://github.com/willvelida/biotrackr/actions/workflows/deploy-auth-service.yml/badge.svg)](https://github.com/willvelida/biotrackr/actions/workflows/deploy-auth-service.yml) | ![Code Coverage](https://img.shields.io/badge/Code%20Coverage-50%25-critical?style=flat) |
| Biotrackr.Activity.Svc | [![Deploy Activity Service](https://github.com/willvelida/biotrackr/actions/workflows/deploy-activity-service.yml/badge.svg)](https://github.com/willvelida/biotrackr/actions/workflows/deploy-activity-service.yml) | ![Code Coverage](https://img.shields.io/badge/Code%20Coverage-39%25-critical?style=flat) |
| Biotrackr.Activity.Api | [![Deploy Activity Api](https://github.com/willvelida/biotrackr/actions/workflows/deploy-activity-api.yml/badge.svg)](https://github.com/willvelida/biotrackr/actions/workflows/deploy-activity-api.yml) | ![Code Coverage](https://img.shields.io/badge/Code%20Coverage-43%25-critical?style=flat) |
| Biotrackr.Sleep.Api | [![Deploy Sleep Api](https://github.com/willvelida/biotrackr/actions/workflows/deploy-sleep-api.yml/badge.svg)](https://github.com/willvelida/biotrackr/actions/workflows/deploy-sleep-api.yml) | ![Code Coverage](https://img.shields.io/badge/Code%20Coverage-56%25-critical?style=flat) |
| Biotrackr.Sleep.Svc | [![Deploy Sleep Service](https://github.com/willvelida/biotrackr/actions/workflows/deploy-sleep-service.yml/badge.svg)](https://github.com/willvelida/biotrackr/actions/workflows/deploy-sleep-service.yml) | ![Code Coverage](https://img.shields.io/badge/Code%20Coverage-48%25-critical?style=flat) |
| Biotrackr.Weight.Api | [![Deploy Weight Api](https://github.com/willvelida/biotrackr/actions/workflows/deploy-weight-api.yml/badge.svg)](https://github.com/willvelida/biotrackr/actions/workflows/deploy-weight-api.yml) | ![Code Coverage](https://img.shields.io/badge/Code%20Coverage-39%25-critical?style=flat) |
| Biotrackr.Weight.Svc | [![Deploy Weight Service](https://github.com/willvelida/biotrackr/actions/workflows/deploy-weight-service.yml/badge.svg)](https://github.com/willvelida/biotrackr/actions/workflows/deploy-weight-service.yml) | ![Code Coverage](https://img.shields.io/badge/Code%20Coverage-41%25-critical?style=flat) | 