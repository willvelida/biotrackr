# biotrackr

[![License](https://img.shields.io/badge/License-Apache%202.0-blue.svg)](LICENSE)
[![.NET](https://img.shields.io/badge/.NET-10.0-purple.svg)](https://dotnet.microsoft.com/)
[![Azure](https://img.shields.io/badge/Azure-Cloud-0078D4.svg)](https://azure.microsoft.com/)

**biotrackr** is a personal health platform that integrates with the Fitbit API to collect, analyze, and provide insights on health and fitness data. The application follows a microservices architecture deployed on Azure, with comprehensive CI/CD pipelines and infrastructure as code.

## 📋 Table of Contents

- [Architecture](#-architecture)
- [Features](#-features)
- [Tech Stack](#-tech-stack)
- [Build Status](#-build-status)
- [License](#-license)

## 🏗️ Architecture

![](./docs/architecture-diagram.png)

The application follows a **microservices architecture** with separate services for different health domains:

- **Activity Service**: Processes and stores physical activity data from Fitbit
- **Sleep Service**: Manages sleep tracking and analysis
- **Weight Service**: Handles weight measurements and trends
- **Food Service**: Tracks nutrition and food logging data from Fitbit
- **Auth Service**: Manages authentication and authorization with Fitbit API
- **Chat API**: AI-powered chat agent for querying and analysing health data via natural language
- **MCP Server**: [Model Context Protocol](https://modelcontextprotocol.io/) server exposing all health data as MCP tools for AI assistants
- **UI**: Blazor Server dashboard for visualizing activity, sleep, weight, and food data

Each service consists of:
- **API Layer**: RESTful endpoints for data access
- **Service Layer**: Business logic and integration with Fitbit API
- **Data Layer**: Azure Cosmos DB for persistence

## ✨ Features

- 🏃 **Activity Tracking**: Comprehensive workout and activity data collection
- 😴 **Sleep Analysis**: Sleep patterns, stages, and quality metrics
- ⚖️ **Weight Management**: Weight tracking and trend visualization
- 🍎 **Food Logging**: Nutrition tracking and food diary management
- 🔐 **Secure Authentication**: OAuth integration with Fitbit
- 📊 **Data Insights**: Analysis and reporting on health metrics
- 💬 **AI Chat Agent**: Natural language chat interface powered by Claude for querying and analysing health data
- 🤖 **MCP Integration**: AI-ready via Model Context Protocol server with 12 tools across all health domains
- 🖥️ **Web Dashboard**: Blazor Server UI for browsing and visualizing health data
- ☁️ **Cloud-Native**: Fully deployed on Azure with auto-scaling
- 🚀 **CI/CD**: Automated testing, deployment, and infrastructure management

## 🛠️ Tech Stack

### Backend
- **.NET 10.0**: Modern C# microservices
- **Azure Container Apps**: Serverless compute for background processing
- **Azure Cosmos DB**: NoSQL database for scalable data storage
- **Azure App Configuration**: Centralized configuration management
- **Azure Key Vault**: Secure secrets management
- **Microsoft Agent Framework**: AI agent orchestration with Claude (Anthropic) as the LLM backend

### Infrastructure
- **Bicep**: Infrastructure as Code (IaC) for Azure resources
- **GitHub Actions**: CI/CD pipelines and workflow automation
- **Docker**: Containerization for consistent environments
- **Azure API Management**: API gateway with JWT validation for secure managed identity authentication
- **ModelContextProtocol SDK**: MCP server with Streamable HTTP transport

### Frontend
- **Blazor Server**: Interactive server-rendered UI with .NET 10.0
- **OpenTelemetry**: Distributed tracing and metrics

### Testing
- **xUnit**: Unit and integration testing framework
- **FluentAssertions**: Readable test assertions
- **Moq**: Mocking framework for unit tests
- **Cosmos DB Emulator**: Local database testing

##  Build Status

| Component | Deployment Status | Unit Test Coverage | Integration Test Coverage |
| --------- | ----------------- | ------------------ | ------------------------- |
| **Infrastructure** | [![Deploy Core Biotrackr Infrastructure](https://github.com/willvelida/biotrackr/actions/workflows/deploy-core-infra.yml/badge.svg)](https://github.com/willvelida/biotrackr/actions/workflows/deploy-core-infra.yml) | N/A | N/A |
| **Auth Service** | [![Deploy Auth Service](https://github.com/willvelida/biotrackr/actions/workflows/deploy-auth-service.yml/badge.svg)](https://github.com/willvelida/biotrackr/actions/workflows/deploy-auth-service.yml) | ![Code Coverage](https://img.shields.io/badge/Code%20Coverage-97.5%25-brightgreen?style=flat) | ![Integration Tests](https://img.shields.io/badge/Tests-14%20Passing-brightgreen?style=flat) |
| **Activity Service** | [![Deploy Activity Service](https://github.com/willvelida/biotrackr/actions/workflows/deploy-activity-service.yml/badge.svg)](https://github.com/willvelida/biotrackr/actions/workflows/deploy-activity-service.yml) | ![Code Coverage](https://img.shields.io/badge/Code%20Coverage-100%25-brightgreen?style=flat) | ![Integration Tests](https://img.shields.io/badge/Tests-17%20Passing-brightgreen?style=flat) |
| **Activity API** | [![Deploy Activity Api](https://github.com/willvelida/biotrackr/actions/workflows/deploy-activity-api.yml/badge.svg)](https://github.com/willvelida/biotrackr/actions/workflows/deploy-activity-api.yml) | ![Code Coverage](https://img.shields.io/badge/Code%20Coverage-79.3%25-yellow?style=flat) | ![Integration Tests](https://img.shields.io/badge/Tests-30%20Passing-brightgreen?style=flat) |
| **Sleep API** | [![Deploy Sleep Api](https://github.com/willvelida/biotrackr/actions/workflows/deploy-sleep-api.yml/badge.svg)](https://github.com/willvelida/biotrackr/actions/workflows/deploy-sleep-api.yml) | ![Code Coverage](https://img.shields.io/badge/Code%20Coverage-87%25-brightgreen?style=flat) | ![Integration Tests](https://img.shields.io/badge/Tests-19%20Passing-brightgreen?style=flat) |
| **Sleep Service** | [![Deploy Sleep Service](https://github.com/willvelida/biotrackr/actions/workflows/deploy-sleep-service.yml/badge.svg)](https://github.com/willvelida/biotrackr/actions/workflows/deploy-sleep-service.yml) | ![Code Coverage](https://img.shields.io/badge/Code%20Coverage-100%25-brightgreen?style=flat) | ![Integration Tests](https://img.shields.io/badge/Tests-16%20Passing-brightgreen?style=flat) |
| **Weight API** | [![Deploy Weight Api](https://github.com/willvelida/biotrackr/actions/workflows/deploy-weight-api.yml/badge.svg)](https://github.com/willvelida/biotrackr/actions/workflows/deploy-weight-api.yml) | ![Code Coverage](https://img.shields.io/badge/Code%20Coverage-75%25-yellow?style=flat) | ![Integration Tests](https://img.shields.io/badge/Tests-8%2F9%20Passing-success?style=flat) |
| **Weight Service** | [![Deploy Weight Service](https://github.com/willvelida/biotrackr/actions/workflows/deploy-weight-service.yml/badge.svg)](https://github.com/willvelida/biotrackr/actions/workflows/deploy-weight-service.yml) | ![Code Coverage](https://img.shields.io/badge/Code%20Coverage-100%25-brightgreen?style=flat) | ![Integration Tests](https://img.shields.io/badge/Tests-4%20Passing-brightgreen?style=flat) |
| **Food API** | [![Deploy Food Api](https://github.com/willvelida/biotrackr/actions/workflows/deploy-food-api.yml/badge.svg)](https://github.com/willvelida/biotrackr/actions/workflows/deploy-food-api.yml) | ![Code Coverage](https://img.shields.io/badge/Code%20Coverage-70%25-yellow?style=flat) | ![Integration Tests](https://img.shields.io/badge/Tests-26%20Passing-brightgreen?style=flat) |
| **Food Service** | [![Deploy Food Service](https://github.com/willvelida/biotrackr/actions/workflows/deploy-food-service.yml/badge.svg)](https://github.com/willvelida/biotrackr/actions/workflows/deploy-food-service.yml) | ![Code Coverage](https://img.shields.io/badge/Code%20Coverage-100%25-brightgreen?style=flat) | ![Integration Tests](https://img.shields.io/badge/Tests-14%20Passing-brightgreen?style=flat) |
| **Chat API** | [![Deploy Chat Api](https://github.com/willvelida/biotrackr/actions/workflows/deploy-chat-api.yml/badge.svg)](https://github.com/willvelida/biotrackr/actions/workflows/deploy-chat-api.yml) | ![Code Coverage](https://img.shields.io/badge/Code%20Coverage-86%25-brightgreen?style=flat) | ![Integration Tests](https://img.shields.io/badge/Tests-2%20Passing-brightgreen?style=flat) |
| **MCP Server** | [![Deploy MCP Server](https://github.com/willvelida/biotrackr/actions/workflows/deploy-mcp-server.yml/badge.svg)](https://github.com/willvelida/biotrackr/actions/workflows/deploy-mcp-server.yml) | ![Code Coverage](https://img.shields.io/badge/Code%20Coverage-76%25-yellow?style=flat) | ![Integration Tests](https://img.shields.io/badge/Tests-13%20Passing-brightgreen?style=flat) |
| **UI** | [![Deploy UI](https://github.com/willvelida/biotrackr/actions/workflows/deploy-ui.yml/badge.svg)](https://github.com/willvelida/biotrackr/actions/workflows/deploy-ui.yml) | N/A | N/A |

##  License

This project is licensed under the Apache License 2.0 - see the [LICENSE](LICENSE) file for details.

---

**Author**: [willvelida](https://github.com/willvelida)

*For questions or feedback, please open an issue on this repository.*