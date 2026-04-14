using 'main.bicep'

param name = 'biotrackr-reporting-svc-monthly-dev'
param imageName = ''
param location = 'australiaeast'
param tags = {
  ApplicationName: 'Biotrackr'
  Component: 'Reporting-Svc-Monthly'
  Environment: 'Dev'
}
param containerAppEnvironmentName = 'env-biotrackr-dev'
param containerRegistryName = 'acrbiotrackrdev'
param uaiName = 'uai-biotrackr-dev'
param keyVaultName = 'kv-biotrackr-dev'
param appInsightsName = 'appins-biotrackr-dev'
param appConfigName = 'config-biotrackr-dev'
