using 'main.bicep'

param name = 'biotrackr-sleep-api-dev'
param imageName = ''
param location = 'australiaeast'
param tags = {
  ApplicationName: 'Biotrackr'
  Component: 'Sleep-Api'
  Environment: 'Dev'
}
param containerAppEnvironmentName = 'env-biotrackr-dev'
param containerRegistryName = 'acrbiotrackrdev'
param uaiName = 'uai-biotrackr-dev'
param appConfigName = 'config-biotrackr-dev'
param cosmosDbAccountName = 'cosmos-biotrackr-dev'
param apimName = 'api-biotrackr-dev'
