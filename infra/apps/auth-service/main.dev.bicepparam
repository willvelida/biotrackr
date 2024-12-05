using 'main.bicep'

param name = 'biotrackr-auth-svc'
param imageName = 'mcr.microsoft.com/k8se/quickstart-jobs:latest'
param location = 'australiaeast'
param tags = {
  ApplicationName: 'Biotrackr'
  Component: 'Auth-Svc'
  Environment: 'Dev'
}
param containerAppEnvironmentName = 'env-biotrackr-dev'
param containerRegistryName = 'acrbiotrackrdev'
param uaiName = 'uai-biotrackr-dev'
param keyVaultName = 'kv-biotrackr-dev'
param appInsightsName = 'appins-biotrackr-dev'
