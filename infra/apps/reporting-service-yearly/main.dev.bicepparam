using 'main.bicep'

param name = 'biotrackr-rpt-svc-yearly-dev'
param imageName = ''
param location = 'australiaeast'
param tags = {
  ApplicationName: 'Biotrackr'
  Component: 'Reporting-Svc-Yearly'
  Environment: 'Dev'
}
param containerAppEnvironmentName = 'env-biotrackr-dev'
param containerRegistryName = 'acrbiotrackrdev'
param uaiName = 'uai-biotrackr-dev'
param keyVaultName = 'kv-biotrackr-dev'
param appInsightsName = 'appins-biotrackr-dev'
param appConfigName = 'config-biotrackr-dev'
param tenantId = 'e60bb76c-8fda-4ed4-a354-4836e3bfcbc3'
param agentBlueprintClientId = '1d7df96b-ba77-459b-a777-8de9e94206d8'
