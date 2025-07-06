@description('The location where all resources will be deployed to')
param location string = resourceGroup().location

@description('The base name that will be given to all resources')
param baseName string

@description('The environment that these resources will be deployed to')
@allowed([
  'dev'
  'prod'
])
param environment string = 'dev'

@description('The tags that will be applied to all resources')
param tags object

module logAnalytics '../modules/monitoring/log-analytics.bicep' = {
  name: 'log-analytics'
  params: {
    baseName: baseName
    environment: environment
    location: location
    tags: tags
  }
}

module appInsights '../modules/monitoring/app-insights.bicep' = {
  name: 'app-insights'
  params: {
    baseName: baseName
    environment: environment
    location: location
    tags: tags
    logAnalyticsName: logAnalytics.outputs.logAnalyticsName
  }
}

module containerAppEnv '../modules/host/container-app-environment.bicep' = {
  name: 'container-app-env'
  params: {
    baseName: baseName
    environment: environment
    location: location
    tags: tags
    logAnalyticsName: logAnalytics.outputs.logAnalyticsName
  }
}

module uai '../modules/identity/user-assigned-identity.bicep' = {
  name: 'user-assigned-identity'
  params: {
    baseName: baseName
    environment: environment
    location: location
    tags: tags
  }
}

module acr '../modules/host/container-registry.bicep' = {
  name: 'acr'
  params: {
    baseName: baseName
    environment: environment
    location: location
    tags: tags
    uaiName: uai.outputs.uaiName
    logAnalyticsName: logAnalytics.outputs.logAnalyticsName
  }
}

module keyVault '../modules/security/key-vault.bicep' = {
  name: 'key-vault'
  params: {
    baseName: baseName
    environment: environment
    location: location
    tags: tags
    uaiName: uai.outputs.uaiName
    logAnalyticsName: logAnalytics.outputs.logAnalyticsName
  }
}

module appConfig '../modules/configuration/azure-app-config.bicep' = {
  name: 'app-config'
  params: {
    baseName: baseName
    environment: environment
    location: location
    tags: tags
    uaiName: uai.outputs.uaiName
    logAnalyticsName: logAnalytics.outputs.logAnalyticsName
  }
}

module budget '../modules/monitoring/budget.bicep' = {
  name: 'budget'
  params: {
    baseName: baseName
    environment: environment
  }
}

module cosmos '../modules/database/serverless-cosmos-db.bicep' = {
  name: 'cosmos'
  params: {
    location: location
    tags: tags
    baseName: baseName
    environment: environment
    appConfigName: appConfig.outputs.appConfigName
    uaiName: uai.outputs.uaiName
    logAnalyticsName: logAnalytics.outputs.logAnalyticsName
  }
}

module apim '../modules/integration/apim-consumption.bicep' = {
  name: 'apim'
  params: {
    baseName: baseName
    environment: environment
    location: location
    tags: tags
    uaiName: uai.outputs.uaiName
    appInsightsName: appInsights.outputs.appInsightsName
    logAnalyticsName: logAnalytics.outputs.logAnalyticsName
  }
}
