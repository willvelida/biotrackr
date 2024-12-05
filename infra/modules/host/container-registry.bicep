metadata name = 'Azure Container Registry'
metadata description = 'This module deploys an Azure Container Registry. It also assigns the "AcrPull" role to a provided user-assigned managed identity'

@description('The name of the Container Registry')
@minLength(5)
@maxLength(50)
param name string

@description('The region where the Container Registry will be deployed')
@allowed([
  'australiaeast'
])
param location string

@description('The tags that will be deployed to the Container Registry')
param tags object

@description('The name of the user-assigned identity that this Container Registry will use')
@minLength(3)
@maxLength(128)
param uaiName string

@description('The name of the Log Analytics workspace that Cosmos DB will send diagnostic settings to')
@minLength(4)
@maxLength(63)
param logAnalyticsName string

var acrPullRoleDefintionId = subscriptionResourceId('Microsoft.Authorization/roleDefinitions', '7f951dda-4ed3-4680-a7ca-43fe172d538d')

resource uai 'Microsoft.ManagedIdentity/userAssignedIdentities@2023-01-31' existing = {
  name: uaiName
}

resource logAnalytics 'Microsoft.OperationalInsights/workspaces@2023-09-01' existing = {
  name: logAnalyticsName
}

resource acr 'Microsoft.ContainerRegistry/registries@2023-07-01' = {
  name: name
  location: location
  tags: tags
  sku: {
    name: 'Basic'
  }
  properties: {
    adminUserEnabled: true
  }
}

resource diagnosticLogs 'Microsoft.Insights/diagnosticSettings@2021-05-01-preview' = {
  name: acr.name
  scope: acr
  properties: {
    workspaceId: logAnalytics.id
    logs: [
      {
        category: 'ContainerRegistryRepositoryEvents'
        enabled: true
      }
      {
        category: 'ContainerRegistryLoginEvents'
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

resource acrPullRoleAssignment 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
  name: guid(resourceGroup().id, acr.id)
  properties: {
    principalId: uai.properties.principalId
    principalType: 'ServicePrincipal'
    roleDefinitionId: acrPullRoleDefintionId
  }
}

@description('The name of the deployed Azure Container Registry')
output acrName string = acr.name
