using 'main.bicep'

param rgName = 'rg-biotrackr-identity'
param location = 'australiaeast'
param tags = {
  ApplicationName: 'Biotrackr'
  Component: 'GitHub-Deployment'
  Environment: 'Production'
}
param uaiName = 'uai-gh-biotrackr'
param githubOrganizationName = 'willvelida'
param githubRepositoryName = 'biotrackr'
param githubEnvironmentName = 'production'
