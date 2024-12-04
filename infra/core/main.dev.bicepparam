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
