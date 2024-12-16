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

@description('The name of the Container Registry')
param containerRegistryName string

@description('The name of the User Assigned Identity')
param uaiName string

@description('The name of the Key Vault')
param keyVaultName string

@description('The name of the App Configuration')
param appConfigName string

@description('The name of the budget')
param budgetName string

@description('The start date of the budget')
param budgetStartDate string

@description('The email address to use for resources')
param emailAddress string

@description('The limit of the budget')
param budgetLimit int

@description('The first budget threshold')
param firstBudgetThreshold int

@description('The second budget threshold')
param secondBudgetThreshold int

@description('The name of the Cosmos DB account')
param cosmosAccountName string

@description('The name of the Cosmos DB database')
param cosmosDatabaseName string

@description('The name of the Cosmos DB container')
param cosmosContainerName string

@description('The name of the API Management Instance')
param apimName string

@description('The name of the owner of this API Management instance')
param publisherName string

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

module uai '../modules/identity/user-assigned-identity.bicep' = {
  name: 'user-assigned-identity'
  params: {
    name: uaiName
    location: location
    tags: tags
  }
}

module acr '../modules/host/container-registry.bicep' = {
  name: 'acr'
  params: {
    name: containerRegistryName
    location: location
    tags: tags
    uaiName: uai.outputs.uaiName
    logAnalyticsName: logAnalytics.outputs.logAnalyticsName
  }
}

module keyVault '../modules/security/key-vault.bicep' = {
  name: 'key-vault'
  params: {
    name: keyVaultName
    location: location
    tags: tags
    uaiName: uai.outputs.uaiName
    logAnalyticsName: logAnalytics.outputs.logAnalyticsName
  }
}

module appConfig '../modules/configuration/azure-app-config.bicep' = {
  name: 'app-config'
  params: {
    name: appConfigName
    location: location
    tags: tags
    uaiName: uai.outputs.uaiName
    logAnalyticsName: logAnalytics.outputs.logAnalyticsName
  }
}

module budget '../modules/monitoring/budget.bicep' = {
  name: 'budget'
  params: {
    name: budgetName
    amount: budgetLimit
    firstThreshold: firstBudgetThreshold
    ownerEmail: emailAddress
    secondThreshold: secondBudgetThreshold
    startDate: budgetStartDate
  }
}

// Updating RBAC flag
module cosmos '../modules/database/serverless-cosmos-db.bicep' = {
  name: 'cosmos'
  params: {
    location: location
    tags: tags
    accountName: cosmosAccountName 
    appConfigName: appConfig.outputs.appConfigName
    databaseName: cosmosDatabaseName
    uaiName: uai.outputs.uaiName
    logAnalyticsName: logAnalytics.outputs.logAnalyticsName
    containerName: cosmosContainerName
  }
}

module apim '../modules/integration/apim-consumption.bicep' = {
  name: 'apim'
  params: {
    name: apimName
    location: location
    tags: tags
    emailAddress: emailAddress
    publisherName: publisherName 
    uaiName: uai.outputs.uaiName
    appInsightsName: appInsights.outputs.appInsightsName
    logAnalyticsName: logAnalytics.outputs.logAnalyticsName
  }
}
