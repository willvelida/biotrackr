@description('The name of the Cosmos DB account')
param accountName string

@description('The name of the Database that will provisioned in the account')
param databaseName string

@description('The region that the Cosmos DB account will be deployed to')
param location string

@description('The tags that will be applied to the Cosmos DB account')
param tags object

@description('The user-assigned identity that this will be granted RBAC roles over the Cosmos DB account')
param uaiName string

@description('The name of the App Configuration store that will store values related to this Cosmos DB account')
param appConfigName string

var cosmosDbEndpointSettingName = 'Biotrackr:CosmosDbEndpoint'
var cosmosDatabaseSettingName = 'Biotrackr:DatabaseName'

resource uai 'Microsoft.ManagedIdentity/userAssignedIdentities@2023-01-31' existing = {
  name: uaiName
}

resource appConfig 'Microsoft.AppConfiguration/configurationStores@2024-05-01' existing = {
  name: appConfigName
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
    options: {
      throughput: 1000
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

@description('The name of the deployed Cosmos DB account')
output accountName string = account.name

@description('The name of the deployed Database')
output databaseName string = database.name
