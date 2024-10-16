module "resource_group" {
  source   = "../modules/resource-group"
  name     = var.resource_group_name
  location = var.location
  tags     = var.tags
}

module "law" {
  source              = "../modules/log-analytics"
  law_name            = var.law_name
  location            = var.location
  resource_group_name = module.resource_group.name
  tags                = var.tags
}

module "ai" {
  source              = "../modules/app-insights"
  app_insights_name   = var.app_insights_name
  location            = var.location
  resource_group_name = module.resource_group.name
  workspace_id        = module.law.id
  tags                = var.tags
}

module "vnet" {
  source              = "../modules/virtual-network"
  location            = var.location
  resource_group_name = module.resource_group.name
  tags                = var.tags
  vnet_name           = var.vnet_name
  address_space       = var.vnet_address_space
}

module "aca_subnet" {
  source                  = "../modules/virutal-network-subnet"
  resource_group_name     = module.resource_group.name
  vnet_name               = module.vnet.vnet_name
  subnet_name             = var.aca_subnet_name
  subnet_address_prefixes = var.aca_subnet_address_prefixes
}

module "aca_env" {
  source                                      = "../modules/container-app-environment"
  env_name                                    = var.aca_env_name
  location                                    = var.location
  resource_group_name                         = module.resource_group.name
  log_analytics_workspace_id                  = module.law.id
  dapr_application_insights_connection_string = module.ai.connection_string
  infrastructure_subnet_id                    = module.aca_subnet.subnet_id
  tags                                        = var.tags
}

module "usi" {
  source   = "../modules/user-assigned-identity"
  name     = var.user_assigned_identity_name
  location = var.location
  rg_name  = module.resource_group.name
  tags     = var.tags
}

module "acr" {
  source                    = "../modules/container-registry"
  name                      = var.acr_name
  location                  = var.location
  rg_name                   = module.resource_group.name
  sku                       = var.acr_sku
  admin_enabled             = var.acr_admin_enabled
  user_assigned_identity_id = module.usi.user_assinged_identity_id
  tags                      = var.tags
}

module "acr_pull_role" {
  source       = "../modules/role-assignment"
  role_name    = var.acr_pull_role_name
  principal_id = module.usi.user_assinged_identity_principal_id
  scope_id     = module.acr.acr_id
}

module "mag" {
  source              = "../modules/monitor-action-group"
  amg_name            = var.monitor_action_group_name
  amg_short_name      = var.monitor_action_group_short_name
  rg_name             = module.resource_group.name
  tags                = var.tags
  email_address       = var.email_address
  email_receiver_name = var.email_receiver_name
}

module "budget" {
  source                  = "../modules/budgets"
  budget_name             = var.budget_name
  resource_group_name     = module.resource_group.name
  resource_group_id       = module.resource_group.id
  monitor_action_group_id = module.mag.amg_id
}

module "kv" {
  source      = "../modules/key-vault"
  kv_name     = var.kv_name
  location    = module.resource_group.location
  rg_name     = module.resource_group.name
  tags        = var.tags
  kv_sku_name = var.kv_sku_name
}

module "sb" {
  source      = "../modules/service-bus"
  sb_name     = var.sb_name
  location    = module.resource_group.location
  rg_name     = module.resource_group.name
  tags        = var.tags
  identity_id = module.usi.user_assinged_identity_id
}