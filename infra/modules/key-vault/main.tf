data "azurerm_client_config" "current" {}

resource "azurerm_key_vault" "kv" {
  name = var.kv_name
  location = var.location
  resource_group_name = var.rg_name
  tags = var.tags
  enable_rbac_authorization = true
  tenant_id = data.azurerm_client_config.current.tenant_id
  sku_name = var.kv_sku_name
  enabled_for_template_deployment = true
}