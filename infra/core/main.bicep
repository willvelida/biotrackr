@description('The location where all resources will be deployed to')
param location string = resourceGroup().location

@description('The tags that will be applied to all resources')
param tags object

@description('The name of the Application Insights instance')
param appInsightsName string

@description('The name of the Log Analytics workspace')
param logAnalyticsName string

@description('The name of the Container App Environment')
param containerAppEnvName string

module logAnalytics '../modules/monitoring/log-analytics.bicep' = {
  name: 'log-analytics'
  params: {
    name: logAnalyticsName
    location: location
    tags: tags
  }
}

module appInsights '../modules/monitoring/app-insights.bicep' = {
  name: 'app-insights'
  params: {
    name: appInsightsName
    location: location
    tags: tags
    logAnalyticsName: logAnalytics.outputs.logAnalyticsName
  }
}

module containerAppEnv '../modules/host/container-app-environment.bicep' = {
  name: 'container-app-env'
  params: {
    name: containerAppEnvName
    location: location
    tags: tags
    logAnalyticsName: logAnalytics.outputs.logAnalyticsName
  }
}
