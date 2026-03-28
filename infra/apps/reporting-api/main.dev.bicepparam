using 'main.bicep'

param name = 'biotrackr-reporting-api-dev'
param imageName = 'mcr.microsoft.com/k8se/quickstart:latest'
param sidecarImageName = 'mcr.microsoft.com/k8se/quickstart:latest'
param location = 'australiaeast'
param tags = {
  ApplicationName: 'Biotrackr'
  Component: 'Reporting-Api'
  Environment: 'Dev'
}
param containerAppEnvironmentName = 'env-biotrackr-dev'
param containerRegistryName = 'acrbiotrackrdev'
param uaiName = 'uai-biotrackr-dev'
param appConfigName = 'config-biotrackr-dev'
param appInsightsName = 'appins-biotrackr-dev'
param apimName = 'api-biotrackr-dev'
param keyVaultName = 'kv-biotrackr-dev'
param enableManagedIdentityAuth = true
param tenantId = ''
param storageAccountName = 'stbiotrackrreportsdev'
