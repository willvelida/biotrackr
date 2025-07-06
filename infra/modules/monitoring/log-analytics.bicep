metadata name = 'Log Analytics Workspace'
metadata description = 'This module deploys a Log Analytics workspace.'

@description('The base name given to all resources')
@minLength(5)
@maxLength(50)
param baseName string

@description('The environment that the Log Analytics workspace will be deployed to')
param environment string

@description('The location that the Log Analytics workspace will be deployed to')
@allowed([
  'australiaeast'
])
param location string

@description('The tags that will be applied to the Log Analytics workspace')
param tags object

var lawName = 'law-${baseName}-${environment}'

resource logAnalytics 'Microsoft.OperationalInsights/workspaces@2023-09-01' = {
  name: lawName
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
