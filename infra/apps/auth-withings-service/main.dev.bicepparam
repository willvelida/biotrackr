using 'main.bicep'

param name = 'biotrackr-auth-withings-svc-dev'
param imageName = ''
param location = 'australiaeast'
param tags = {
  ApplicationName: 'Biotrackr'
  Component: 'Auth-Withings-Svc'
  Environment: 'Dev'
}
param containerAppEnvironmentName = 'env-biotrackr-dev'
param containerRegistryName = 'acrbiotrackrdev'
param uaiName = 'uai-biotrackr-dev'
param keyVaultName = 'kv-biotrackr-dev'
param appInsightsName = 'appins-biotrackr-dev'
