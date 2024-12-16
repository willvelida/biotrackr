using 'main.bicep'

param name = 'biotrackr-weight-svc-dev'
param imageName = ''
param location = 'australiaeast'
param tags = {
  ApplicationName: 'Biotrackr'
  Component: 'Weight-Svc'
  Environment: 'Dev'
}
param containerAppEnvironmentName = 'env-biotrackr-dev'
param containerRegistryName = 'acrbiotrackrdev'
param uaiName = 'uai-biotrackr-dev'
param keyVaultName = 'kv-biotrackr-dev'
param appInsightsName = 'appins-biotrackr-dev'
param appConfigName = 'config-biotrackr-dev'
param cosmosDbAccountName = 'cosmos-biotrackr-dev'
