@description('The name of the budget')
param name string

@description('The limit of the budget')
param amount int

@description('The first budget threshold')
param firstThreshold int

@description('The second budget threshold')
param secondThreshold int

@description('The email address of the owner of this budget')
param ownerEmail string

@description('The start date of this budget. Must be in YYYY-MM-DD format')
param startDate string

resource budget 'Microsoft.Consumption/budgets@2024-08-01' = {
  name: name
  properties: {
    amount: amount
    category: 'Cost'
    timeGrain: 'BillingMonth'
    timePeriod: {
      startDate: startDate
    }
    notifications: {
      NotificationForExceededBudget1: {
        contactEmails: [
          ownerEmail
        ]
        enabled: true
        operator: 'GreaterThanOrEqualTo'
        threshold: firstThreshold
      }
      NotificationForExceededBudget2: {
        contactEmails: [
          ownerEmail
        ]
        enabled: true
        operator: 'GreaterThanOrEqualTo'
        threshold: secondThreshold
      }
    }
    filter: {
      dimensions: {
        name: 'ResourceGroupName'
        operator: 'In'
        values: [
          resourceGroup().name
        ]
      }
    }
  }
}
