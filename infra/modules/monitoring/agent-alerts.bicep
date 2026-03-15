metadata name = 'Agent Alert Rules'
metadata description = 'Azure Monitor scheduled query rules for agent anomaly detection'

@description('The base name given to all resources')
@minLength(5)
@maxLength(50)
param baseName string

@description('The environment that these resources will be deployed to')
param environment string

@description('Location for the alert rules')
@allowed([
  'australiaeast'
])
param location string

@description('The name of the Application Insights instance')
param appInsightsName string

@description('Email address for alert notifications')
param alertEmailAddress string

@description('The tags that will be applied to these resources')
param tags object

resource appInsights 'Microsoft.Insights/components@2020-02-02' existing = {
  name: appInsightsName
}

resource actionGroup 'Microsoft.Insights/actionGroups@2023-01-01' = {
  name: 'ag-${baseName}-agent-alerts-${environment}'
  location: 'global'
  tags: tags
  properties: {
    groupShortName: 'AgentAlerts'
    enabled: true
    emailReceivers: [
      {
        name: 'Biotrackr Admins'
        emailAddress: alertEmailAddress
      }
    ]
  }
}

resource excessiveToolCallsAlert 'Microsoft.Insights/scheduledQueryRules@2023-03-15-preview' = {
  name: 'alert-excessive-tool-calls-${environment}'
  location: location
  tags: tags
  properties: {
    description: 'Fires when a single chat session exceeds 50 tool calls in 5 minutes'
    severity: 2
    enabled: true
    evaluationFrequency: 'PT5M'
    windowSize: 'PT5M'
    scopes: [appInsights.id]
    skipQueryValidation: true
    criteria: {
      allOf: [
        {
          query: '''
            AppTraces
            | where Message startswith "Tool call invoked:"
            | extend SessionId = tostring(Properties["SessionId"])
            | summarize ToolCallCount = count() by SessionId, bin(TimeGenerated, 5m)
            | where ToolCallCount > 50
          '''
          timeAggregation: 'Count'
          operator: 'GreaterThan'
          threshold: 0
        }
      ]
    }
    actions: {
      actionGroups: [actionGroup.id]
    }
  }
}

resource httpErrorSpikeAlert 'Microsoft.Insights/scheduledQueryRules@2023-03-15-preview' = {
  name: 'alert-agent-auth-failures-${environment}'
  location: location
  tags: tags
  properties: {
    description: 'Fires when HTTP 401/403 responses exceed 10 in a 15 minute window'
    severity: 1
    enabled: true
    evaluationFrequency: 'PT5M'
    windowSize: 'PT15M'
    scopes: [appInsights.id]
    skipQueryValidation: true
    criteria: {
      allOf: [
        {
          query: '''
            AppRequests
            | where ResultCode in ("401", "403")
            | summarize ErrorCount = count() by bin(TimeGenerated, 15m)
            | where ErrorCount > 10
          '''
          timeAggregation: 'Count'
          operator: 'GreaterThan'
          threshold: 0
        }
      ]
    }
    actions: {
      actionGroups: [actionGroup.id]
    }
  }
}
