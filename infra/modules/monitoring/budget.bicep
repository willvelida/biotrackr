metadata name = 'Azure Monitor Budget'
metadata description = 'This module deploys an Azure Budget to a resource group.'

@description('The base name given to all resources')
@minLength(5)
@maxLength(50)
param baseName string

@description('The environment that the Budget will be deployed to')
param environment string

@description('The limit of the budget. Default is $200')
@maxValue(250)
param amount int = 200

@description('The first budget threshold. Default is $100')
@minValue(1)
@maxValue(150)
param firstThreshold int = 100

@description('The second budget threshold. Default is $175')
@minValue(1)
@maxValue(200)
param secondThreshold int = 175

@description('The email address of the owner of this budget')
param ownerEmail string = 'willvelida@hotmail.co.uk'

@description('The start date of this budget. Must be in YYYY-MM-DD format')
param startDate string = '2024-12-01'

var budgetName = 'budget-${baseName}-${environment}'

resource budget 'Microsoft.Consumption/budgets@2024-08-01' = {
  name: budgetName
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
