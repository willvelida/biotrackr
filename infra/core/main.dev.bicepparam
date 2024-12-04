using 'main.bicep'

param tags = {
  ApplicationName: 'Biotrackr'
  Component: 'Core-Infra'
  Environment: 'Dev'
}
param appInsightsName = 'appins-biotrackr-dev'
param logAnalyticsName = 'law-biotrackr-dev'
param containerAppEnvName = 'env-biotrackr-dev'
param containerRegistryName = 'acrbiotrackrdev'
param uaiName = 'uai-biotrackr-dev'
param keyVaultName = 'kv-biotrackr-dev'
param appConfigName = 'config-biotrackr-dev'
param budgetName = 'budget-biotrackr-dev'
param emailAddress = 'willvelida@hotmail.co.uk'
param budgetLimit = 200
param firstBudgetThreshold = 100
param secondBudgetThreshold = 175
param budgetStartDate = '2024-12-01'
