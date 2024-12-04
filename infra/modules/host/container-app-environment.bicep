@description('The name for the Container App Environment')
param name string

@description('The region that this Container App Environment will be deployed to')
param location string

@description('The tags that will be applied to this Container App Environment')
param tags object

@description('The name of the Log Analytics workspace that this Container App Environment will send logs to')
param logAnalyticsName string

resource logAnalytics 'Microsoft.OperationalInsights/workspaces@2023-09-01' existing = {
  name: logAnalyticsName
}

resource containerAppEnv 'Microsoft.App/managedEnvironments@2024-03-01' = {
  name: name
  location: location
  tags: tags
  properties: {
    appLogsConfiguration: {
      destination: 'log-analytics'
      logAnalyticsConfiguration: {
        customerId: logAnalytics.properties.customerId
        sharedKey: logAnalytics.listKeys().primarySharedKey
      }
    }
  }
}

@description('The name of the deployed Container App Environment')
output containerAppEnvName string = containerAppEnv.name
