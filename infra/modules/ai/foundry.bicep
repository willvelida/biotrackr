metadata name = 'Azure AI Foundry'
metadata description = 'This module deploys an Azure AI Foundry hub account and project for GenAIOps (evaluation, monitoring, tracing).'

@description('The name of the AI Foundry resource')
@minLength(2)
@maxLength(64)
param name string

@description('The name of the Foundry project')
@minLength(2)
@maxLength(64)
param projectName string

@description('The location for all resources')
param location string

@description('Tags to apply to all resources')
param tags object

@description('The name of the Application Insights instance to connect for trace correlation')
param appInsightsName string

@description('The name of the Log Analytics workspace for diagnostic settings')
param logAnalyticsName string

@description('The name of the user-assigned identity to grant Cognitive Services User role')
param uaiName string

@description('The name of the evaluation storage account for dataset uploads')
@minLength(3)
@maxLength(24)
param evaluationStorageAccountName string

resource uai 'Microsoft.ManagedIdentity/userAssignedIdentities@2023-01-31' existing = {
  name: uaiName
}

resource appInsights 'Microsoft.Insights/components@2020-02-02' existing = {
  name: appInsightsName
}

resource logAnalytics 'Microsoft.OperationalInsights/workspaces@2023-09-01' existing = {
  name: logAnalyticsName
}

resource foundryAccount 'Microsoft.CognitiveServices/accounts@2025-09-01' = {
  name: name
  location: location
  tags: tags
  kind: 'AIServices'
  identity: {
    type: 'SystemAssigned'
  }
  sku: {
    name: 'S0'
  }
  properties: {
    customSubDomainName: name
    publicNetworkAccess: 'Enabled'
    allowProjectManagement: true
  }
}

resource foundryProject 'Microsoft.CognitiveServices/accounts/projects@2025-09-01' = {
  parent: foundryAccount
  name: projectName
  location: location
  tags: tags
  identity: {
    type: 'SystemAssigned'
  }
  properties: {
    displayName: projectName
    description: 'Biotrackr GenAIOps — evaluation, monitoring, and tracing for Claude-powered agents'
  }
}

resource diagnosticLogs 'Microsoft.Insights/diagnosticSettings@2021-05-01-preview' = {
  name: foundryAccount.name
  scope: foundryAccount
  properties: {
    workspaceId: logAnalytics.id
    logs: [
      {
        category: 'Audit'
        enabled: true
      }
      {
        category: 'RequestResponse'
        enabled: true
      }
    ]
    metrics: [
      {
        category: 'AllMetrics'
        enabled: true
      }
    ]
  }
}

// Cognitive Services User — enables Chat.Api to send correlated traces and future SDK calls
resource cognitiveServicesUserRole 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
  name: guid(subscription().id, resourceGroup().id, foundryAccount.id, 'CognitiveServicesUser')
  scope: foundryAccount
  properties: {
    principalId: uai.properties.principalId
    roleDefinitionId: subscriptionResourceId('Microsoft.Authorization/roleDefinitions', 'a97b65f3-24c7-4388-baec-2e87135dc908')
    principalType: 'ServicePrincipal'
  }
}

// --- Evaluation Storage ---
// Dedicated storage account for Foundry evaluation dataset uploads.
// The SDK's UploadFileAsync requires a connected storage resource on the project.
resource evaluationStorage 'Microsoft.Storage/storageAccounts@2024-01-01' = {
  name: evaluationStorageAccountName
  location: location
  tags: tags
  kind: 'StorageV2'
  sku: {
    name: 'Standard_LRS'
  }
  properties: {
    accessTier: 'Hot'
    allowBlobPublicAccess: false
    minimumTlsVersion: 'TLS1_2'
    supportsHttpsTrafficOnly: true
  }
}

resource evaluationBlobService 'Microsoft.Storage/storageAccounts/blobServices@2024-01-01' = {
  name: 'default'
  parent: evaluationStorage
}

resource evaluationDatasetsContainer 'Microsoft.Storage/storageAccounts/blobServices/containers@2024-01-01' = {
  name: 'evaluation-datasets'
  parent: evaluationBlobService
  properties: {
    publicAccess: 'None'
  }
}

// Storage Blob Data Contributor for the Foundry account's system-assigned identity
// so the SDK can upload datasets via PendingUpload
var storageBlobDataContributorRoleId = 'ba92f5b4-2d11-453d-a403-e96b0029c9fe'

resource foundryStorageRole 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
  name: guid(evaluationStorage.id, foundryAccount.id, storageBlobDataContributorRoleId)
  scope: evaluationStorage
  properties: {
    principalId: foundryAccount.identity.principalId
    roleDefinitionId: subscriptionResourceId('Microsoft.Authorization/roleDefinitions', storageBlobDataContributorRoleId)
    principalType: 'ServicePrincipal'
  }
}

// Storage Blob Data Contributor for the project's system-assigned identity
resource projectStorageRole 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
  name: guid(evaluationStorage.id, foundryProject.id, storageBlobDataContributorRoleId)
  scope: evaluationStorage
  properties: {
    principalId: foundryProject.identity.principalId
    roleDefinitionId: subscriptionResourceId('Microsoft.Authorization/roleDefinitions', storageBlobDataContributorRoleId)
    principalType: 'ServicePrincipal'
  }
}

// NOTE: The Foundry project connection for evaluation storage is managed out-of-band
// via CLI (az rest) because the ARM API only accepts category 'AzureBlob' but the
// SDK data plane (AzureML asset store) only accepts 'AzureBlobStorage'. This is a
// known platform category mapping mismatch. The connection was created manually:
//   az rest --method put --url ".../connections/evaluation-storage?api-version=2025-09-01"
//   --body "{'properties':{'category':'AzureBlob','target':'https://stbiotrackrevaldev.blob.core.windows.net/',...}}"

// Connect Application Insights to the Foundry project for trace correlation and monitoring
resource appInsightsConnection 'Microsoft.CognitiveServices/accounts/projects/connections@2025-09-01' = {
  parent: foundryProject
  name: 'app-insights'
  properties: {
    category: 'AppInsights'
    target: appInsights.properties.ConnectionString
    authType: 'ApiKey'
    credentials: {
      key: appInsights.properties.InstrumentationKey
    }
    metadata: {
      ResourceId: appInsights.id
    }
  }
}

@description('The name of the deployed AI Foundry account')
output foundryAccountName string = foundryAccount.name

@description('The resource ID of the AI Foundry account')
output foundryAccountId string = foundryAccount.id

@description('The endpoint of the AI Foundry account')
output foundryEndpoint string = foundryAccount.properties.endpoint

@description('The name of the deployed Foundry project')
output foundryProjectName string = foundryProject.name

@description('The Application Insights resource ID (for portal connection)')
output appInsightsResourceId string = appInsights.id

@description('The project endpoint for SDK calls (e.g., evaluations, datasets)')
output foundryProjectEndpoint string = foundryProject.properties.endpoints['AI Foundry API']

@description('The name of the evaluation storage account')
output evaluationStorageAccountName string = evaluationStorage.name
