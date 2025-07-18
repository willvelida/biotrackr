metadata name = 'Application Insights instance'
metadata description = 'This module deploys an Application Insights instance. It also integrates with a provided Log Analytics workspace'

@description('The base name given to all resources')
@minLength(5)
@maxLength(50)
param baseName string

@description('The environment that the Application Insights instance will be deployed to')
param environment string

@description('The location that our Application Insights will be deployed to')
@allowed([
  'australiaeast'
])
param location string

@description('The name of the Log Analytics workspace that this Application Insights will integrate with')
@minLength(4)
@maxLength(63)
param logAnalyticsName string

@description('The tags that will be applied to the Application Insights instance')
param tags object

var appInsightsName = 'appins-${baseName}-${environment}'

resource logAnalytics 'Microsoft.OperationalInsights/workspaces@2023-09-01' existing = {
  name: logAnalyticsName
}

resource appInsights 'Microsoft.Insights/components@2020-02-02' = {
  name: appInsightsName
  location: location
  tags: tags
  kind: 'web'
  properties: {
    Application_Type: 'web'
    publicNetworkAccessForIngestion: 'Enabled'
    publicNetworkAccessForQuery: 'Enabled'
    WorkspaceResourceId: logAnalytics.id
  }
}

@description('The name of the deployed Application Insights instance')
output appInsightsName string = appInsights.name
