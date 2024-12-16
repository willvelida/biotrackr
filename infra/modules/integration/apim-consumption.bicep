metadata name = 'API Management'
metadata description = 'Deploys a API Management instance using the Consumption SKU'

@description('The name of the API Management instance')
param name string

@description('The region where the API Management instance will be deployed')
param location string

@description('The tags that will be applied to the API Management Instance')
param tags object

@description('The email address of the owner of this API Management instance')
param emailAddress string

@description('The name of the owner of this API Management instance')
param publisherName string

@description('The name of the user-assigned identity that this API Management instance will use')
param uaiName string

@description('The name of the App Insights instance that this API Management instance sends logs to')
param appInsightsName string

@description('The name of the Log Analytics workspace that will be used for diagnostic logs')
param logAnalyticsName string

resource uai 'Microsoft.ManagedIdentity/userAssignedIdentities@2023-01-31' existing = {
  name: uaiName
}

resource appInsights 'Microsoft.Insights/components@2020-02-02' existing = {
  name: appInsightsName
}

resource logAnalytics 'Microsoft.OperationalInsights/workspaces@2023-09-01' existing = {
  name: logAnalyticsName
}

resource apiManagement 'Microsoft.ApiManagement/service@2024-05-01' = {
  name: name
  location: location
  tags: tags
  sku: {
    name: 'Consumption'
    capacity: 0
  }
  properties: {
    publisherEmail: emailAddress
    publisherName: publisherName
  }
  identity: {
    type: 'UserAssigned'
    userAssignedIdentities: {
      '${uai.id}': {}
    }
  }
}

resource apiLogger 'Microsoft.ApiManagement/service/loggers@2023-09-01-preview' = {
  name: '${appInsightsName}-apim'
  parent: apiManagement
  properties: {
    loggerType: 'applicationInsights'
    description: 'APIM Logger resources to APIM'
    credentials: {
      connectionString: appInsights.properties.ConnectionString
    }
  }
}

resource diagnosticSettings 'Microsoft.Insights/diagnosticSettings@2021-05-01-preview' = {
  name: 'apiManagementDiagnosticSettings'
  scope: apiManagement
  properties: {
    workspaceId: logAnalytics.id
    metrics: [
      {
        category: 'AllMetrics'
        enabled: true
      }
    ]
  }
}

@description('The name of the deployed API Management Instance')
output name string = apiManagement.name
