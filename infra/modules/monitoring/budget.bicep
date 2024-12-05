metadata name = 'Azure Monitor Budget'
metadata description = 'This module deploys an Azure Budget to a resource group.'

@description('The name of the budget')
@minLength(3)
@maxLength(50)
param name string

@description('The limit of the budget')
@maxValue(250)
param amount int

@description('The first budget threshold')
@minValue(1)
@maxValue(150)
param firstThreshold int

@description('The second budget threshold')
@minValue(1)
@maxValue(200)
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
