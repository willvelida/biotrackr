using 'main.bicep'

param rgName = 'rg-biotrackr-dev'
param location = 'australiaeast'
param tags = {
  ApplicationName: 'Biotrackr'
  Component: 'Core-Infra'
  Environment: 'Dev'
}
param appInsightsName = 'appins-biotrackr-dev'
param logAnalyticsName = 'law-biotrackr-dev'
param containerAppEnvName = 'env-biotrackr-dev'
