@description('The name of the API Management instance that this Product will be deployed to')
param apimName string

@description('The name of the Product')
param productName string

@description('The name of the API that will be linked to this Product')
param apiName string

resource apim 'Microsoft.ApiManagement/service@2024-06-01-preview' existing = {
  name: apimName
}

resource api 'Microsoft.ApiManagement/service/apis@2024-06-01-preview' existing = {
  name: apiName
  parent: apim
}

resource apimProduct 'Microsoft.ApiManagement/service/products@2024-06-01-preview' = {
  name: productName
  parent: apim
  properties: {
    displayName: productName
    approvalRequired: false
    state: 'published'
    subscriptionRequired: true
  }
}

resource apiProductLink 'Microsoft.ApiManagement/service/products/apiLinks@2024-06-01-preview' = {
  name: '${api.name}-link'
  parent: apimProduct
  properties: {
    apiId: api.id
  }
}
