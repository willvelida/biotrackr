@description('The name of the API Management instance that this Product will be deployed to')
param apimName string

@description('The name of the Product')
param productName string

resource apim 'Microsoft.ApiManagement/service@2024-06-01-preview' existing = {
  name: apimName
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
