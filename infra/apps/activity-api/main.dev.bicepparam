using 'main.bicep'

param name = 'biotrackr-activity-api-dev'
param imageName = 'mcr.microsoft.com/k8se/quickstart:latest'
param location = 'australiaeast'
param tags = {
  ApplicationName: 'Biotrackr'
  Component: 'Activity-Api'
  Environment: 'Dev'
}
param containerAppEnvironmentName = 'env-biotrackr-dev'
param containerRegistryName = 'acrbiotrackrdev'
param uaiName = 'uai-biotrackr-dev'
param appConfigName = 'config-biotrackr-dev'
param cosmosDbAccountName = 'cosmos-biotrackr-dev'
