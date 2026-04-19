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
param agentBlueprintClientId = '1d7df96b-ba77-459b-a777-8de9e94206d8'
param chatApiAgentIdentityId = '707307f7-ffc4-4744-a66b-19fa942c1c10'
param storageAccountName = 'stbiotrackrreportsdev'
