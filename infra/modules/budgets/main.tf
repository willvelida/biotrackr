resource "azurerm_consumption_budget_resource_group" "budget" {
  name = var.budget_name
  resource_group_id = var.resource_group_id
    amount = var.amount
    time_grain = "Monthly"
    time_period {
        start_date = var.start_date
        end_date = var.end_date
    }

    filter {
      dimension {
        name = "ResourceGroupName"
        operator = "In"
        values = [ var.resource_group_name ]
      }
    }

    notification {
        enabled = true
        threshold = var.threshold_one
        operator = "EqualTo"
        threshold_type = "Actual"
      contact_groups = [ 
        var.monitor_action_group_id
       ]
    }

    notification {
        enabled = true
        threshold = var.threshold_two
        operator = "GreaterThan"
        threshold_type = "Actual"
      contact_groups = [ 
        var.monitor_action_group_id
       ]
    }
}