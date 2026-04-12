# biotrackr

[![License](https://img.shields.io/badge/License-Apache%202.0-blue.svg)](LICENSE)
[![.NET](https://img.shields.io/badge/.NET-10.0-purple.svg)](https://dotnet.microsoft.com/)
[![Azure](https://img.shields.io/badge/Azure-Cloud-0078D4.svg)](https://azure.microsoft.com/)

**biotrackr** is a personal health platform that integrates with the Fitbit API and Withings API to collect, analyze, and provide insights on health and fitness data. The application follows a microservices architecture deployed on Azure, with comprehensive CI/CD pipelines and infrastructure as code.

## 📋 Table of Contents

- [Architecture](#-architecture)
- [Features](#-features)
- [Tech Stack](#-tech-stack)
- [Build Status](#-build-status)
- [License](#-license)

## 🏗️ Architecture

![](./docs/architecture-diagram.png)

The application follows a **microservices architecture** with separate services for different health domains:

### Data Ingestion (Background Workers)
Scheduled Container App Jobs that fetch data from the Fitbit and Withings APIs:
- **Auth Fitbit Service**: Manages OAuth token refresh with Fitbit API (every 6 hours), storing tokens in Azure Key Vault
- **Auth Withings Service**: Manages OAuth token refresh with Withings API (every 2 hours), storing rotating tokens in Azure Key Vault
- **Activity Service**: Daily fetch of physical activity and workout data from Fitbit
- **Sleep Service**: Daily fetch of sleep tracking and stage analysis data from Fitbit
- **Vitals Service**: Daily fetch of weight, blood pressure, and body composition data from Withings (muscle mass, bone mass, water mass, fat mass, fat-free mass, visceral fat index)
- **Food Service**: Daily fetch of nutrition and food logging data from Fitbit

### Data Access (REST APIs)
HTTP-based Container Apps serving data from Cosmos DB via Azure API Management:
- **Activity API**: Activity data endpoints (`/activity/*`)
- **Sleep API**: Sleep data endpoints (`/sleep/*`)
- **Vitals API**: Vitals data endpoints (`/vitals/*`)
- **Food API**: Food data endpoints (`/food/*`)
- **Reporting API**: Report generation and retrieval endpoints (`/reports/*`) with Copilot SDK lifecycle hooks (ASI02/ASI05), sub-agent specialization (data-analyst, chart-generator, pdf-builder), custom SKILL.md domain knowledge, agent-to-agent auth, and artifact review (ASI09)

### Consumer Layer
- **Chat API**: AI-powered chat agent (Claude via Microsoft Agent Framework) with AGUI SSE streaming, tool policy enforcement, conversation persistence, and graceful degradation when MCP tools are unavailable
- **MCP Server**: [Model Context Protocol](https://modelcontextprotocol.io/) server exposing 12 tools across all health domains via Streamable HTTP transport
- **Reporting API**: Generates PDF reports and chart images from health data using a GitHub Copilot coding agent sidecar with sub-agent orchestration (data-analyst → chart-generator → pdf-builder), custom skills for domain knowledge, lifecycle hooks for security and observability, and AI-driven report review
- **UI**: Blazor Server dashboard with Radzen components for visualizing activity, sleep, vitals, and food data

### Supporting Infrastructure
- **Azure API Management**: API gateway with JWT validation, subscription key auth, and rate limiting
- **Azure Cosmos DB**: Serverless NoSQL database for all health data and chat conversation history
- **Azure Key Vault**: Secure storage for Fitbit and Withings OAuth tokens
- **Azure App Configuration**: Centralized configuration for all services
- **Azure Container Registry**: Docker image storage
- **Azure Blob Storage**: Report artifact storage (PDFs, charts) with SAS URL generation
- **GitHub Copilot**: Coding agent sidecar for Reporting API with sub-agent orchestration (data-analyst, chart-generator, pdf-builder), custom SKILL.md domain knowledge files, SDK lifecycle hooks, and OpenTelemetry instrumentation
- **Managed Identity (UAI)**: Passwordless authentication across all Azure resources
- **Observability**: Application Insights, Log Analytics, OpenTelemetry (traces/metrics), Azure Monitor Alerts, Azure AI Foundry (evaluation)
- **Azure AI Foundry**: GenAIOps evaluation and monitoring — safety evaluators, groundedness checking, and evaluation pipeline via Foundry project in East US 2

## ✨ Features

- 🏃 **Activity Tracking**: Comprehensive workout and activity data collection
- 😴 **Sleep Analysis**: Sleep patterns, stages, and quality metrics
- ⚕️ **Vitals Tracking**: Weight, blood pressure, and body composition tracking with Withings data (muscle mass, bone mass, water mass, fat mass, visceral fat)
- 🍎 **Food Logging**: Nutrition tracking and food diary management
- 🔐 **Secure Authentication**: OAuth integration with Fitbit and Withings
- 📊 **Data Insights**: Analysis and reporting on health metrics
- 📝 **Report Generation**: Automated PDF reports and data visualizations via a Copilot coding agent with sub-agent specialization, custom skills, lifecycle hooks, and AI review
- 💬 **AI Chat Agent**: Natural language chat interface powered by Claude for querying and analysing health data
- 🤖 **MCP Integration**: AI-ready via Model Context Protocol server with 12 tools across all health domains
- �️ **Tool Policy Enforcement**: Per-session tool call limits, tool whitelisting, and rate limiting for AI agent safety
- 🔄 **Graceful Degradation**: Chat API continues operating when MCP tools are unavailable, rebuilding automatically when restored
- 💾 **Conversation Persistence**: Chat history stored in Cosmos DB with message limits and truncation for context management
- 🖥️ **Web Dashboard**: Blazor Server UI with Radzen components for browsing and visualizing health data
- 🧪 **AI Safety Evaluation**: Automated safety + groundedness evaluations via Azure AI Foundry with violence, self-harm, sexual content, and hate/unfairness detection
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
- **Azure AI Foundry**: GenAIOps evaluation pipeline with safety evaluators

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
| **Auth Fitbit Service** | [![Deploy Auth Fitbit Service](https://github.com/willvelida/biotrackr/actions/workflows/deploy-auth-fitbit-service.yml/badge.svg)](https://github.com/willvelida/biotrackr/actions/workflows/deploy-auth-fitbit-service.yml) | ![Code Coverage](https://img.shields.io/badge/Code%20Coverage-97.5%25-brightgreen?style=flat) | ![Integration Tests](https://img.shields.io/badge/Tests-19%20Passing-brightgreen?style=flat) |
| **Auth Withings Service** | [![Deploy Auth Withings Service](https://github.com/willvelida/biotrackr/actions/workflows/deploy-auth-withings-service.yml/badge.svg)](https://github.com/willvelida/biotrackr/actions/workflows/deploy-auth-withings-service.yml) | ![Code Coverage](https://img.shields.io/badge/Code%20Coverage-97.5%25-brightgreen?style=flat) | ![Integration Tests](https://img.shields.io/badge/Tests-19%20Passing-brightgreen?style=flat) |
| **Activity Service** | [![Deploy Activity Service](https://github.com/willvelida/biotrackr/actions/workflows/deploy-activity-service.yml/badge.svg)](https://github.com/willvelida/biotrackr/actions/workflows/deploy-activity-service.yml) | ![Code Coverage](https://img.shields.io/badge/Code%20Coverage-100%25-brightgreen?style=flat) | ![Integration Tests](https://img.shields.io/badge/Tests-17%20Passing-brightgreen?style=flat) |
| **Activity API** | [![Deploy Activity Api](https://github.com/willvelida/biotrackr/actions/workflows/deploy-activity-api.yml/badge.svg)](https://github.com/willvelida/biotrackr/actions/workflows/deploy-activity-api.yml) | ![Code Coverage](https://img.shields.io/badge/Code%20Coverage-79.3%25-yellow?style=flat) | ![Integration Tests](https://img.shields.io/badge/Tests-30%20Passing-brightgreen?style=flat) |
| **Sleep API** | [![Deploy Sleep Api](https://github.com/willvelida/biotrackr/actions/workflows/deploy-sleep-api.yml/badge.svg)](https://github.com/willvelida/biotrackr/actions/workflows/deploy-sleep-api.yml) | ![Code Coverage](https://img.shields.io/badge/Code%20Coverage-87%25-brightgreen?style=flat) | ![Integration Tests](https://img.shields.io/badge/Tests-19%20Passing-brightgreen?style=flat) |
| **Sleep Service** | [![Deploy Sleep Service](https://github.com/willvelida/biotrackr/actions/workflows/deploy-sleep-service.yml/badge.svg)](https://github.com/willvelida/biotrackr/actions/workflows/deploy-sleep-service.yml) | ![Code Coverage](https://img.shields.io/badge/Code%20Coverage-100%25-brightgreen?style=flat) | ![Integration Tests](https://img.shields.io/badge/Tests-16%20Passing-brightgreen?style=flat) |
| **Vitals API** | [![Deploy Vitals Api](https://github.com/willvelida/biotrackr/actions/workflows/deploy-vitals-api.yml/badge.svg)](https://github.com/willvelida/biotrackr/actions/workflows/deploy-vitals-api.yml) | ![Code Coverage](https://img.shields.io/badge/Code%20Coverage-75%25-yellow?style=flat) | ![Integration Tests](https://img.shields.io/badge/Tests-8%2F9%20Passing-success?style=flat) |
| **Vitals Service** | [![Deploy Vitals Service](https://github.com/willvelida/biotrackr/actions/workflows/deploy-vitals-service.yml/badge.svg)](https://github.com/willvelida/biotrackr/actions/workflows/deploy-vitals-service.yml) | ![Code Coverage](https://img.shields.io/badge/Code%20Coverage-100%25-brightgreen?style=flat) | ![Integration Tests](https://img.shields.io/badge/Tests-50%20Passing-brightgreen?style=flat) |
| **Food API** | [![Deploy Food Api](https://github.com/willvelida/biotrackr/actions/workflows/deploy-food-api.yml/badge.svg)](https://github.com/willvelida/biotrackr/actions/workflows/deploy-food-api.yml) | ![Code Coverage](https://img.shields.io/badge/Code%20Coverage-70%25-yellow?style=flat) | ![Integration Tests](https://img.shields.io/badge/Tests-26%20Passing-brightgreen?style=flat) |
| **Food Service** | [![Deploy Food Service](https://github.com/willvelida/biotrackr/actions/workflows/deploy-food-service.yml/badge.svg)](https://github.com/willvelida/biotrackr/actions/workflows/deploy-food-service.yml) | ![Code Coverage](https://img.shields.io/badge/Code%20Coverage-100%25-brightgreen?style=flat) | ![Integration Tests](https://img.shields.io/badge/Tests-14%20Passing-brightgreen?style=flat) |
| **Chat API** | [![Deploy Chat Api](https://github.com/willvelida/biotrackr/actions/workflows/deploy-chat-api.yml/badge.svg)](https://github.com/willvelida/biotrackr/actions/workflows/deploy-chat-api.yml) | ![Code Coverage](https://img.shields.io/badge/Code%20Coverage-86%25-brightgreen?style=flat) | ![Integration Tests](https://img.shields.io/badge/Tests-2%20Passing-brightgreen?style=flat) |
| **MCP Server** | [![Deploy MCP Server](https://github.com/willvelida/biotrackr/actions/workflows/deploy-mcp-server.yml/badge.svg)](https://github.com/willvelida/biotrackr/actions/workflows/deploy-mcp-server.yml) | ![Code Coverage](https://img.shields.io/badge/Code%20Coverage-76%25-yellow?style=flat) | ![Integration Tests](https://img.shields.io/badge/Tests-13%20Passing-brightgreen?style=flat) |
| **Reporting API** | [![Deploy Reporting Api](https://github.com/willvelida/biotrackr/actions/workflows/deploy-reporting-api.yml/badge.svg)](https://github.com/willvelida/biotrackr/actions/workflows/deploy-reporting-api.yml) | ![Code Coverage](https://img.shields.io/badge/Code%20Coverage-74%25-yellow?style=flat) | ![Integration Tests](https://img.shields.io/badge/Tests-182%20Passing-brightgreen?style=flat) |
| **AI Evaluation** | [![Evaluate AI Agents](https://github.com/willvelida/biotrackr/actions/workflows/evaluation.yml/badge.svg)](https://github.com/willvelida/biotrackr/actions/workflows/evaluation.yml) | N/A | N/A |
| **UI** | [![Deploy UI](https://github.com/willvelida/biotrackr/actions/workflows/deploy-ui.yml/badge.svg)](https://github.com/willvelida/biotrackr/actions/workflows/deploy-ui.yml) | ![Code Coverage](https://img.shields.io/badge/Tests-177%20Passing-brightgreen?style=flat) | N/A |

##  License

This project is licensed under the Apache License 2.0 - see the [LICENSE](LICENSE) file for details.

---

**Author**: [willvelida](https://github.com/willvelida)

*For questions or feedback, please open an issue on this repository.*