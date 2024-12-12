metadata name = 'Serverless Cosmos DB Account and Database'
metadata description = 'This module deploys a Serverless Cosmos DB account and database. It also send diagnostic logs to a supplied Log Analytics workspace, and stores the document endpoint and database name in a supplied Azure App Configuration store'

@description('The name of the Cosmos DB account')
@minLength(5)
@maxLength(50)
param accountName string

@description('The name of the Database that will provisioned in the account')
@minLength(1)
@maxLength(255)
param databaseName string

@description('The name of the Container that will be provisioned in the Database')
param containerName string

@description('The region that the Cosmos DB account will be deployed to')
@allowed([
  'australiaeast'
])
param location string

@description('The tags that will be applied to the Cosmos DB account')
param tags object

@description('The user-assigned identity that this will be granted RBAC roles over the Cosmos DB account')
@minLength(3)
@maxLength(128)
param uaiName string

@description('The name of the App Configuration store that will store values related to this Cosmos DB account')
@minLength(5)
@maxLength(50)
param appConfigName string

@description('The name of the Log Analytics workspace that Cosmos DB will send diagnostic settings to')
@minLength(4)
@maxLength(63)
param logAnalyticsName string

var cosmosDbEndpointSettingName = 'Biotrackr:CosmosDbEndpoint'
var cosmosDatabaseSettingName = 'Biotrackr:DatabaseName'
var containerSettingName = 'Biotrackr:ContainerName'
var cosmosDbDataContributorRole = '00000000-0000-0000-0000-000000000002'

resource uai 'Microsoft.ManagedIdentity/userAssignedIdentities@2023-01-31' existing = {
  name: uaiName
}

resource appConfig 'Microsoft.AppConfiguration/configurationStores@2024-05-01' existing = {
  name: appConfigName
}

resource logAnalytics 'Microsoft.OperationalInsights/workspaces@2023-09-01' existing = {
  name: logAnalyticsName
}

resource account 'Microsoft.DocumentDB/databaseAccounts@2024-08-15' = {
  name: accountName
  location: location
  tags: tags
  kind: 'GlobalDocumentDB'
  properties: {
    databaseAccountOfferType: 'Standard'
    consistencyPolicy: {
      defaultConsistencyLevel: 'Session'
    }
    locations: [
      { 
        failoverPriority: 0
        locationName: location
        isZoneRedundant: false
      }
    ]
    capabilities: [
      { 
        name: 'EnableServerless'
      }
    ]
  }
  identity: {
    type: 'UserAssigned'
    userAssignedIdentities: {
      '${uai.id}': {}
    }
  }
}

resource database 'Microsoft.DocumentDB/databaseAccounts/sqlDatabases@2024-08-15' = {
  name: databaseName
  parent: account
  properties: {
    resource: {
      id: databaseName
    }
  }
}

resource endpointConfigSetting 'Microsoft.AppConfiguration/configurationStores/keyValues@2024-05-01' = {
  name: cosmosDbEndpointSettingName
  parent: appConfig
  properties: {
    value: account.properties.documentEndpoint
  }
}

resource databaseConfigSetting 'Microsoft.AppConfiguration/configurationStores/keyValues@2024-05-01' = {
  name: cosmosDatabaseSettingName
  parent: appConfig
  properties: {
    value: database.name
  }
}

resource activityContainer 'Microsoft.DocumentDB/databaseAccounts/sqlDatabases/containers@2024-08-15' = {
  name: containerName
  parent: database
  properties: {
    resource: {
      id: containerName
      partitionKey: {
        paths: [
          '/documentType'
        ]
        kind: 'Hash'
      }
      indexingPolicy: {
        indexingMode: 'consistent'
        includedPaths: [
          {
            path: '/*'
          }
        ]
      }
    }
  }
}

resource activityContainerSetting 'Microsoft.AppConfiguration/configurationStores/keyValues@2024-05-01' = {
  name: containerSettingName
  parent: appConfig
  properties: {
    value: containerName
  }
}

resource diagnosticLogs 'Microsoft.Insights/diagnosticSettings@2021-05-01-preview' = {
  name: account.name
  scope: account
  properties: {
    workspaceId: logAnalytics.id
    logs: [
      {
        category: 'DataPlaneRequests'
        enabled: true
      }
      {
        category: 'QueryRuntimeStatistics'
        enabled: true
      }
      {
        category: 'PartitionKeyStatistics'
        enabled: true
      }
      {
        category: 'PartitionKeyRUConsumption'
        enabled: true
      }
      {
        category: 'ControlPlaneRequests'
        enabled: true
      }
    ]
  }
}

// Adding role assignment
resource sqlRoleAssignment 'Microsoft.DocumentDB/databaseAccounts/sqlRoleAssignments@2024-11-15' = {
  name: cosmosDbDataContributorRole
  parent: account
  properties: {
    principalId: uai.properties.principalId
    roleDefinitionId: '/${subscription().id}/resourceGroups/${resourceGroup().name}/providers/Microsoft.DocumentDB/databaseAccounts/${account.name}/sqlRoleDefinitions/${cosmosDbDataContributorRole}'
    scope: account.id
  }
}

@description('The name of the deployed Cosmos DB account')
output accountName string = account.name

@description('The name of the deployed Database')
output databaseName string = database.name
