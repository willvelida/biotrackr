using 'main.bicep'

param name = 'biotrackr-ui-dev'
param imageName = 'mcr.microsoft.com/k8se/quickstart:latest'
param location = 'australiaeast'
param tags = {
  ApplicationName: 'Biotrackr'
  Component: 'UI'
  Environment: 'Dev'
}
param containerAppEnvironmentName = 'env-biotrackr-dev'
param containerRegistryName = 'acrbiotrackrdev'
param uaiName = 'uai-biotrackr-dev'
