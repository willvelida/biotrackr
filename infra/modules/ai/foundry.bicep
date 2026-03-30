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
@allowed([
  'australiaeast'
])
param location string

@description('Tags to apply to all resources')
param tags object

@description('The name of the Application Insights instance to connect for trace correlation')
param appInsightsName string

@description('The name of the Log Analytics workspace for diagnostic settings')
param logAnalyticsName string

@description('The name of the user-assigned identity to grant Cognitive Services User role')
param uaiName string

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
