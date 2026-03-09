using 'main.bicep'

param name = 'biotrackr-chat-api-dev'
param imageName = 'mcr.microsoft.com/k8se/quickstart:latest'
param location = 'australiaeast'
param tags = {
  ApplicationName: 'Biotrackr'
  Component: 'Chat-Api'
  Environment: 'Dev'
}
param containerAppEnvironmentName = 'env-biotrackr-dev'
param containerRegistryName = 'acrbiotrackrdev'
param uaiName = 'uai-biotrackr-dev'
param appConfigName = 'config-biotrackr-dev'
param cosmosDbAccountName = 'cosmos-biotrackr-dev'
param apimName = 'api-biotrackr-dev'
param keyVaultName = 'kv-biotrackr-dev'
param enableManagedIdentityAuth = true
param tenantId = ''
param chatAgentModel = 'claude-sonnet-4-6'
param agentBlueprintClientId = 'b638e7d4-52f3-4afb-a466-9b796da98d77'
param agentIdentityId = '707307f7-ffc4-4744-a66b-19fa942c1c10'
