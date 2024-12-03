targetScope = 'subscription'

@description('The name of the resource group that all resources will be deployed to')
param rgName string

@description('The location where all resources will be deployed to')
param location string

@description('The tags that will be applied to all resources')
param tags object

@description('The name of the Application Insights instance')
param appInsightsName string

@description('The name of the Log Analytics workspace')
param logAnalyticsName string

@description('The name of the Container App Environment')
param containerAppEnvName string

resource rg 'Microsoft.Resources/resourceGroups@2024-07-01' = {
  name: rgName
  location: location
  tags: tags
}

module logAnalytics '../modules/monitoring/log-analytics.bicep' = {
  scope: rg
  name: 'log-analytics'
  params: {
    name: logAnalyticsName
    location: logAnalyticsName
    tags: tags
  }
}

module appInsights '../modules/monitoring/app-insights.bicep' = {
  scope: rg
  name: 'app-insights'
  params: {
    name: appInsightsName
    location: location
    tags: tags
    logAnalyticsName: logAnalytics.outputs.logAnalyticsName
  }
}

module containerAppEnv '../modules/host/container-app-environment.bicep' = {
  scope: rg
  name: 'container-app-env'
  params: {
    name: containerAppEnvName
    location: location
    tags: tags
    logAnalyticsName: logAnalytics.outputs.logAnalyticsName
  }
}
