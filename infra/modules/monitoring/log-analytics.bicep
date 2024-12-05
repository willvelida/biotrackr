metadata name = 'Log Analytics Workspace'
metadata description = 'This module deploys a Log Analytics workspace.'

@description('The name of the Log Analytics workspace')
@minLength(4)
@maxLength(63)
param name string

@description('The location that the Log Analytics workspace will be deployed to')
@allowed([
  'australiaeast'
])
param location string

@description('The tags that will be applied to the Log Analytics workspace')
param tags object

resource logAnalytics 'Microsoft.OperationalInsights/workspaces@2023-09-01' = {
  name: name
  location: location
  tags: tags
  properties: {
    retentionInDays: 30
    publicNetworkAccessForIngestion: 'Enabled'
    publicNetworkAccessForQuery: 'Enabled'
    features: {
      searchVersion: 1
    }
    sku: {
      name: 'PerGB2018'
    }
  }
}

@description('The name of the deployed Log Analytics workspace')
output logAnalyticsName string = logAnalytics.name
