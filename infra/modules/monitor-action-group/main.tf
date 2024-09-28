resource "azurerm_monitor_action_group" "amg" {
  name = var.amg_name
  resource_group_name = var.rg_name
  short_name = var.amg_short_name
  tags = var.tags

  email_receiver {
    name = var.email_receiver_name
    email_address = var.email_address
    use_common_alert_schema = true
  }
}