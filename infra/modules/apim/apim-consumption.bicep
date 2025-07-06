metadata name = 'API Management'
metadata description = 'Deploys a API Management instance using the Consumption SKU'

@description('The base name given to all resources')
@minLength(5)
@maxLength(50)
param baseName string

@description('The environment that the Api Management instance will be deployed to')
param environment string

@description('The region where the API Management instance will be deployed')
@allowed(['australiaeast'])
param location string

@description('The tags that will be applied to the API Management Instance')
param tags object

@description('The email address of the owner of this API Management instance')
@minLength(5)
@maxLength(50)
param emailAddress string = 'willvelida@hotmail.co.uk'

@description('The name of the owner of this API Management instance')
@minLength(1)
@maxLength(100)
param publisherName string = 'Will Velida'

@description('The name of the user-assigned identity that this API Management instance will use')
@minLength(3)
@maxLength(128)
param uaiName string

@description('The name of the App Insights instance that this API Management instance sends logs to')
@minLength(4)
@maxLength(50)
param appInsightsName string

@description('The name of the Log Analytics workspace that will be used for diagnostic logs')
@minLength(4)
@maxLength(63)
param logAnalyticsName string

var apimName = 'api-${baseName}-${environment}'
var productNames = [
  'Activity'
  'Sleep'
  'Weight'
]

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
  name: apimName
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

module products 'apim-products.bicep' = [for name in productNames: {
  name: '${name}-product'
  params: {
    apimName: apiManagement.name
    productName: name
  }
}]

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
